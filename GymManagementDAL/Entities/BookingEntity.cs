namespace GymManagementDAL.Entities
{
    public class BookingEntity : BaseEntity
    {
        public bool IsAttended { get; set; } = false;
        public int MemberId { get; set; }
        public MemberEntity Member { get; set; } = default!;
        public int SessionId { get; set; }
        public SessionEntity Session { get; set; } = default!;
    }
}
