using GymManagement.Tests.Fakes;
using GymManagementBLL.Common;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementDAL.Entities.Enums;

namespace GymManagement.Tests
{
    public class BookingServiceTests
    {
        private readonly World w = new();
        private BookingService Sut => new(w.Uow, w.Clock);

        private static CreateBookingViewModel Book(int memberId, int sessionId) => new() { MemberId = memberId, SessionId = sessionId };

        [Fact]
        public async Task Booking_succeeds_when_membership_covers_the_session_start()
        {
            var member = w.AddMember(); w.AddMembership(member, w.AddPlan());
            var session = w.AddSession(w.AddTrainer());

            var result = await Sut.CreateNewBookingAsync(Book(member.Id, session.Id));

            Assert.True(result.Success, result.Error);
            Assert.Equal(1, w.Db.Bookings.Count);
            Assert.Equal(1, w.Uow.Transactions);   // capacity check + insert run in one transaction
        }

        [Fact]
        public async Task Booking_is_rejected_when_the_membership_expires_before_the_session()
        {
            var member = w.AddMember();
            w.AddMembership(member, w.AddPlan(days: 3), startOffsetDays: 0);          // ends in 3 days
            var session = w.AddSession(w.AddTrainer(), startsInHours: 24 * 10);        // 10 days away

            var result = await Sut.CreateNewBookingAsync(Book(member.Id, session.Id));

            Assert.False(result.Success);
            Assert.Contains("covers", result.Error!);
            Assert.Empty(w.Db.Bookings);
        }

        [Fact]
        public async Task Booking_is_rejected_without_any_membership()
        {
            var member = w.AddMember();
            var session = w.AddSession(w.AddTrainer());

            var result = await Sut.CreateNewBookingAsync(Book(member.Id, session.Id));

            Assert.False(result.Success);
        }

        [Fact]
        public async Task A_membership_that_starts_before_the_session_covers_a_future_session()
        {
            var member = w.AddMember();
            w.AddMembership(member, w.AddPlan(), startOffsetDays: 5);                  // starts in 5 days
            var session = w.AddSession(w.AddTrainer(), startsInHours: 24 * 6);

            Assert.True((await Sut.CreateNewBookingAsync(Book(member.Id, session.Id))).Success);
        }

        [Fact]
        public async Task Booking_the_same_member_twice_is_rejected()
        {
            var member = w.AddMember(); w.AddMembership(member, w.AddPlan());
            var session = w.AddSession(w.AddTrainer());
            await Sut.CreateNewBookingAsync(Book(member.Id, session.Id));

            var second = await Sut.CreateNewBookingAsync(Book(member.Id, session.Id));

            Assert.False(second.Success);
            Assert.Equal(1, w.Db.Bookings.Count);
        }

        [Fact]
        public async Task Booking_is_rejected_when_the_session_is_full()
        {
            var plan = w.AddPlan();
            var session = w.AddSession(w.AddTrainer(), capacity: 2);
            foreach (var _ in Enumerable.Range(0, 2)) { var m = w.AddMember(); w.AddMembership(m, plan); w.AddBooking(session, m); }
            var late = w.AddMember(); w.AddMembership(late, plan);

            var result = await Sut.CreateNewBookingAsync(Book(late.Id, session.Id));

            Assert.False(result.Success);
            Assert.Contains("full", result.Error!);
            Assert.Equal(2, w.Db.Bookings.Count);
        }

        [Fact]
        public async Task Booking_is_rejected_once_the_session_has_started()
        {
            var member = w.AddMember(); w.AddMembership(member, w.AddPlan());
            var session = w.AddSession(w.AddTrainer(), startsInHours: -0.5, hours: 1);

            Assert.False((await Sut.CreateNewBookingAsync(Book(member.Id, session.Id))).Success);
        }

        [Fact]
        public async Task Unknown_session_or_member_is_not_found()
        {
            var member = w.AddMember();
            var r1 = await Sut.CreateNewBookingAsync(Book(member.Id, 999));
            Assert.Equal(ResultKind.NotFound, r1.Kind);

            var session = w.AddSession(w.AddTrainer());
            var r2 = await Sut.CreateNewBookingAsync(Book(999, session.Id));
            Assert.Equal(ResultKind.NotFound, r2.Kind);
        }

        [Fact]
        public async Task Cancelling_works_before_the_session_starts_but_not_after()
        {
            var member = w.AddMember();
            var trainer = w.AddTrainer();
            var future = w.AddSession(trainer);
            var started = w.AddSession(trainer, startsInHours: -0.25, hours: 1);
            w.AddBooking(future, member); w.AddBooking(started, member);

            Assert.True((await Sut.CancelBookingAsync(member.Id, future.Id)).Success);
            Assert.False((await Sut.CancelBookingAsync(member.Id, started.Id)).Success);
            Assert.Equal(1, w.Db.Bookings.Count);
        }

        [Fact]
        public async Task Attendance_can_only_be_marked_while_the_session_is_running()
        {
            var member = w.AddMember();
            var trainer = w.AddTrainer();
            var upcoming = w.AddSession(trainer, startsInHours: 2);
            var live = w.AddSession(trainer, startsInHours: -0.25, hours: 1);
            var over = w.AddSession(trainer, startsInHours: -3, hours: 1);
            var b1 = w.AddBooking(upcoming, member); var b2 = w.AddBooking(live, member); var b3 = w.AddBooking(over, member);

            Assert.False((await Sut.MarkAttendedAsync(member.Id, upcoming.Id)).Success);
            Assert.False((await Sut.MarkAttendedAsync(member.Id, over.Id)).Success);
            Assert.True((await Sut.MarkAttendedAsync(member.Id, live.Id)).Success);

            Assert.False(b1.IsAttended);
            Assert.True(b2.IsAttended);
            Assert.False(b3.IsAttended);
        }

        [Fact]
        public async Task Roster_offers_only_unbooked_members_whose_membership_covers_the_session()
        {
            var plan = w.AddPlan();
            var session = w.AddSession(w.AddTrainer());
            var eligible = w.AddMember("Eligible"); w.AddMembership(eligible, plan);
            var booked = w.AddMember("Booked"); w.AddMembership(booked, plan); w.AddBooking(session, booked);
            var expired = w.AddMember("Expired"); w.AddMembership(expired, plan, startOffsetDays: -90);
            w.AddMember("NoPlan");

            var roster = await Sut.GetRosterAsync(session.Id);

            Assert.NotNull(roster);
            Assert.Equal("Eligible", Assert.Single(roster!.EligibleMembers).Name);
            Assert.Equal("Booked", Assert.Single(roster.Members).MemberName);
            Assert.True(roster.CanBook);
            Assert.True(roster.CanCancel);
            Assert.False(roster.CanMarkAttendance);
        }

        [Fact]
        public async Task Roster_of_a_running_session_allows_attendance_but_not_booking()
        {
            var live = w.AddSession(w.AddTrainer(), startsInHours: -0.25, hours: 1);

            var roster = await Sut.GetRosterAsync(live.Id);

            Assert.False(roster!.CanBook);
            Assert.True(roster.CanMarkAttendance);
            Assert.Equal(SessionStatus.Ongoing, roster.Session.Status);
        }

        [Fact]
        public async Task Schedule_lists_unfinished_sessions_soonest_first_with_booked_counts()
        {
            var trainer = w.AddTrainer();
            var member = w.AddMember();
            w.AddSession(trainer, startsInHours: -5, hours: 1);                // finished: excluded
            var later = w.AddSession(trainer, startsInHours: 30);
            var sooner = w.AddSession(trainer, startsInHours: 3);
            w.AddBooking(sooner, member);

            var schedule = await Sut.GetScheduleAsync();

            Assert.Equal(2, schedule.Count);
            Assert.Equal(sooner.Id, schedule[0].Id);
            Assert.Equal(1, schedule[0].BookedCount);
            Assert.Equal(later.Id, schedule[1].Id);
        }
    }
}
