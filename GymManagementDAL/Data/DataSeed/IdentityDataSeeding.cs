using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace GymManagementDAL.Data.DataSeed
{
    public static class IdentityDataSeeding
    {
        /// <summary>
        /// Ensures the roles exist and creates the configured accounts that are missing.
        /// Existing users are never modified, and an account without a configured password is skipped
        /// (so a fresh production database never ends up with a well-known default login).
        /// </summary>
        public static async Task SeedAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            SeedOptions options,
            ILogger logger)
        {
            foreach (var roleName in new[] { RoleNames.SuperAdmin, RoleNames.Admin })
            {
                if (await roleManager.RoleExistsAsync(roleName)) continue;

                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                    logger.LogError("Failed to create role {Role}: {Errors}", roleName, Describe(result));
            }

            await EnsureAccountAsync(userManager, options.SuperAdmin, RoleNames.SuperAdmin, logger);
            await EnsureAccountAsync(userManager, options.Admin, RoleNames.Admin, logger);
        }

        private static async Task EnsureAccountAsync(
            UserManager<ApplicationUser> userManager, SeedAccount account, string role, ILogger logger)
        {
            if (!account.IsConfigured)
            {
                logger.LogWarning(
                    "No Seed:{Role} email/password configured - skipping. Set them via environment variables or user-secrets to create the first login.",
                    role);
                return;
            }

            if (await userManager.FindByEmailAsync(account.Email!) is not null) return;

            var user = new ApplicationUser
            {
                FirstName = account.FirstName ?? role,
                LastName = account.LastName ?? string.Empty,
                UserName = string.IsNullOrWhiteSpace(account.UserName) ? account.Email : account.UserName,
                Email = account.Email,
                EmailConfirmed = true,
                PhoneNumber = account.PhoneNumber
            };

            var create = await userManager.CreateAsync(user, account.Password!);
            if (!create.Succeeded)
            {
                logger.LogError("Failed to create {Role} {Email}: {Errors}", role, account.Email, Describe(create));
                return;
            }

            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                logger.LogError("Failed to assign role {Role} to {Email}: {Errors}", role, account.Email, Describe(addRole));
                return;
            }

            logger.LogInformation("Seeded {Role} account {Email}.", role, account.Email);
        }

        private static string Describe(IdentityResult result) =>
            string.Join("; ", result.Errors.Select(e => e.Description));
    }
}
