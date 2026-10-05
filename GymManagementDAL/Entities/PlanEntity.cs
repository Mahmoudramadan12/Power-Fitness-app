namespace GymManagementDAL.Entities
{
    public class PlanEntity : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int DurationDays { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

        public ICollection<MembershipEntity> PlanMembers { get; set; } = default!;
    }
}
