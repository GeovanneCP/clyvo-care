# Clyvo Care – Monitoramento Preditivo e Adesão Contínua (CLYVO VET)

> **Sprint 4 - Arquitetura Integrada: .NET 8 Web API & Inteligência Artificial (FastAPI)**  
> **FIAP 2026**

---

## 📌 1. Visão Geral e Problema de Negócio

### O Problema
No pós-operatório ou no tratamento de condições clínicas e cirúrgicas crônicas em clínicas veterinárias (como na **CLYVO VET**), a continuidade do cuidado frequentemente sofre rupturas críticas após a alta hospitalar. Identificam-se altas taxas de abandono ou interrupção prematura da terapia medicamentosa, esquecimentos de dosagens e extrema dificuldade dos tutores em notar **sinais clínicos sutis de piora** (como picos febris, letargia prolongada, dor silenciosa, perda de apetite e desidratação). 

Esse cenário culmina em complicações evitáveis, readmissões emergenciais de alto risco e perda de confiança do tutor com a clínica.

### A Solução
O **Clyvo Care** é um ecossistema inteligente de monitoramento proativo contínuo composto por:
- **Camada de Triagem Cognitiva (NLP / LLM):** Processamento de relatos diários informais de tutores enviados via canais conversacionais (WhatsApp/App), convertendo descrições subjetivas em parâmetros clínicos padronizados.
- **Camada de Telemetria e Percepção Multimodal (IoT & Visão Computacional):** Monitoramento contínuo de temperatura corporal, acelerometria (níveis de atividade) e frequência de alimentação por sensores e câmeras.
- **Motor Preditivo de Risco Clínico:** Cruzamento instantâneo entre os dados subjetivos extraídos pela IA e as métricas físicas da telemetria, gerando um índice determinístico de gravidade (0 a 100) para acionamento médico imediato.
- **Backend Robusto em .NET 8:** Orquestrador central em arquitetura REST que autentica, valida, consome os serviços de IA e disponibiliza os endpoints clínicos consolidados para os painéis hospitalares.

---

## 🎯 2. Objetivos e Benefícios por Stakeholder

- **Para o Tutor:** Acompanhamento humanizado, canal direto para reporte de sintomas, orientações sem ruído e tranquilidade na recuperação do pet.
- **Para a Equipe Veterinária:** Triagem prévia automatizada que prioriza os casos realmente críticos na fila de atendimento, reduzindo a sobrecarga hospitalar.
- **Para a Clínica Veterinária (B2B):** Redução drástica de intercorrências graves, aumento de adesão a tratamentos prescritos e maximização do *Lifetime Value* (LTV) com retenção ativa de clientes.
- **Para o Animal:** Intervenção rápida a desvios sutis de saúde, alívio precoce de quadros álgicos (dor) e garantia de cumprimento da posologia cirúrgica.

---

## 🧠 3. Papel da Inteligência Artificial no Sistema

A inteligência da plataforma opera por meio de uma **arquitetura híbrida** dividida em duas responsabilidades especializadas:

### 1. Triagem Cognitiva e Extração de Entidades Clínicas (NLP)
- **Localização:** Endpoint `/api/v1/ai/triage` (FastAPI).
- **Entrada:** Identificador do animal (`pet_id`) e mensagem informal em texto livre digitada pelo tutor.
- **Mecanismo:** Analisa semanticamente a mensagem para identificar padrões como:
  - Nível de dor percebido (escalonado de 0 a 10).
  - Adesão medicamentosa (se o tutor administrou ou se o animal repeliu/vomitou o remédio).
  - Status nutricional e hídrico (ingestão alimentar e de água).
  - Sintomas clínicos declarados (`febre`, `letargia`, `vomito`, `diarreia`, `mancando`, `chorando`).
  - Estado emocional do tutor (`tutor_ansioso`).
- **Saída:** Objeto JSON estruturado e estritamente tipado (`ExtractedClinicalData`).

### 2. Motor Preditivo de Gravidade Clínica (Fusão Multimodal)
- **Localização:** Endpoint `/api/v1/ai/risk-score` (FastAPI).
- **Entrada:** Dados clínicos extraídos da mensagem + métricas do sensor de coleira IoT (temperatura e aceleração) + contagem de idas à tigela por visão computacional (últimas 12h).
- **Lógica de Pontuação Preditiva (0 a 100 pontos):**
  - **Fatores Nutricionais/Adesão:** Hiporexia (+15 pts); Quebra na administração da medicação (+35 pts).
  - **Fator Álgico:** Dor clínica $\ge 6$ adiciona $(\text{Nível de Dor} \times 4)$ pontos.
  - **Fatores IoT:** Temperatura $> 39.4^\circ\text{C}$ ou $< 37.5^\circ\text{C}$ (+25 pts); Acelerômetro indicando letargia contínua (+20 pts).
  - **Visão Computacional:** Ausência total de eventos alimentares nas últimas 12h (+15 pts).
- **Classificação Final:**
  - **CRÍTICO ($\ge 70$ pts):** Disparo de alerta médico urgente (`alerta_critico: true`), notificação imediata ao plantonista.
  - **MODERADO (40 a 69 pts):** Recomendação de novo check-in em intervalo de 4 horas e acompanhamento de curvas vitais.
  - **ESTÁVEL ($< 40$ pts):** Parâmetros sob controle, manutenção da rotina de recuperação.

---

## 🏗️ 4. Arquitetura da Solução e Fluxo de Integração

           [ Swagger / Frontend / App ]
                         │
                         ▼ HTTP POST
         ┌────────────────────────────────┐
         │     Backend .NET 8 Web API     │
         │        (Porta 5145)            │
         │   Controller: CheckInController│
         └───────────────┬────────────────┘
                         │
              HttpClient (ClyvoAiClient)
                         │
       ┌─────────────────┴─────────────────┐
       │                                   │
       ▼ HTTP POST                         ▼ HTTP POST
┌───────────────────────┐           ┌───────────────────────┐
│ /api/v1/ai/triage     │           │ /api/v1/ai/risk-score │
│ (Extração de Sintomas)│           │ (Score Multimodal)    │
└──────────┬────────────┘           └───────────┬───────────┘
│                                    │
└─────────────────┬──────────────────┘
│
▼
┌────────────────────────────────┐
│    Microserviço de IA Python   │
│      FastAPI (Porta 5000)      │
└────────────────────────────────┘
1. O cliente consome o endpoint unificado do .NET: `POST /api/v1/check-ins/analisar`.
2. O `CheckInController` recebe os parâmetros do pet, telemetria e o relato.
3. O serviço `ClyvoAiClient` despacha a mensagem para a rota de triagem do FastAPI na porta `5000`.
4. O `ClyvoAiClient` compõe os dados extraídos com a telemetria do pet e aciona o cálculo de risco na porta `5000`.
5. O .NET consolida os resultados e retorna o status `200 OK` unificado com diagnóstico e plano de ação.

---

## 📂 5. Estrutura do Repositório

```text
clyvo-care/
├── ai-engine/
│   ├── main.py              # Microserviço FastAPI (Triagem e Cálculo de Risco)
│   ├── ai_demo.py           # Script de demonstração independente de IA
│   └── requirements.txt     # Dependências Python (fastapi, uvicorn, pydantic)
├── backend-dotnet/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── CheckInController.cs   # Controller integrado com a IA
│   │   └── LogsSaudeController.cs
│   ├── Services/
│   │   └── ClyvoAiClient.cs       # Cliente HTTP de comunicação com o FastAPI
│   ├── Program.cs                 # Configuração de injeção de dependência e CORS
│   └── ClyvoCare.API.csproj       # Projeto .NET 8
└── README.md                      # Documentação técnica completa
💻 6. Pré-requisitos de InstalaçãoAntes de executar o ecossistema, certifique-se de possuir instalado na máquina:.NET SDK 8.0 ou superior (dotnet --version)Python 3.10 ou superior (python --version)Git(Recomendado para Windows): Clonar ou mover o projeto fora de pastas gerenciadas por nuvem (como OneDrive) para evitar travas de compilação por sincronização (exemplo: C:\Dev\clyvo-care).🚀 7. Como Executar os Serviços Passo a PassoPara que a integração funcione, os dois serviços devem ser iniciados simultaneamente em terminais separados.Passo 1: Iniciar o Microserviço de IA (Python FastAPI)Abra um terminal na pasta raiz do projeto (clyvo-care).Instale as dependências necessárias:Bashpip install -r ai-engine/requirements.txt
(Caso não possua o FastAPI e Uvicorn no requirements, instale diretamente com pip install fastapi uvicorn pydantic).Inicie o servidor FastAPI na porta 5000:Bashpython -m uvicorn main:app --app-dir ai-engine --port 5000 --reload
Validação: O terminal exibirá:PlaintextINFO:     Uvicorn running on [http://127.0.0.1:5000](http://127.0.0.1:5000) (Press CTRL+C to quit)
INFO:     Application startup complete.
Passo 2: Iniciar a API Backend (.NET 8)Abra um segundo terminal e acesse a pasta do projeto .NET:Bashcd backend-dotnet
Compile a solução garantindo integridade de binários:Bashdotnet build /p:UseSharedCompilation=false
Inicie a API .NET:Bashdotnet run
Validação: O Kestrel iniciará os ouvintes HTTP e HTTPS, disponibilizando o serviço na porta 5145 (ou na porta configurada no seu launchSettings.json).🧪 8. Teste e Validação da Integração via SwaggerCom ambos os terminais em execução:Endereço de Acesso ao Swagger:👉 http://localhost:5145/swaggerExecutando a Triagem Integrada:Localize a seção CheckIn e clique sobre o endpoint:POST /api/v1/check-ins/analisarClique no botão Try it out.Preencha o parâmetro de query:petId: pet-bob-01No campo Request body (application/json), insira o payload estruturado de teste:JSON{
  "relatoTutor": "Ele está com muita dor na pata traseira, chorando muito e não dei o remédio porque ele vomitou.",
  "temperatura": 39.8,
  "atividade": "LETHARGIC",
  "refeicoes": 0
}
Clique em Execute.📋 9. Resposta Esperada (HTTP 200 OK)A requisição orquestrará a chamada entre as pilhas tecnológicas e devolverá o diagnóstico consolidado com HTTP status 200:JSON{
  "status": "Sucesso",
  "petId": "pet-bob-01",
  "dadosClinicos": {
    "pet_id": "pet-bob-01",
    "comeu_bem": true,
    "bebeu_agua": true,
    "tomou_medicacao": false,
    "nivel_dor": 7,
    "sintomas": [
      "vomito",
      "chorando"
    ],
    "tutor_ansioso": false
  },
  "avaliacaoRisco": {
    "pet_id": "pet-bob-01",
    "timestamp": "2026-10-26T20:33:10.202842",
    "score_risco": 100,
    "classificacao": "CRÍTICO",
    "fatores_detectados": [
      "Quebra de adesão medicamentosa prescrita",
      "Dor informada em patamar alto (7/10)",
      "Hipertermia / Febre detectada no IoT (39.8°C)",
      "Acelerômetro registrou padrão prolongado de letargia",
      "Câmera registrou ausência de ida à tigela nas últimas 12h"
    ],
    "recomendacao_clinica": "Priorizar atendimento imediato. Notificar veterinário plantonista.",
    "alerta_critico": true
  }
}
👥 Integrantes do Grupo

Geovanne Coneglian RM562673
Lucas Silva Gastão Pinheiro RM563960
Guilherme Soares de Almeida RM563143