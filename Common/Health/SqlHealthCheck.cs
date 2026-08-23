using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Employee_History.Common.Health
{
    /// <summary>Health check that verifies the SQL Server database is reachable.</summary>
    public class SqlHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;

        public SqlHealthCheck(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync(cancellationToken);
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy("Database reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database unreachable.", ex);
            }
        }
    }
}
