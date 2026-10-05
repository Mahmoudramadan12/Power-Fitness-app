using GymManagement.Tests.Fakes;
using GymManagementBLL.Common;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementDAL.Entities.Enums;

namespace GymManagement.Tests
{
    public class SessionServiceTests
    {
        private readonly World w = new();
        private SessionService Sut => new(w.Uow, w.Clock);

        private CreateSessionViewModel NewSession(int trainerId, string category, double startsInHours = 24, double hours = 1, int capacity = 10) => new()
        {
            Description = "Evening class for everyone", Capacity = capacity, TrainerId = trainerId, CategoryId = w.Category(category).Id,
            StartDate = w.Now.AddHours(startsInHours), EndDate = w.Now.AddHours(startsInHours + hours)
        };

        [Fact]
        public async Task A_trainer_can_create_a_session_in_their_own_category()
        {
            var trainer = w.AddTrainer(Specialties.Boxing);
            var result = await Sut.CreateSessionAsync(NewSession(trainer.Id, "Boxing"));
            Assert.True(result.Success, result.Error);
            Assert.Equal(1, w.Db.Sessions.Count);
        }

        [Fact]
        public async Task A_trainer_cannot_lead_another_specialty()
        {
            var trainer = w.AddTrainer(Specialties.Boxing);
            var result = await Sut.CreateSessionAsync(NewSession(trainer.Id, "Yoga"));
            Assert.False(result.Success);
            Assert.Empty(w.Db.Sessions);
        }

        [Fact]
        public async Task General_fitness_trainers_can_lead_Cardio_and_Strength()
        {
            // Regression: these two categories were impossible to schedule before.
            var trainer = w.AddTrainer(Specialties.GeneralFitness);
            Assert.True((await Sut.CreateSessionAsync(NewSession(trainer.Id, "Cardio", startsInHours: 24))).Success);
            Assert.True((await Sut.CreateSessionAsync(NewSession(trainer.Id, "Strength", startsInHours: 48))).Success);
            Assert.False((await Sut.CreateSessionAsync(NewSession(trainer.Id, "Boxing", startsInHours: 72))).Success);
        }

        [Fact]
        public async Task A_trainer_cannot_be_double_booked()
        {
            var trainer = w.AddTrainer(Specialties.Yoga);
            await Sut.CreateSessionAsync(NewSession(trainer.Id, "Yoga", startsInHours: 24, hours: 2));

            var overlapping = await Sut.CreateSessionAsync(NewSession(trainer.Id, "Yoga", startsInHours: 25, hours: 1));
            var backToBack = await Sut.CreateSessionAsync(NewSession(trainer.Id, "Yoga", startsInHours: 26, hours: 1));

            Assert.False(overlapping.Success);
            Assert.True(backToBack.Success, backToBack.Error);   // touching end/start is fine
        }

        [Fact]
        public async Task Sessions_must_end_after_they_start_and_begin_in_the_future()
        {
            var trainer = w.AddTrainer();
            Assert.False((await Sut.CreateSessionAsync(NewSession(trainer.Id, "Boxing", hours: -1))).Success);
            Assert.False((await Sut.CreateSessionAsync(NewSession(trainer.Id, "Boxing", startsInHours: -2))).Success);
        }

        [Fact]
        public async Task Editing_is_blocked_once_started_or_when_there_are_bookings()
        {
            var trainer = w.AddTrainer();
            var member = w.AddMember();
            var started = w.AddSession(trainer, startsInHours: -1, hours: 3);
            var booked = w.AddSession(trainer, startsInHours: 30); w.AddBooking(booked, member);
            var free = w.AddSession(trainer, startsInHours: 60);

            Assert.Null(await Sut.GetSessionToUpdateAsync(started.Id));
            Assert.Null(await Sut.GetSessionToUpdateAsync(booked.Id));
            Assert.NotNull(await Sut.GetSessionToUpdateAsync(free.Id));

            var vm = (await Sut.GetSessionToUpdateAsync(free.Id))!;
            vm.Capacity = 20; vm.Description = "Updated description here";
            Assert.True((await Sut.UpdateSessionAsync(free.Id, vm)).Success);
            Assert.Equal(20, free.Capacity);

            Assert.False((await Sut.UpdateSessionAsync(booked.Id, vm)).Success);
        }

        [Fact]
        public async Task Updating_a_session_does_not_conflict_with_itself()
        {
            var trainer = w.AddTrainer();
            var s = w.AddSession(trainer, startsInHours: 30);
            var vm = (await Sut.GetSessionToUpdateAsync(s.Id))!;
            vm.EndDate = vm.EndDate!.Value.AddMinutes(30);

            Assert.True((await Sut.UpdateSessionAsync(s.Id, vm)).Success);
        }

        [Fact]
        public async Task Upcoming_sessions_without_bookings_can_be_deleted_but_running_or_booked_ones_cannot()
        {
            var trainer = w.AddTrainer(); var member = w.AddMember();
            var upcoming = w.AddSession(trainer, startsInHours: 30);
            var live = w.AddSession(trainer, startsInHours: -0.5, hours: 1);
            var booked = w.AddSession(trainer, startsInHours: 60); w.AddBooking(booked, member);
            var finished = w.AddSession(trainer, startsInHours: -48, hours: 1);

            Assert.True((await Sut.RemoveSessionAsync(upcoming.Id)).Success);
            Assert.False((await Sut.RemoveSessionAsync(live.Id)).Success);
            Assert.False((await Sut.RemoveSessionAsync(booked.Id)).Success);
            Assert.True((await Sut.RemoveSessionAsync(finished.Id)).Success);
            Assert.Equal(ResultKind.NotFound, (await Sut.RemoveSessionAsync(424242)).Kind);
        }

        [Fact]
        public async Task The_list_filters_by_status_category_and_search()
        {
            var boxing = w.AddTrainer(Specialties.Boxing, "Omar");
            var yoga = w.AddTrainer(Specialties.Yoga, "Mariam");
            w.AddSession(boxing, "Boxing", startsInHours: 24);
            w.AddSession(yoga, "Yoga", startsInHours: 30);
            w.AddSession(yoga, "Yoga", startsInHours: -0.5, hours: 1);
            w.AddSession(boxing, "Boxing", startsInHours: -48, hours: 1);

            var upcoming = await Sut.GetSessionsAsync(new SessionListQuery { Status = SessionStatus.Upcoming });
            var live = await Sut.GetSessionsAsync(new SessionListQuery { Status = SessionStatus.Ongoing });
            var done = await Sut.GetSessionsAsync(new SessionListQuery { Status = SessionStatus.Completed });
            var all = await Sut.GetSessionsAsync(new SessionListQuery { Status = null });
            var yogaOnly = await Sut.GetSessionsAsync(new SessionListQuery { Status = null, CategoryId = w.Category("Yoga").Id });
            var byTrainer = await Sut.GetSessionsAsync(new SessionListQuery { Status = SessionStatus.Upcoming, Search = "omar" });

            Assert.Equal(2, upcoming.TotalCount);
            Assert.Equal(1, live.TotalCount);
            Assert.Equal(1, done.TotalCount);
            Assert.Equal(4, all.TotalCount);
            Assert.Equal(2, yogaOnly.TotalCount);
            Assert.Equal("Boxing", Assert.Single(byTrainer.Items).CategoryName);
        }

        [Fact]
        public async Task Paging_reports_totals_and_clamps_the_page()
        {
            var trainer = w.AddTrainer();
            for (var i = 0; i < 25; i++) w.AddSession(trainer, startsInHours: 24 + i * 2);

            var page3 = await Sut.GetSessionsAsync(new SessionListQuery { Page = 3, PageSize = 10 });
            var beyond = await Sut.GetSessionsAsync(new SessionListQuery { Page = 99, PageSize = 10 });

            Assert.Equal(25, page3.TotalCount);
            Assert.Equal(3, page3.TotalPages);
            Assert.Equal(5, page3.Items.Count);
            Assert.Equal(21, page3.FirstItem);
            Assert.Equal(25, page3.LastItem);
            Assert.Equal(3, beyond.Page);
        }

        [Fact]
        public async Task Trainer_dropdown_lists_the_categories_each_trainer_can_lead()
        {
            w.AddTrainer(Specialties.GeneralFitness, "Gen"); w.AddTrainer(Specialties.Yoga, "Yogi");

            var list = await Sut.GetTrainersForDropDownAsync();

            Assert.Equal(new[] { 1, 2 }, list.First(t => t.Name == "Gen").CategoryIds.OrderBy(i => i).ToArray());
            Assert.Equal(new[] { 3 }, list.First(t => t.Name == "Yogi").CategoryIds.ToArray());
        }
    }
}
