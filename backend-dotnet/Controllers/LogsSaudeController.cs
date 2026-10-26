using ClyvoCare.API.Data;
using ClyvoCare.API.Diagnostics;
using ClyvoCare.API.DTOs;
using ClyvoCare.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClyvoCare.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LogsSaudeController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<LogsSaudeController> _logger;

    public LogsSaudeController(AppDbContext context, ILogger<LogsSaudeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogSaude>>> GetLogSaude()
    {
        _logger.LogInformation("Consultando listagem de logs de saude.");
        return await _context.LogsSaude.ToListAsync();
    }

    [HttpGet("pet/{petId}", Name = "GetLogsPorPet")]
    public async Task<ActionResult<IEnumerable<LogSaude>>> GetLogsPorPet(int petId)
    {
        _logger.LogInformation("Consultando historico de saude para o PetId {PetId}", petId);
        return await _context.LogsSaude
            .Where(l => l.PetId == petId)
            .OrderByDescending(l => l.DataHora)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<LogSaude>> PostLogSaude([FromBody] LogSaudeCreateDTO dto)
    {
        using var activity = TelemetryConfig.ActivitySource.StartActivity("ProcessarRegistroLogSaude");

        var log = new LogSaude
        {
            Peso = dto.Peso,
            Temperatura = dto.Temperatura,
            BatimentosCardiacos = dto.BatimentosCardiacos,
            Observacoes = dto.Observacoes,
            PetId = dto.PetId,
            DataHora = DateTime.Now
        };

        _context.LogsSaude.Add(log);
        await _context.SaveChangesAsync();

        TelemetryConfig.LogsSaudeProcessadosCounter.Add(1);
        _logger.LogInformation("Log de saude registrado com sucesso. Id: {LogId}, PetId: {PetId}", log.Id, log.PetId);

        return CreatedAtRoute("GetLogsPorPet", new { petId = log.PetId }, log);
    }
}