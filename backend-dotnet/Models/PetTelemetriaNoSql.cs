using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ClyvoCare.API.Models;

public class PetTelemetriaNoSql
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int PetId { get; set; }
    public decimal Temperatura { get; set; }
    public int Batimentos { get; set; }
    public string StatusAlerta { get; set; } = "Normal";
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
}