using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;

namespace GymManagementBLL.ViewModels.AnalyticsViewModels
{
    public class DashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int TotalTrainers { get; set; }
        public int NewMembersThisMonth { get; set; }

        public int UpcomingSessions { get; set; }
        public int OngoingSessions { get; set; }
        public int CompletedSessions { get; set; }

        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }

        /// <summary>Share (0-100) of bookings attended for sessions that ended in the last 30 days; null when there were none.</summary>
        public int? AttendanceRatePercent { get; set; }

        public IReadOnlyList<SessionViewModel> NextSessions { get; set; } = [];
        public IReadOnlyList<MembershipViewModel> ExpiringSoon { get; set; } = [];

        public IReadOnlyList<ChartPoint> PlanDistribution { get; set; } = [];
        public IReadOnlyList<MonthlyPoint> Monthly { get; set; } = [];
    }

    public record ChartPoint(string Label, int Value);

    public record MonthlyPoint(string Label, int NewMemberships, decimal Revenue);
}
