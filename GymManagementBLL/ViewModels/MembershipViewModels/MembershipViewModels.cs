using GymManagementBLL.Common;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.MembershipViewModels
{
    public class MembershipListQuery : ListQuery
    {
        /// <summary>null = all memberships.</summary>
        public MembershipStatus? Status { get; set; } = MembershipStatus.Active;
    }

    public class MembershipViewModel
    {
        public int MembershipId { get; set; }
        public int MemberId { get; set; }
        public int PlanId { get; set; }
        public string MemberName { get; set; } = default!;
        public string PlanName { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PricePaid { get; set; }
        public MembershipStatus Status { get; set; }
        public int RemainingDays { get; set; }
    }

    public class CreateMembershipViewModel
    {
        [Required(ErrorMessage = "Select a member")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a member")]
        [Display(Name = "Member")]
        public int? MemberId { get; set; }

        [Required(ErrorMessage = "Select a plan")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a plan")]
        [Display(Name = "Plan")]
        public int? PlanId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }
    }

    public class MemberSelectListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Phone { get; set; }
    }

    public class PlanSelectListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int DurationDays { get; set; }
        public decimal Price { get; set; }
    }
}
