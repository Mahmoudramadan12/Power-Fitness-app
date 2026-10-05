using GymManagementBLL.Common;
using GymManagementBLL.Validation;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementDAL.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.TrainerViewModels
{
    public class TrainerListQuery : ListQuery
    {
        public Specialties? Specialty { get; set; }
    }

    public class TrainerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public DateOnly DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = default!;
        public string Address { get; set; } = default!;
        public Specialties Specialty { get; set; }
        public string SpecialtyName => EnumDisplay.Name(Specialty);
        public DateTime HireDate { get; set; }
    }

    public class TrainerDetailsViewModel : TrainerViewModel
    {
        public IReadOnlyList<SessionViewModel> UpcomingSessions { get; set; } = [];
        public int TotalSessions { get; set; }
    }

    public class CreateTrainerViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Name can only contain letters, spaces, and . ' -")]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(ValidationPatterns.EgyptianMobile, ErrorMessage = "Enter an 11-digit Egyptian mobile number (010, 011, 012 or 015)")]
        public string Phone { get; set; } = default!;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        [DateOfBirth(18, 80)]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public Gender? Gender { get; set; }

        [Required(ErrorMessage = "Building number is required")]
        [Range(1, 9000, ErrorMessage = "Building number must be between 1 and 9000")]
        [Display(Name = "Building number")]
        public int? BuildingNumber { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, MinimumLength = 2)]
        [RegularExpression(ValidationPatterns.City, ErrorMessage = "City can only contain letters, spaces, and . ' -")]
        public string City { get; set; } = default!;

        [Required(ErrorMessage = "Street is required")]
        [StringLength(150, MinimumLength = 2)]
        [RegularExpression(ValidationPatterns.Street, ErrorMessage = "Street contains invalid characters")]
        public string Street { get; set; } = default!;

        [Required(ErrorMessage = "Specialty is required")]
        public Specialties? Specialty { get; set; }
    }

    public class TrainerToUpdateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Name can only contain letters, spaces, and . ' -")]
        public string Name { get; set; } = default!;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(ValidationPatterns.EgyptianMobile, ErrorMessage = "Enter an 11-digit Egyptian mobile number (010, 011, 012 or 015)")]
        public string Phone { get; set; } = default!;

        [Required(ErrorMessage = "Building number is required")]
        [Range(1, 9000, ErrorMessage = "Building number must be between 1 and 9000")]
        [Display(Name = "Building number")]
        public int? BuildingNumber { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, MinimumLength = 2)]
        [RegularExpression(ValidationPatterns.City, ErrorMessage = "City can only contain letters, spaces, and . ' -")]
        public string City { get; set; } = default!;

        [Required(ErrorMessage = "Street is required")]
        [StringLength(150, MinimumLength = 2)]
        [RegularExpression(ValidationPatterns.Street, ErrorMessage = "Street contains invalid characters")]
        public string Street { get; set; } = default!;

        [Required(ErrorMessage = "Specialty is required")]
        public Specialties? Specialty { get; set; }
    }
}
