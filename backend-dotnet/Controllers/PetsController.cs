using ClyvoCare.API.Data.Repositories;
using ClyvoCare.API.DTOs;
using ClyvoCare.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoCare.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PetsController : ControllerBase
{
    private readonly IRepository<Pet> _petRepository;
    private readonly ILogger<PetsController> _logger;

    public PetsController(IRepository<Pet> petRepository, ILogger<PetsController> logger)
    {
        _petRepository = petRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HateoasResponse<Pet>>>> GetPets([FromQuery] PaginationFilter filter)
    {
        _logger.LogInformation("A consultar listagem de pets com paginação e filtros.");

        var paged = await _petRepository.GetPagedAsync(
            filter,
            p => string.IsNullOrEmpty(filter.SearchTerm) || p.Nome.ToLower().Contains(filter.SearchTerm.ToLower())
        );

        var responseItems = paged.Items.Select(pet => GerarHateoas(pet)).ToList();

        return Ok(new PagedResult<HateoasResponse<Pet>>
        {
            Items = responseItems,
            TotalCount = paged.TotalCount,
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize
        });
    }

    [HttpGet("{id}", Name = "GetPetById")]
    public async Task<ActionResult<HateoasResponse<Pet>>> GetPet(int id)
    {
        var pet = await _petRepository.GetByIdAsync(id);
        if (pet == null)
        {
            _logger.LogWarning("Pet com Id {PetId} não encontrado.", id);
            return NotFound();
        }

        return Ok(GerarHateoas(pet));
    }

    [HttpPost]
    public async Task<ActionResult<HateoasResponse<Pet>>> PostPet([FromBody] PetCreateDTO petDto)
    {
        var pet = new Pet
        {
            Nome = petDto.Nome,
            Especie = petDto.Especie,
            DataNascimento = petDto.DataNascimento,
            UsuarioId = petDto.UsuarioId
        };

        await _petRepository.AddAsync(pet);
        _logger.LogInformation("Pet registado com sucesso. Id: {PetId}", pet.Id);

        var hateoas = GerarHateoas(pet);
        return CreatedAtRoute("GetPetById", new { id = pet.Id }, hateoas);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPet(int id, [FromBody] Pet pet)
    {
        if (id != pet.Id)
        {
            _logger.LogWarning("Id de rota {RotaId} diverge do corpo {BodyId}.", id, pet.Id);
            return BadRequest();
        }

        var existente = await _petRepository.GetByIdAsync(id);
        if (existente == null)
        {
            return NotFound();
        }

        await _petRepository.UpdateAsync(pet);
        _logger.LogInformation("Pet {PetId} atualizado com sucesso.", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePet(int id)
    {
        var pet = await _petRepository.GetByIdAsync(id);
        if (pet == null)
        {
            _logger.LogWarning("Tentativa de eliminação falhou. Pet {PetId} não encontrado.", id);
            return NotFound();
        }

        await _petRepository.DeleteAsync(pet);
        _logger.LogInformation("Pet {PetId} removido com sucesso.", id);
        return NoContent();
    }

    private HateoasResponse<Pet> GerarHateoas(Pet pet)
    {
        var item = new HateoasResponse<Pet>(pet);
        item.Links.Add(new LinkDto(Url.Link("GetPetById", new { id = pet.Id }) ?? $"/api/pets/{pet.Id}", "self", "GET"));
        item.Links.Add(new LinkDto($"/api/pets/{pet.Id}", "update_pet", "PUT"));
        item.Links.Add(new LinkDto($"/api/pets/{pet.Id}", "delete_pet", "DELETE"));
        item.Links.Add(new LinkDto($"/api/logssaude/pet/{pet.Id}", "logs_saude", "GET"));
        item.Links.Add(new LinkDto($"/api/telemetriapet/pet/{pet.Id}", "telemetria_nosql", "GET"));
        return item;
    }
}