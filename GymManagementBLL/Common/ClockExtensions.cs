namespace GymManagementBLL.Common
{
    public static class ClockExtensions
    {
        /// <summary>
        /// The gym's current local wall-clock time. All stored dates are gym-local, so every service
        /// reads "now" from here (never from DateTime.Now) which also makes the logic testable.
        /// </summary>
        public static DateTime Now(this TimeProvider time) => time.GetLocalNow().DateTime;

        public static int AgeOn(this DateOnly dateOfBirth, DateTime now)
        {
            var today = DateOnly.FromDateTime(now);
            var age = today.Year - dateOfBirth.Year;
            return dateOfBirth > today.AddYears(-age) ? age - 1 : age;
        }
    }
}
