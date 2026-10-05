using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.Validation
{
    /// <summary>A date of birth that is in the past and implies an age within a sensible range.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DateOfBirthAttribute(int minimumAge = 12, int maximumAge = 100) : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not DateOnly dob) return ValidationResult.Success;   // [Required] handles missing

            var members = validationContext.MemberName is { } name ? new[] { name } : null;
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (dob > today.AddYears(-minimumAge))
                return new ValidationResult($"Must be at least {minimumAge} years old.", members);
            if (dob < today.AddYears(-maximumAge))
                return new ValidationResult("Please enter a valid date of birth.", members);
            return ValidationResult.Success;
        }
    }

    /// <summary>Optional image upload: size and extension are checked here; content is verified again on save.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ImageUploadAttribute(int maxMegabytes = 5) : ValidationAttribute
    {
        private static readonly string[] Allowed = [".jpg", ".jpeg", ".png"];

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IFormFile { Length: > 0 } file) return ValidationResult.Success;

            var members = validationContext.MemberName is { } name ? new[] { name } : null;
            if (file.Length > maxMegabytes * 1024L * 1024L)
                return new ValidationResult($"The photo must be {maxMegabytes} MB or smaller.", members);

            var extension = Path.GetExtension(file.FileName);
            if (!Allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
                return new ValidationResult("Only JPG and PNG images are allowed.", members);

            return ValidationResult.Success;
        }
    }
}
