using Microsoft.AspNetCore.Identity;

namespace GymManagementDAL.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
