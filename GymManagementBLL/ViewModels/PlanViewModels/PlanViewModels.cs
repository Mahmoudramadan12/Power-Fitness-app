using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.PlanViewModels
{
    public class PlanViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public int DurationDays { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int ActiveMembers { get; set; }
    }

    public class CreatePlanViewModel
    {
        [Required(ErrorMessage = "Plan name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [Display(Name = "Plan name")]
        public string PlanName { get; set; } = default!;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 200 characters")]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
        [Display(Name = "Duration (days)")]
        public int? DurationDays { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 100000, ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Price (EGP)")]
        public decimal? Price { get; set; }
    }

    public class UpdatePlanViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Plan name is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [Display(Name = "Plan name")]
        public string PlanName { get; set; } = default!;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 200 characters")]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 365, ErrorMessage = "Duration must be between 1 and 365 days")]
        [Display(Name = "Duration (days)")]
        public int? DurationDays { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 100000, ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Price (EGP)")]
        public decimal? Price { get; set; }
    }
}
