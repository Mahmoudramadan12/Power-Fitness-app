namespace GymManagementDAL.Data.DataSeed
{
    public static class RoleNames
    {
        public const string SuperAdmin = "SuperAdmin";
        public const string Admin = "Admin";
    }

    /// <summary>Bound from the "Seed" configuration section. Nothing sensitive is hardcoded in source.</summary>
    public sealed class SeedOptions
    {
        public const string SectionName = "Seed";

        public SeedAccount SuperAdmin { get; set; } = new();
        public SeedAccount Admin { get; set; } = new();

        /// <summary>Create sample trainers, members, sessions and memberships (Development only).</summary>
        public bool DemoData { get; set; }
    }

    public sealed class SeedAccount
    {
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
    }
}
