namespace GymManagementPL.Infrastructure
{
    public sealed class GymOptions
    {
        public const string SectionName = "Gym";

        /// <summary>Brand name shown in the UI.</summary>
        public string Name { get; set; } = "Power Fitness";

        /// <summary>IANA ("Africa/Cairo") or Windows id. All scheduling is in this zone, not the server's.</summary>
        public string TimeZone { get; set; } = "Africa/Cairo";
    }

    /// <summary>A <see cref="TimeProvider"/> whose "local" time is the gym's time zone.</summary>
    public sealed class GymTimeProvider(TimeZoneInfo zone) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => zone;
    }
}
