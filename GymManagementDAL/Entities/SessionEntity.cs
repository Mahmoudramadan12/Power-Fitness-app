namespace GymManagementDAL.Entities
{
    public class SessionEntity : BaseEntity
    {

        public string Description { get; set; } = default!;
        public int Capacity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ICollection<BookingEntity> SessionMembers { get; set; } = default!;
        public int TrainerId { get; set; }
        public TrainerEntity Trainer { get; set; } = default!;

        public int CategoryId { get; set; }
        public CategoryEntity Category { get; set; } = default!;
    }
}
