using ClyvoCare.API.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ClyvoCare.API.Data.Repositories;

public interface ITelemetriaMongoRepository
{
    Task InserirTelemetriaAsync(PetTelemetriaNoSql telemetria);
    Task<IEnumerable<PetTelemetriaNoSql>> ObterPorPetIdAsync(int petId);
}

public class TelemetriaMongoRepository : ITelemetriaMongoRepository
{
    private readonly IMongoCollection<PetTelemetriaNoSql> _collection;

    public TelemetriaMongoRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDbConnection");
        var databaseName = configuration.GetValue<string>("MongoDbDatabaseName") ?? "ClyvoCareNoSqlDb";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<PetTelemetriaNoSql>("TelemetriasPet");
    }

    public async Task InserirTelemetriaAsync(PetTelemetriaNoSql telemetria)
    {
        await _collection.InsertOneAsync(telemetria);
    }

    public async Task<IEnumerable<PetTelemetriaNoSql>> ObterPorPetIdAsync(int petId)
    {
        return await _collection.Find(x => x.PetId == petId).SortByDescending(x => x.DataRegistro).ToListAsync();
    }
}