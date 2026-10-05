using GymManagement.Tests.Fakes;
using GymManagementBLL.Common;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.ViewModels.MembershipViewModels;

namespace GymManagement.Tests
{
    public class MembershipServiceTests
    {
        private readonly World w = new();
        private MembershipService Sut => new(w.Uow, w.Clock);

        [Fact]
        public async Task Creating_a_membership_snapshots_price_and_runs_whole_days_from_midnight()
        {
            var member = w.AddMember();
            var plan = w.AddPlan(days: 30, price: 750);

            var result = await Sut.CreateMembershipAsync(new CreateMembershipViewModel { MemberId = member.Id, PlanId = plan.Id });

            Assert.True(result.Success, result.Error);
            var m = Assert.Single(w.Db.Memberships);
            Assert.Equal(w.Now.Date, m.CreatedAt);                 // start date is stored as the start, at midnight
            Assert.Equal(w.Now.Date.AddDays(30), m.EndDate);
            Assert.Equal(750m, m.PricePaid);

            plan.Price = 999;                                      // later price change must not rewrite history
            Assert.Equal(750m, m.PricePaid);
        }

        [Fact]
        public async Task A_future_start_date_is_honoured()
        {
            var member = w.AddMember(); var plan = w.AddPlan(days: 10);
            var start = w.Now.Date.AddDays(14);

            await Sut.CreateMembershipAsync(new CreateMembershipViewModel { MemberId = member.Id, PlanId = plan.Id, StartDate = start });

            var m = Assert.Single(w.Db.Memberships);
            Assert.Equal(start, m.StartDate);
            Assert.Equal(start.AddDays(10), m.EndDate);
            Assert.False(m.IsActiveAt(w.Now));
            Assert.True(m.IsUpcomingAt(w.Now));
        }

        [Fact]
        public async Task A_start_date_in_the_past_is_rejected()
        {
            var member = w.AddMember(); var plan = w.AddPlan();

            var result = await Sut.CreateMembershipAsync(new CreateMembershipViewModel
            { MemberId = member.Id, PlanId = plan.Id, StartDate = w.Now.Date.AddDays(-1) });

            Assert.False(result.Success);
            Assert.Empty(w.Db.Memberships);
        }

        [Fact]
        public async Task A_member_cannot_hold_overlapping_memberships_but_can_queue_the_next_one()
        {
            var member = w.AddMember(); var plan = w.AddPlan(days: 30);
            w.AddMembership(member, plan, startOffsetDays: 0);                         // runs today .. +30d

            var overlapping = await Sut.CreateMembershipAsync(new CreateMembershipViewModel
            { MemberId = member.Id, PlanId = plan.Id, StartDate = w.Now.Date.AddDays(10) });
            var next = await Sut.CreateMembershipAsync(new CreateMembershipViewModel
            { MemberId = member.Id, PlanId = plan.Id, StartDate = w.Now.Date.AddDays(30) });

            Assert.False(overlapping.Success);
            Assert.True(next.Success, next.Error);
            Assert.Equal(2, w.Db.Memberships.Count);
        }

        [Fact]
        public async Task Inactive_plans_and_unknown_members_are_rejected()
        {
            var member = w.AddMember();
            var inactive = w.AddPlan("Old", active: false);

            Assert.False((await Sut.CreateMembershipAsync(new CreateMembershipViewModel { MemberId = member.Id, PlanId = inactive.Id })).Success);
            Assert.Equal(ResultKind.NotFound, (await Sut.CreateMembershipAsync(new CreateMembershipViewModel { MemberId = 999, PlanId = inactive.Id })).Kind);
        }

        [Fact]
        public async Task Ending_an_active_membership_keeps_the_record_and_ends_it_now()
        {
            var member = w.AddMember(); var plan = w.AddPlan(price: 600);
            var m = w.AddMembership(member, plan, startOffsetDays: -5);

            var result = await Sut.CancelMembershipAsync(m.Id);

            Assert.True(result.Success);
            Assert.Equal(w.Now, m.EndDate);
            Assert.Equal(600m, m.PricePaid);
            Assert.Equal(1, w.Db.Memberships.Count);
            Assert.False(m.IsActiveAt(w.Now));
        }

        [Fact]
        public async Task Cancelling_a_membership_that_has_not_started_removes_it()
        {
            var member = w.AddMember(); var plan = w.AddPlan();
            var m = w.AddMembership(member, plan, startOffsetDays: 7);

            Assert.True((await Sut.CancelMembershipAsync(m.Id)).Success);
            Assert.Empty(w.Db.Memberships);
        }

        [Fact]
        public async Task An_expired_membership_cannot_be_cancelled()
        {
            var member = w.AddMember(); var plan = w.AddPlan(days: 5);
            var m = w.AddMembership(member, plan, startOffsetDays: -30);

            Assert.False((await Sut.CancelMembershipAsync(m.Id)).Success);
            Assert.Equal(ResultKind.NotFound, (await Sut.CancelMembershipAsync(12345)).Kind);
        }

        [Fact]
        public async Task The_list_filters_by_status_and_search_term()
        {
            var plan = w.AddPlan(days: 30);
            var alice = w.AddMember("Alice"); var bob = w.AddMember("Bob"); var carol = w.AddMember("Carol");
            w.AddMembership(alice, plan, startOffsetDays: -2);                 // active
            w.AddMembership(bob, plan, startOffsetDays: 5);                    // upcoming
            w.AddMembership(carol, plan, startOffsetDays: -60);                // expired

            var active = await Sut.GetMembershipsAsync(new MembershipListQuery { Status = MembershipStatus.Active });
            var upcoming = await Sut.GetMembershipsAsync(new MembershipListQuery { Status = MembershipStatus.Upcoming });
            var expired = await Sut.GetMembershipsAsync(new MembershipListQuery { Status = MembershipStatus.Expired });
            var all = await Sut.GetMembershipsAsync(new MembershipListQuery { Status = null });
            var searched = await Sut.GetMembershipsAsync(new MembershipListQuery { Status = null, Search = "bo" });

            Assert.Equal("Alice", Assert.Single(active.Items).MemberName);
            Assert.Equal("Bob", Assert.Single(upcoming.Items).MemberName);
            Assert.Equal("Carol", Assert.Single(expired.Items).MemberName);
            Assert.Equal(3, all.TotalCount);
            Assert.Equal("Bob", Assert.Single(searched.Items).MemberName);
        }

        [Fact]
        public async Task Remaining_days_round_up_and_status_is_reported()
        {
            var member = w.AddMember(); var plan = w.AddPlan(days: 10);
            w.AddMembership(member, plan, startOffsetDays: -3);            // ends in 7 days at 00:00 -> 6d14h left

            var vm = Assert.Single((await Sut.GetMembershipsAsync(new MembershipListQuery())).Items);

            Assert.Equal(MembershipStatus.Active, vm.Status);
            Assert.Equal(7, vm.RemainingDays);
        }
    }
}
