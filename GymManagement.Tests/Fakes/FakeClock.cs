namespace GymManagement.Tests.Fakes
{
    /// <summary>A controllable clock. "Local" time equals UTC so tests can reason in plain wall-clock terms.</summary>
    public sealed class FakeClock(DateTime now) : TimeProvider
    {
        public DateTime Now { get; set; } = now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
    }
}
