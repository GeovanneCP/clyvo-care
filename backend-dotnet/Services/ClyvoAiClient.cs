using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClyvoCare.API.Services;

public record ExtractedClinicalData(
    [property: JsonPropertyName("pet_id")] string PetId,
    [property: JsonPropertyName("comeu_bem")] bool ComeuBem,
    [property: JsonPropertyName("bebeu_agua")] bool BebeuAgua,
    [property: JsonPropertyName("tomou_medicacao")] bool TomouMedicacao,
    [property: JsonPropertyName("nivel_dor")] int NivelDor,
    [property: JsonPropertyName("sintomas")] List<string> Sintomas,
    [property: JsonPropertyName("tutor_ansioso")] bool TutorAnsioso
);

public record RiskAssessmentResponse(
    [property: JsonPropertyName("pet_id")] string PetId,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("score_risco")] int ScoreRisco,
    [property: JsonPropertyName("classificacao")] string Classificacao,
    [property: JsonPropertyName("fatores_detectados")] List<string> FatoresDetectados,
    [property: JsonPropertyName("recomendacao_clinica")] string RecomendacaoClinica,
    [property: JsonPropertyName("alerta_critico")] bool AlertaCritico
);

public class ClyvoAiClient
{
    private readonly HttpClient _httpClient;

    public ClyvoAiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExtractedClinicalData?> ProcessarTriagemAsync(string petId, string mensagemTutor)
    {
        var rawText = (mensagemTutor ?? "").Trim().Trim('"', '\'');

        // Cria exatamente o JSON que funcionou no terminal
        var requestObj = new
        {
            pet_id = petId,
            tutor_message = rawText,
            clinical_history = ""
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestObj),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("/api/v1/ai/triage", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var detalheErro = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"FastAPI triage erro {response.StatusCode}: {detalheErro}");
        }

        var resJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ExtractedClinicalData>(resJson);
    }

    public async Task<RiskAssessmentResponse?> AvaliarRiscoClinicoAsync(
        string petId,
        ExtractedClinicalData clinicalData,
        double temp,
        string activity,
        int feedingEvents)
    {
        var requestObj = new
        {
            pet_id = petId,
            clinical_data = new
            {
                pet_id = clinicalData.PetId,
                comeu_bem = clinicalData.ComeuBem,
                bebeu_agua = clinicalData.BebeuAgua,
                tomou_medicacao = clinicalData.TomouMedicacao,
                nivel_dor = clinicalData.NivelDor,
                sintomas = clinicalData.Sintomas,
                tutor_ansioso = clinicalData.TutorAnsioso
            },
            iot_temperature = temp,
            iot_activity_level = activity,
            cv_feeding_events_12h = feedingEvents
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestObj),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("/api/v1/ai/risk-score", jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            var detalheErro = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"FastAPI risk-score erro {response.StatusCode}: {detalheErro}");
        }

        var resJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RiskAssessmentResponse>(resJson);
    }
}