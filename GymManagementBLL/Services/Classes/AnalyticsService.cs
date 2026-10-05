using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.AnalyticsViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System.Globalization;

namespace GymManagementBLL.Services.Classes
{
    public class AnalyticsService(IUnitOfWork unitOfWork, TimeProvider time) : IAnalyticsService
    {
        public async Task<DashboardViewModel> GetDashboardAsync(CancellationToken ct = default)
        {
            var now = time.Now();
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);
            var nextMonthStart = monthStart.AddMonths(1);
            var sixMonthsAgo = monthStart.AddMonths(-5);
            var weekAhead = now.AddDays(7);
            var thirtyDaysAgo = now.AddDays(-30);

            var sessions = unitOfWork.SessionRepository;
            var memberships = unitOfWork.MembershipRepository;

            var activeMemberIds = await memberships.SelectAsync(
                m => m.MemberId, m => m.CreatedAt <= now && m.EndDate > now, ct);

            var nextSessions = await sessions.GetWithTrainerAndCategoryAsync(
                s => s.EndDate >= now, newestFirst: false, take: 5, ct);
            var counts = await sessions.GetBookedCountsAsync(nextSessions.Select(s => s.Id).ToList(), ct);

            var expiring = await memberships.GetWithMemberAndPlanAsync(
                m => m.CreatedAt <= now && m.EndDate > now && m.EndDate <= weekAhead,
                q => q.OrderBy(m => m.EndDate), take: 5, ct);

            var planNames = await memberships.SelectAsync(
                m => m.Plan.Name, m => m.CreatedAt <= now && m.EndDate > now, ct);

            var recent = await memberships.SelectAsync(
                m => new { m.CreatedAt, m.PricePaid }, m => m.CreatedAt >= sixMonthsAgo && m.CreatedAt < nextMonthStart, ct);

            var attendanceBase = await unitOfWork.BookingRepository.CountAsync(
                b => b.Session.EndDate < now && b.Session.EndDate >= thirtyDaysAgo, ct);
            var attended = attendanceBase == 0 ? 0 : await unitOfWork.BookingRepository.CountAsync(
                b => b.IsAttended && b.Session.EndDate < now && b.Session.EndDate >= thirtyDaysAgo, ct);

            var monthly = Enumerable.Range(0, 6).Select(i => sixMonthsAgo.AddMonths(i)).Select(month =>
            {
                var rows = recent.Where(r => r.CreatedAt.Year == month.Year && r.CreatedAt.Month == month.Month).ToList();
                return new MonthlyPoint(month.ToString("MMM", CultureInfo.InvariantCulture), rows.Count, rows.Sum(r => r.PricePaid));
            }).ToList();

            return new DashboardViewModel
            {
                TotalMembers = await unitOfWork.GetRepository<MemberEntity>().CountAsync(ct: ct),
                TotalTrainers = await unitOfWork.GetRepository<TrainerEntity>().CountAsync(ct: ct),
                ActiveMembers = activeMemberIds.Distinct().Count(),
                NewMembersThisMonth = await unitOfWork.GetRepository<MemberEntity>().CountAsync(m => m.CreatedAt >= monthStart, ct),

                UpcomingSessions = await sessions.CountAsync(s => s.StartDate > now, ct),
                OngoingSessions = await sessions.CountAsync(s => s.StartDate <= now && s.EndDate >= now, ct),
                CompletedSessions = await sessions.CountAsync(s => s.EndDate < now, ct),

                // Revenue is recognised in the month a membership starts, using the price actually paid.
                RevenueThisMonth = await memberships.SumAsync(m => m.PricePaid, m => m.CreatedAt >= monthStart && m.CreatedAt < nextMonthStart, ct),
                RevenueLastMonth = await memberships.SumAsync(m => m.PricePaid, m => m.CreatedAt >= lastMonthStart && m.CreatedAt < monthStart, ct),

                AttendanceRatePercent = attendanceBase == 0 ? null : (int)Math.Round(attended * 100.0 / attendanceBase),

                NextSessions = nextSessions.Select(s => s.ToViewModel(counts.GetValueOrDefault(s.Id), now)).ToList(),
                ExpiringSoon = expiring.Select(m => m.ToViewModel(now)).ToList(),
                PlanDistribution = planNames.GroupBy(n => n).OrderByDescending(g => g.Count())
                                            .Select(g => new ChartPoint(g.Key, g.Count())).ToList(),
                Monthly = monthly
            };
        }
    }
}
