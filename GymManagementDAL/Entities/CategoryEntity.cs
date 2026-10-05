namespace GymManagementDAL.Entities
{
    public class CategoryEntity : BaseEntity
    {
        public string CategoryName { get; set; } = default!;

        public ICollection<SessionEntity> Sessions { get; set; } = default!;
    }
}
