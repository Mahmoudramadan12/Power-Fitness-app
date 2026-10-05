namespace GymManagementDAL.Entities
{
    public class HealthRecordEntity : BaseEntity
    {
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public string BloodType { get; set; } = default!;
        public string? Note { get; set; }

        public MemberEntity Member { get; set; } = default!;
        public int MemberId { get; set; }
    }
}
