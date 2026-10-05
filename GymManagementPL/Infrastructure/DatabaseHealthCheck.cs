using GymManagementDAL.Data.Contexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GymManagementPL.Infrastructure
{
    public sealed class DatabaseHealthCheck(GymDbContext db) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                return await db.Database.CanConnectAsync(ct)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Cannot connect to the database.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database check failed.", ex);
            }
        }
    }
}
