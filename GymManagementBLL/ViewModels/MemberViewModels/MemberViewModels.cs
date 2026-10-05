using GymManagementBLL.Common;
using GymManagementBLL.Validation;
using GymManagementDAL.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.MemberViewModels
{
    /// <summary>A row in the members list.</summary>
    public class MemberViewModel
    {
        public int Id { get; set; }
        public string? Photo { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Gender { get; set; } = default!;
        public string? PlanName { get; set; }
        public DateTime? MembershipEndDate { get; set; }
        public bool HasActiveMembership => PlanName is not null;
    }

    public class MembershipSummaryViewModel
    {
        public int MembershipId { get; set; }
        public string PlanName { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PricePaid { get; set; }
        public MembershipStatus Status { get; set; }
        public int RemainingDays { get; set; }
    }

    public class MemberBookingViewModel
    {
        public int SessionId { get; set; }
        public string CategoryName { get; set; } = default!;
        public string TrainerName { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsAttended { get; set; }
        public SessionStatus Status { get; set; }
    }

    public class MemberDetailsViewModel
    {
        public int Id { get; set; }
        public string? Photo { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Gender { get; set; } = default!;
        public DateOnly DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Address { get; set; } = default!;
        public DateTime JoinDate { get; set; }

        public MembershipSummaryViewModel? CurrentMembership { get; set; }
        public IReadOnlyList<MembershipSummaryViewModel> MembershipHistory { get; set; } = [];
        public HealthRecordViewModel? HealthRecord { get; set; }
        public IReadOnlyList<MemberBookingViewModel> RecentBookings { get; set; } = [];
    }

    public class CreateMemberViewModel
    {
        [Display(Name = "Profile photo (optional)")]
        [ImageUpload(5)]
        public IFormFile? PhotoFile { get; set; }

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
        [DateOfBirth]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public Gender? Gender { get; set; }

        [Required(ErrorMessage = "Building number is required")]
        [Range(1, 9000, ErrorMessage = "Building number must be between 1 and 9000")]
        [Display(Name = "Building number")]
        public int? BuildingNumber { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
        [RegularExpression(ValidationPatterns.City, ErrorMessage = "City can only contain letters, spaces, and . ' -")]
        public string City { get; set; } = default!;

        [Required(ErrorMessage = "Street is required")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Street must be between 2 and 150 characters")]
        [RegularExpression(ValidationPatterns.Street, ErrorMessage = "Street contains invalid characters")]
        public string Street { get; set; } = default!;

        [Required]
        public HealthRecordViewModel HealthRecordViewModel { get; set; } = new();
    }

    public class MemberToUpdateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [RegularExpression(ValidationPatterns.Name, ErrorMessage = "Name can only contain letters, spaces, and . ' -")]
        public string Name { get; set; } = default!;

        /// <summary>Current stored photo (display only).</summary>
        public string? Photo { get; set; }

        [Display(Name = "Replace photo")]
        [ImageUpload(5)]
        public IFormFile? PhotoFile { get; set; }

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
    }

    public class HealthRecordViewModel
    {
        /// <summary>Display only (not bound from forms).</summary>
        public int MemberId { get; set; }
        public string? MemberName { get; set; }

        [Required(ErrorMessage = "Height is required")]
        [Range(50, 260, ErrorMessage = "Height must be between 50 and 260 cm")]
        [Display(Name = "Height (cm)")]
        public decimal? Height { get; set; }

        [Required(ErrorMessage = "Weight is required")]
        [Range(20, 400, ErrorMessage = "Weight must be between 20 and 400 kg")]
        [Display(Name = "Weight (kg)")]
        public decimal? Weight { get; set; }

        [Required(ErrorMessage = "Blood type is required")]
        [RegularExpression(ValidationPatterns.BloodType, ErrorMessage = "Choose a valid blood type")]
        [Display(Name = "Blood type")]
        public string BloodType { get; set; } = default!;

        [StringLength(500, ErrorMessage = "Notes can be up to 500 characters")]
        public string? Note { get; set; }

        public decimal? Bmi => Height is > 0 && Weight is > 0
            ? Math.Round(Weight.Value / (Height.Value / 100 * (Height.Value / 100)), 1)
            : null;
    }
}
