import pytest
from fastapi.testclient import TestClient
import sys
import os

# Adiciona o diretório ai-engine ao path para importar a app
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
from main import app

client = TestClient(app)

def test_cenario_1_pet_critico():
    """Valida um caso de pet com dor, febre e sem medicação (alerta crítico)."""
    # 1. Triagem
    triage_payload = {
        "pet_id": "pet-101",
        "tutor_message": "Ele está com muita dor na pata traseira, chorando muito e não dei o remédio porque ele vomitou."
    }
    triage_res = client.post("/api/v1/ai/triage", json=triage_payload)
    assert triage_res.status_code == 200
    clinical_data = triage_res.json()
    assert clinical_data["tomou_medicacao"] is False
    assert clinical_data["nivel_dor"] >= 6

    # 2. Avaliação Preditiva
    risk_payload = {
        "pet_id": "pet-101",
        "clinical_data": clinical_data,
        "iot_temperature": 39.8,
        "iot_activity_level": "LETHARGIC",
        "cv_feeding_events_12h": 0
    }
    risk_res = client.post("/api/v1/ai/risk-score", json=risk_payload)
    assert risk_res.status_code == 200
    risk_data = risk_res.json()
    assert risk_data["score_risco"] >= 70
    assert risk_data["classificacao"] == "CRÍTICO"
    assert risk_data["alerta_critico"] is True

def test_cenario_2_pet_estavel():
    """Valida um pet em recuperação pós-operatória estável."""
    triage_payload = {
        "pet_id": "pet-202",
        "tutor_message": "Tudo ótimo hoje, comeu bem a ração e tomou a medicação prescrita certinho."
    }
    triage_res = client.post("/api/v1/ai/triage", json=triage_payload)
    assert triage_res.status_code == 200
    clinical_data = triage_res.json()
    assert clinical_data["comeu_bem"] is True
    assert clinical_data["tomou_medicacao"] is True

    risk_payload = {
        "pet_id": "pet-202",
        "clinical_data": clinical_data,
        "iot_temperature": 38.6,
        "iot_activity_level": "NORMAL",
        "cv_feeding_events_12h": 3
    }
    risk_res = client.post("/api/v1/ai/risk-score", json=risk_payload)
    assert risk_res.status_code == 200
    risk_data = risk_res.json()
    assert risk_data["classificacao"] == "ESTÁVEL"
    assert risk_data["alerta_critico"] is False