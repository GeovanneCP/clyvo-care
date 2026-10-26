from datetime import datetime
from fastapi import FastAPI
from pydantic import BaseModel, Field

app = FastAPI(
    title="Clyvo Care - AI Microservice",
    description="Serviço de Triagem Cognitiva e Motor Preditivo de Risco Clínico (Clyvo Vet)",
    version="2.0.0"
)

# -------------------------------------------------------------
# Modelos de Dados (Entrada e Saída da IA)
# -------------------------------------------------------------
class TriageRequest(BaseModel):
    pet_id: str
    tutor_message: str
    clinical_history: str = ""

class ExtractedClinicalData(BaseModel):
    pet_id: str
    comeu_bem: bool
    bebeu_agua: bool
    tomou_medicacao: bool
    nivel_dor: int = Field(ge=0, le=10)
    sintomas: list[str]
    tutor_ansioso: bool

class RiskAssessmentRequest(BaseModel):
    pet_id: str
    clinical_data: ExtractedClinicalData
    iot_temperature: float = Field(description="Temperatura do sensor em °C")
    iot_activity_level: str = Field(description="LOW, NORMAL, HIGH, LETHARGIC")
    cv_feeding_events_12h: int = Field(description="Idas à tigela detectadas por visão computacional")

class RiskAssessmentResponse(BaseModel):
    pet_id: str
    timestamp: str
    score_risco: int
    classificacao: str
    fatores_detectados: list[str]
    recomendacao_clinica: str
    alerta_critico: bool

# -------------------------------------------------------------
# Endpoints que o .NET vai chamar
# -------------------------------------------------------------
@app.post("/api/v1/ai/triage", response_model=ExtractedClinicalData)
def triage_conversational_agent(request: TriageRequest):
    """Processa o relato informal do tutor e extrai dados clínicos."""
    msg = request.tutor_message.lower()
    
    comeu = not ("não comeu" in msg or "só comeu um tiquinho" in msg or "pouco" in msg or "sem apetite" in msg)
    bebeu = not ("não bebeu" in msg or "sem beber" in msg)
    medicou = not ("não dei" in msg or "esqueci" in msg or "vomitou o remédio" in msg)
    
    dor = 2
    if "dor" in msg or "chora" in msg or "gemendo" in msg:
        dor = 7 if ("muita" in msg or "bastante" in msg or "forte" in msg) else 5

    sintomas = []
    for s in ["febre", "letargia", "vomito", "vômito", "diarreia", "mancando", "chorando", "desanimado"]:
        if s in msg:
            sintomas.append(s)

    ansioso = any(w in msg for w in ["preocupado", "preocupada", "urgente", "medo", "socorro", "ajuda"])

    return ExtractedClinicalData(
        pet_id=request.pet_id,
        comeu_bem=comeu,
        bebeu_agua=bebeu,
        tomou_medicacao=medicou,
        nivel_dor=dor,
        sintomas=sintomas,
        tutor_ansioso=ansioso
    )

@app.post("/api/v1/ai/risk-score", response_model=RiskAssessmentResponse)
def calculate_risk_score(request: RiskAssessmentRequest):
    """Calcula o Score de Risco cruzando relato, IoT e Visão Computacional."""
    score = 0
    fatores = []

    # 1. Avaliação do relato (NLP/LLM)
    if not request.clinical_data.comeu_bem:
        score += 15
        fatores.append("Hiporexia / Redução de apetite informada pelo tutor")
    if not request.clinical_data.tomou_medicacao:
        score += 35
        fatores.append("Quebra de adesão medicamentosa prescrita")
    if request.clinical_data.nivel_dor >= 6:
        score += (request.clinical_data.nivel_dor * 4)
        fatores.append(f"Dor informada em patamar alto ({request.clinical_data.nivel_dor}/10)")

    # 2. Avaliação do IoT
    if request.iot_temperature > 39.4:
        score += 25
        fatores.append(f"Hipertermia / Febre detectada no IoT ({request.iot_temperature}°C)")
    elif request.iot_temperature < 37.5:
        score += 25
        fatores.append(f"Hipotermia detectada no IoT ({request.iot_temperature}°C)")

    if request.iot_activity_level == "LETHARGIC":
        score += 20
        fatores.append("Acelerômetro registrou padrão prolongado de letargia")

    # 3. Avaliação da Visão Computacional
    if request.cv_feeding_events_12h == 0:
        score += 15
        fatores.append("Câmera registrou ausência de ida à tigela nas últimas 12h")

    score_final = min(score, 100)

    if score_final >= 70:
        classificacao = "CRÍTICO"
        recomendacao = "Priorizar atendimento imediato. Notificar veterinário plantonista."
        alerta = True
    elif score_final >= 40:
        classificacao = "MODERADO"
        recomendacao = "Solicitar novo check-in em 4 horas e monitorar temperatura."
        alerta = False
    else:
        classificacao = "ESTÁVEL"
        recomendacao = "Quadro normal. Manter rotina de cuidados."
        alerta = False

    return RiskAssessmentResponse(
        pet_id=request.pet_id,
        timestamp=datetime.now().isoformat(),
        score_risco=score_final,
        classificacao=classificacao,
        fatores_detectados=fatores,
        recomendacao_clinica=recomendacao,
        alerta_critico=alerta
    )