using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.AccountViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Enter your current password")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = default!;

        [Required(ErrorMessage = "Enter a new password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters")]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = default!;

        [Required(ErrorMessage = "Confirm the new password")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = default!;
    }
}
