using GymManagementBLL.Services.AttachmentService;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.Interfaces;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Data.DataSeed;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Classes;
using GymManagementDAL.Repositories.Interfaces;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

namespace GymManagementPL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string LoginRateLimitPolicy = "login";

        public static IServiceCollection AddGymApplication(
            this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.Configure<GymOptions>(configuration.GetSection(GymOptions.SectionName));

            services.AddControllersWithViews(options =>
            {
                // Every POST/PUT/DELETE must carry a valid antiforgery token - no per-action opt-in to forget.
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            services.AddGymClock(configuration);
            services.AddGymData(configuration);
            services.AddGymIdentity(environment);
            services.AddGymServices();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(LoginRateLimitPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

            // Containers lose their data-protection keys on restart, which logs everyone out; persist them when asked to.
            var keysPath = configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(keysPath))
                services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("GymManagement");

            return services;
        }

        private static void AddGymClock(this IServiceCollection services, IConfiguration configuration)
        {
            var zoneId = configuration[$"{GymOptions.SectionName}:TimeZone"] ?? "Africa/Cairo";
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                throw new InvalidOperationException(
                    $"Gym:TimeZone '{zoneId}' is not a valid time zone on this machine (install tzdata in containers).", ex);
            }

            services.AddSingleton<TimeProvider>(new GymTimeProvider(zone));
        }

        private static void AddGymData(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is not configured. Set it with user-secrets " +
                    "('dotnet user-secrets set ConnectionStrings:DefaultConnection ...') or the " +
                    "ConnectionStrings__DefaultConnection environment variable.");

            services.AddDbContext<GymDbContext>(options =>
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

            services.AddScoped<IMembershipRepository, MembershipRepository>();
            services.AddScoped<ISessionRepository, SessionRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        private static void AddGymIdentity(this IServiceCollection services, IHostEnvironment environment)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequiredUniqueChars = 4;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                    options.Lockout.AllowedForNewUsers = true;
                })
                .AddEntityFrameworkStores<GymDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "PowerFitness.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });
        }

        private static void AddGymServices(this IServiceCollection services)
        {
            services.AddScoped<IMemberService, MemberService>();
            services.AddScoped<ITrainerService, TrainerService>();
            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IAnalyticsService, AnalyticsService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
        }

        /// <summary>Binds the "Seed" section (bound here so the data layer has no configuration dependency).</summary>
        public static SeedOptions GetSeedOptions(this IConfiguration configuration) =>
            configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
    }
}
