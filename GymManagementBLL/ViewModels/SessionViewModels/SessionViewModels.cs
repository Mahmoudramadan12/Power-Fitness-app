using GymManagementBLL.Common;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.SessionViewModels
{
    public class SessionListQuery : ListQuery
    {
        /// <summary>null = all sessions.</summary>
        public SessionStatus? Status { get; set; } = SessionStatus.Upcoming;
        public int? CategoryId { get; set; }
    }

    public class SessionViewModel
    {
        public int Id { get; set; }
        public string Description { get; set; } = default!;
        public int Capacity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = default!;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
        public int BookedCount { get; set; }
        public SessionStatus Status { get; set; }

        public int AvailableSlots => Math.Max(0, Capacity - BookedCount);
        public bool IsFull => BookedCount >= Capacity;
        public int FillPercent => Capacity == 0 ? 0 : Math.Min(100, BookedCount * 100 / Capacity);
        public TimeSpan Duration => EndDate - StartDate;
        public string DurationDisplay =>
            Duration.TotalMinutes % 60 == 0 ? $"{(int)Duration.TotalHours} h" :
            Duration.TotalHours < 1 ? $"{(int)Duration.TotalMinutes} min" :
            $"{(int)Duration.TotalHours} h {Duration.Minutes} min";
    }

    public class CreateSessionViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters")]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 25, ErrorMessage = "Capacity must be between 1 and 25")]
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Starts")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "Ends")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Trainer is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Trainer is required")]
        [Display(Name = "Trainer")]
        public int? TrainerId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate is { } s && EndDate is { } e && e <= s)
                yield return new ValidationResult("End must be after start.", [nameof(EndDate)]);
        }
    }

    public class UpdateSessionViewModel : IValidatableObject
    {
        public int Id { get; set; }

        /// <summary>Display only: the category is fixed once a session exists.</summary>
        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 500 characters")]
        public string Description { get; set; } = default!;

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 25, ErrorMessage = "Capacity must be between 1 and 25")]
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Starts")]
        public DateTime? StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "Ends")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Trainer is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Trainer is required")]
        [Display(Name = "Trainer")]
        public int? TrainerId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (StartDate is { } s && EndDate is { } e && e <= s)
                yield return new ValidationResult("End must be after start.", [nameof(EndDate)]);
        }
    }

    public class TrainerSelectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Specialty { get; set; } = default!;

        /// <summary>Categories this trainer may lead (drives the filtered dropdown on the session form).</summary>
        public IReadOnlyList<int> CategoryIds { get; set; } = [];
    }

    public class CategorySelectViewModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = default!;
    }
}
