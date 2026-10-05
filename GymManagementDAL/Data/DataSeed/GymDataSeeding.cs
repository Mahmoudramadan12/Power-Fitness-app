using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GymManagementDAL.Data.DataSeed
{
    public static class GymDataSeeding
    {
        /// <summary>Seeds reference data (membership plans) from <c>plans.json</c> when the table is empty.</summary>
        public static async Task SeedAsync(
            GymDbContext dbContext,
            string seedFilesPath,
            ILogger logger,
            CancellationToken ct = default)
        {
            try
            {
                if (!await dbContext.Plans.AnyAsync(ct))
                {
                    var plans = LoadDataFromJsonFile<PlanEntity>("plans.json", seedFilesPath);
                    if (plans.Count > 0)
                    {
                        dbContext.Plans.AddRange(plans);
                        await dbContext.SaveChangesAsync(ct);
                        logger.LogInformation("Seeded {Count} plans.", plans.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gym data seeding failed.");
                throw;
            }
        }

        private static List<T> LoadDataFromJsonFile<T>(string fileName, string folderPath)
        {
            var filePath = Path.Combine(folderPath, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Seed data file not found: {filePath}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());

            return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(filePath), options) ?? [];
        }
    }
}
