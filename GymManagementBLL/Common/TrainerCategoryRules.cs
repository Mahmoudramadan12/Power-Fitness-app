using GymManagementDAL.Entities.Enums;

namespace GymManagementBLL.Common
{
    /// <summary>
    /// Which session categories a trainer may lead.
    /// The original code required <c>Specialty == CategoryName</c>, which made the "Cardio" and "Strength"
    /// categories impossible to schedule (no such specialty exists) and left General Fitness trainers
    /// unable to lead anything. General Fitness trainers now lead Cardio and Strength.
    /// </summary>
    public static class TrainerCategoryRules
    {
        public static bool CanLead(Specialties specialty, string categoryName) => specialty switch
        {
            Specialties.GeneralFitness =>
                categoryName.Equals("Cardio", StringComparison.OrdinalIgnoreCase) ||
                categoryName.Equals("Strength", StringComparison.OrdinalIgnoreCase),
            _ => specialty.ToString().Equals(categoryName, StringComparison.OrdinalIgnoreCase)
        };
    }
}
