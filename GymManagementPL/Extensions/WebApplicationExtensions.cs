using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Data.DataSeed;
using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManagementPL.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>Applies pending migrations and seeds reference data, accounts and (in Development) demo data.</summary>
        public static async Task MigrateAndSeedAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var sp = scope.ServiceProvider;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

            try
            {
                var db = sp.GetRequiredService<GymDbContext>();

                if (app.Configuration.GetValue("Database:AutoMigrate", true))
                    await db.Database.MigrateAsync();

                var seed = app.Configuration.GetSeedOptions();

                await GymDataSeeding.SeedAsync(db, Path.Combine(app.Environment.WebRootPath, "Files"), logger);

                await IdentityDataSeeding.SeedAsync(
                    sp.GetRequiredService<RoleManager<IdentityRole>>(),
                    sp.GetRequiredService<UserManager<ApplicationUser>>(),
                    seed, logger);

                if (seed.DemoData && app.Environment.IsDevelopment())
                    await DemoDataSeeding.SeedAsync(db, sp.GetRequiredService<TimeProvider>(), logger);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Database migration/seeding failed. Check the connection string and that SQL Server is reachable.");
                throw;
            }
        }

        public static WebApplication UseGymPipeline(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");
            app.UseHttpsRedirection();
            app.UseSecurityHeaders();

            app.MapStaticAssets();

            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/health");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            return app;
        }

        private static IApplicationBuilder UseSecurityHeaders(this WebApplication app)
        {
            // The app serves everything itself (no CDNs, no inline scripts), so a tight CSP is possible.
            // Skipped in Development because hot-reload / browser-refresh inject their own scripts.
            var csp = "default-src 'self'; img-src 'self' data:; font-src 'self'; style-src 'self' 'unsafe-inline'; " +
                      "script-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
            var isDev = app.Environment.IsDevelopment();

            return app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                if (!isDev) headers["Content-Security-Policy"] = csp;
                await next();
            });
        }
    }
}
