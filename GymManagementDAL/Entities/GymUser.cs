using GymManagementDAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymManagementDAL.Entities
{
    [Owned]
    public class Address
    {
        public int BuildingNumber { get; set; }
        public string City { get; set; } = default!;
        public string Street { get; set; } = default!;
    }

    /// <summary>Shared shape of people the gym manages (members and trainers).</summary>
    public abstract class GymUser : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public DateOnly DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public Address Address { get; set; } = default!;
    }
}
