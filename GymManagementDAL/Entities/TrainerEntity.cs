using GymManagementDAL.Entities.Enums;


namespace GymManagementDAL.Entities
{
    public class TrainerEntity : GymUser
    {
        public Specialties Specialties { get; set; }

        public ICollection<SessionEntity> Sessions { get; set; } = default!;
    }
}
