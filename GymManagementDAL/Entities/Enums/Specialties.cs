using System.ComponentModel.DataAnnotations;

namespace GymManagementDAL.Entities.Enums
{
    /// <summary>
    /// A trainer's specialty. Names must match the seeded <see cref="CategoryEntity.CategoryName"/>
    /// values (Yoga, Boxing, CrossFit) because a trainer may only lead sessions of their own category.
    /// </summary>
    public enum Specialties
    {
        [Display(Name = "General Fitness")]
        GeneralFitness = 1,
        Yoga,
        Boxing,
        CrossFit
    }
}
