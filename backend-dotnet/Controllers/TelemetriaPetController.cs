using ClyvoCare.API.Data.Repositories;
using ClyvoCare.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoCare.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TelemetriaPetController : ControllerBase
{
    private readonly ITelemetriaMongoRepository _mongoRepo;

    public TelemetriaPetController(ITelemetriaMongoRepository mongoRepo)
    {
        _mongoRepo = mongoRepo;
    }

    [HttpPost]
    public async Task<IActionResult> InserirTelemetria([FromBody] PetTelemetriaNoSql telemetria)
    {
        telemetria.DataRegistro = DateTime.UtcNow;
        await _mongoRepo.InserirTelemetriaAsync(telemetria);
        return Created("", telemetria);
    }

    [HttpGet("pet/{petId}")]
    public async Task<IActionResult> ObterPorPet(int petId)
    {
        var logs = await _mongoRepo.ObterPorPetIdAsync(petId);
        return Ok(logs);
    }
}