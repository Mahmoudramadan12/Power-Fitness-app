namespace GymManagementDAL.Entities
{
    public class MemberEntity : GymUser
    {
        /// <summary>Stored file name of the profile picture, or <c>null</c> when none was uploaded.</summary>
        public string? Photo { get; set; }
        public HealthRecordEntity HealthRecord { get; set; } = default!;
        public ICollection<BookingEntity> MemberSessions { get; set; } = default!;
        public ICollection<MembershipEntity> MemberPlans { get; set; } = default!;
    }
}
