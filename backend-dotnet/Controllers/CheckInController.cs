using Microsoft.AspNetCore.Mvc;
using ClyvoCare.API.Services;

namespace ClyvoCare.API.Controllers;

public record CheckInInputDTO(
    string RelatoTutor,
    double Temperatura = 39.5,
    string Atividade = "LETHARGIC",
    int Refeicoes = 0
);

[ApiController]
[Route("api/v1/check-ins")]
public class CheckInController : ControllerBase
{
    private readonly ClyvoAiClient _aiClient;

    public CheckInController(ClyvoAiClient aiClient)
    {
        _aiClient = aiClient;
    }

    [HttpPost("analisar")]
    public async Task<IActionResult> AnalisarCheckIn(
        [FromQuery] string petId,
        [FromBody] CheckInInputDTO dto)
    {
        try
        {
            var dadosClinicos = await _aiClient.ProcessarTriagemAsync(petId, dto.RelatoTutor);
            if (dadosClinicos == null)
            {
                return BadRequest(new { erro = "Não foi possível extrair dados clínicos com a IA." });
            }

            var avaliacaoRisco = await _aiClient.AvaliarRiscoClinicoAsync(
                petId, dadosClinicos, dto.Temperatura, dto.Atividade, dto.Refeicoes);

            return Ok(new
            {
                Status = "Sucesso",
                PetId = petId,
                DadosClinicos = dadosClinicos,
                AvaliacaoRisco = avaliacaoRisco
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(503, new { erro = "Falha de comunicação com o serviço de IA.", detalhe = ex.Message });
        }
    }
}