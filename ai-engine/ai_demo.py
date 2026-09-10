"""
CLYVO CARE - Módulo de Demonstração de Inteligência Artificial
Sprint 3 - Disruptive Architectures: IoT, IoB & Generative IA (FIAP)

Este script demonstra:
1. LLM com Extração Estruturada (Pydantic / Structured Outputs / Function Calling)
   - Simula o diálogo com o tutor via WhatsApp.
   - Extrai sintomas não estruturados e converte em JSON estrito.
2. Motor de Score de Risco Clínico Preditivo
   - Cruza os dados do tutor, telemetria IoT (acelerômetro/temperatura) e visão computacional.
   - Determina a gravidade (BAIXO, MODERADO, CRÍTICO) para o Dashboard da Clyvo Vet.
"""

import json
from datetime import datetime
from pydantic import BaseModel, Field


# -------------------------------------------------------------
# 1. Esquema Estruturado para Validação do Output da IA (LLM)
# -------------------------------------------------------------
class RelatoClinicoTutor(BaseModel):
    pet_id: str = Field(description="ID do Pet cadastrado na Clyvo Vet")
    comeu_bem: bool = Field(description="Se o pet ingeriu a quantidade esperada de ração")
    bebeu_agua: bool = Field(description="Se houve consumo hídrico adequado")
    tomou_medicacao: bool = Field(description="Se os remédios prescritos foram administrados")
    nivel_dor_observado: int = Field(ge=0, le=10, description="Escala de desconforto de 0 a 10")
    sintomas_detectados: list[str] = Field(default_factory=list, description="Lista de sintomas (ex: vômito, tremor, letargia)")
    humor_tutor_ansioso: bool = Field(description="Análise de sentimento sobre o estado emocional do tutor")


# -------------------------------------------------------------
# 2. Simulação de Resposta de LLM (com RAG & Function Calling)
# -------------------------------------------------------------
def simular_agente_conversacional_llm(mensagem_tutor: str, prontuario_alta: dict) -> RelatoClinicoTutor:
    """
    Simula o comportamento de um LLM recebendo o prompt contextualizado 
    com o prontuário da clínica (RAG) e extraindo as variáveis clínicas estruturadas.
    """
    print("\n[1] === MÓDULO LLM / AGENTE CONVERSACIONAL (IoB) ===")
    print(f"-> Mensagem recebida do Tutor: \"{mensagem_tutor}\"")
    print(f"-> Contexto RAG recuperado: Procedimento: {prontuario_alta['cirurgia']} | Medicação: {prontuario_alta['medicamento']}")

    # Simulação da extração semântica feita pelo modelo a partir do texto informal
    dados_extraidos = RelatoClinicoTutor(
        pet_id=prontuario_alta["pet_id"],
        comeu_bem=False,       # Tutor disse: "só comeu um tiquinho"
        bebeu_agua=True,       # Tutor disse: "bebeu água normalmente"
        tomou_medicacao=True,  # Tutor disse: "dei o antibiótico"
        nivel_dor_observado=7, # Tutor disse: "parece estar sentindo bastante dor quando encosta na pata"
        sintomas_detectados=["letargia", "hiporexia", "dor_localizada"],
        humor_tutor_ansioso=True
    )
    
    print("\n-> Output Estruturado gerado pela IA (JSON/DTO):")
    print(dados_extraidos.model_dump_json(indent=2))
    return dados_extraidos


# -------------------------------------------------------------
# 3. Motor Preditivo de Score de Risco Clínico
# -------------------------------------------------------------
def calcular_score_risco(
    relato: RelatoClinicoTutor,
    telemetria_iot: dict,
    visao_computacional: dict
) -> dict:
    """
    Cruza as 3 frentes da solução:
    - Diálogo do Tutor (NLP/LLM)
    - Sensor da Coleira (IoT - Acelerômetro/Temperatura)
    - Câmera da Tigela (Visão Computacional)
    """
    print("\n[2] === MOTOR PREDITIVO DE RISCO CLÍNICO ===")
    
    score = 0
    fatores_risco = []

    # Avaliação do relato do tutor processado pelo LLM
    if not relato.comeu_bem:
        score += 15
        fatores_risco.append("Hiporexia / Redução de apetite informada pelo tutor")
    if not relato.tomou_medicacao:
        score += 35
        fatores_risco.append("Quebra de adesão medicamentosa (Risco grave)")
    if relato.nivel_dor_observado >= 6:
        score += (relato.nivel_dor_observado * 4)
        fatores_risco.append(f"Índice de dor elevado informado ({relato.nivel_dor_observado}/10)")

    # Avaliação da telemetria IoT (sensor da coleira simulado)
    temp = telemetria_iot.get("temperatura_celsius", 38.5)
    if temp > 39.4:
        score += 25
        fatores_risco.append(f"Hipertermia / Febre detectada no IoT ({temp}°C)")
    elif temp < 37.5:
        score += 25
        fatores_risco.append(f"Hipotermia detectada no IoT ({temp}°C)")

    nivel_atividade = telemetria_iot.get("nivel_atividade_acelerometro")
    if nivel_atividade == "LETHARGIC":
        score += 20
        fatores_risco.append("Acelerômetro registrou imobilidade atípica pós-operatória")

    # Avaliação da visão computacional (câmera da tigela)
    visitas_tigela = visao_computacional.get("eventos_consumo_ultimas_12h", 0)
    if visitas_tigela == 0:
        score += 15
        fatores_risco.append("Câmera residencial não detectou aproximação da tigela nas últimas 12h")

    # Limite do Score entre 0 e 100
    score_final = min(score, 100)

    # Classificação de Gravidade para a Clínica Clyvo Vet
    if score_final >= 70:
        classificacao = "CRÍTICO (Intervenção Imediata)"
        acao_recomendada = "Acionar plantão veterinário para contato telefônico urgente e antecipação de consulta."
    elif score_final >= 40:
        classificacao = "MODERADO (Atenção Redobrada)"
        acao_recomendada = "Enviar orientações adicionais ao tutor e solicitar novo check-in em 4 horas."
    else:
        classificacao = "ESTÁVEL (Recuperação Esperada)"
        acao_recomendada = "Manter protocolo habitual de acompanhamento diário."

    resultado = {
        "pet_id": relato.pet_id,
        "timestamp_analise": datetime.now().isoformat(),
        "score_risco": score_final,
        "classificacao": classificacao,
        "fatores_detectados": fatores_risco,
        "recomendacao_clinica": acao_recomendada
    }

    print("\n-> Avaliação de Risco Consolidada:")
    print(json.dumps(resultado, indent=2, ensure_ascii=False))
    return resultado


# -------------------------------------------------------------
# Execução da Demonstração
# -------------------------------------------------------------
if __name__ == "__main__":
    # Contexto clínico simulado (origem: Prontuário Clyvo Vet via Spring Boot)
    mock_prontuario = {
        "pet_id": "pet-clyvo-7892",
        "nome_pet": "Thor",
        "cirurgia": "Ortopédica (Osteossíntese de Fêmur)",
        "medicamento": "Meloxicam + Cefalexina"
    }

    # Mensagem informal simulada enviada pelo tutor no WhatsApp
    mock_mensagem_whatsapp = (
        "Olá, aqui é o tutor do Thor. Hoje ele acordou meio desanimado, só comeu um tiquinho de nada, "
        "mas bebeu água normalmente. Eu dei o antibiótico certinho, só que ele parece estar sentindo "
        "bastante dor quando tenta apoiar a pata traseira, chora quando encosta."
    )

    # Dados simulados do sensor IoT da coleira (Wokwi: Acelerômetro + Temp)
    mock_iot = {
        "temperatura_celsius": 39.6,  # Febre detectada
        "nivel_atividade_acelerometro": "LETHARGIC"
    }

    # Dados simulados da Visão Computacional (detecção da tigela/pet)
    mock_visao = {
        "eventos_consumo_ultimas_12h": 0
    }

    print("================================================================")
    print("DEMONSTRAÇÃO DO COMPONENTE DE IA - CLYVO CARE (SPRINT 3)")
    print("================================================================")
    
    # 1. Executa o Agente LLM
    relato_processado = simular_agente_conversacional_llm(mock_mensagem_whatsapp, mock_prontuario)

    # 2. Executa o Motor de Risco com dados multimodais
    resultado_risco = calcular_score_risco(relato_processado, mock_iot, mock_visao)