using GymManagementBLL.Common;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using System.ComponentModel.DataAnnotations;

namespace GymManagementBLL.ViewModels.BookingViewModels
{
    public class CreateBookingViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a member")]
        public int MemberId { get; set; }

        [Range(1, int.MaxValue)]
        public int SessionId { get; set; }
    }

    public class MemberForSessionViewModel
    {
        public int MemberId { get; set; }
        public int SessionId { get; set; }
        public string MemberName { get; set; } = default!;
        public DateTime BookedAt { get; set; }
        public bool IsAttended { get; set; }
    }

    /// <summary>Everything the roster page needs: the session, who is booked, and who could still be added.</summary>
    public class SessionRosterViewModel
    {
        public SessionViewModel Session { get; set; } = default!;
        public IReadOnlyList<MemberForSessionViewModel> Members { get; set; } = [];
        public IReadOnlyList<MemberSelectListViewModel> EligibleMembers { get; set; } = [];

        public bool CanBook => Session.Status == SessionStatus.Upcoming && !Session.IsFull;
        public bool CanCancel => Session.Status == SessionStatus.Upcoming;
        public bool CanMarkAttendance => Session.Status == SessionStatus.Ongoing;
    }
}
