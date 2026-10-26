using ClyvoCare.API.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClyvoCare.API.HealthChecks;

public class OracleDbHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public OracleDbHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                return HealthCheckResult.Healthy("Conexao com banco Oracle estabelecida com sucesso.");
            }

            return HealthCheckResult.Unhealthy("Nao foi possivel conectar ao banco Oracle.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao validar conexao com o banco Oracle.", ex);
        }
    }
}