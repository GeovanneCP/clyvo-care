using ClyvoCare.API.Services;
using Xunit;

namespace backend_dotnet.Tests;

public class TriageTests
{
    [Fact]
    public void ExtractedClinicalData_DeveCriarInstanciaComSucesso()
    {
        var data = new ExtractedClinicalData(
            PetId: "pet-bob-01",
            ComeuBem: true,
            BebeuAgua: true,
            TomouMedicacao: false,
            NivelDor: 7,
            Sintomas: new List<string> { "vomito", "chorando" },
            TutorAnsioso: false
        );

        Assert.Equal("pet-bob-01", data.PetId);
        Assert.Equal(7, data.NivelDor);
        Assert.False(data.TomouMedicacao);
        Assert.Contains("vomito", data.Sintomas);
    }
}