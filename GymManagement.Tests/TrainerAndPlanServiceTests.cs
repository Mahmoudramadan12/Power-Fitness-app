using GymManagement.Tests.Fakes;
using GymManagementBLL.Common;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.ViewModels.PlanViewModels;
using GymManagementBLL.ViewModels.TrainerViewModels;
using GymManagementDAL.Entities.Enums;

namespace GymManagement.Tests
{
    public class TrainerServiceTests
    {
        private readonly World w = new();
        private TrainerService Sut => new(w.Uow, w.Clock);

        [Fact]
        public async Task A_trainer_with_any_session_history_cannot_be_deleted()
        {
            // Regression: deleting a trainer used to cascade-delete their past sessions and all bookings.
            var withPast = w.AddTrainer(name: "Past");
            w.AddSession(withPast, startsInHours: -100, hours: 1);
            var withFuture = w.AddTrainer(name: "Future");
            w.AddSession(withFuture, startsInHours: 40);
            var unused = w.AddTrainer(name: "Unused");

            Assert.False((await Sut.RemoveTrainerAsync(withPast.Id)).Success);
            Assert.False((await Sut.RemoveTrainerAsync(withFuture.Id)).Success);
            Assert.True((await Sut.RemoveTrainerAsync(unused.Id)).Success);

            Assert.Equal(2, w.Db.Trainers.Count);
            Assert.Equal(2, w.Db.Sessions.Count);
        }

        [Fact]
        public async Task Changing_specialty_is_blocked_when_upcoming_sessions_would_no_longer_match()
        {
            var trainer = w.AddTrainer(Specialties.Boxing);
            w.AddSession(trainer, "Boxing", startsInHours: 30);
            var vm = (await Sut.GetTrainerToUpdateAsync(trainer.Id))!;
            vm.Specialty = Specialties.Yoga;

            var result = await Sut.UpdateTrainerDetailsAsync(trainer.Id, vm);

            Assert.False(result.Success);
            Assert.Equal(Specialties.Boxing, trainer.Specialties);
        }

        [Fact]
        public async Task Specialty_can_change_when_nothing_is_scheduled_and_contact_details_update()
        {
            var trainer = w.AddTrainer(Specialties.Boxing);
            var vm = (await Sut.GetTrainerToUpdateAsync(trainer.Id))!;
            vm.Specialty = Specialties.CrossFit; vm.Name = "New Name";

            Assert.True((await Sut.UpdateTrainerDetailsAsync(trainer.Id, vm)).Success);
            Assert.Equal(Specialties.CrossFit, trainer.Specialties);
            Assert.Equal("New Name", trainer.Name);
        }

        [Fact]
        public async Task Duplicate_contact_details_are_rejected()
        {
            var a = w.AddTrainer(); var b = w.AddTrainer();
            var vm = (await Sut.GetTrainerToUpdateAsync(a.Id))!;
            vm.Phone = b.Phone;
            Assert.False((await Sut.UpdateTrainerDetailsAsync(a.Id, vm)).Success);
        }

        [Fact]
        public async Task The_list_filters_by_specialty_and_search()
        {
            w.AddTrainer(Specialties.Yoga, "Mariam"); w.AddTrainer(Specialties.Boxing, "Omar"); w.AddTrainer(Specialties.Boxing, "Hassan");

            var boxing = await Sut.GetTrainersAsync(new TrainerListQuery { Specialty = Specialties.Boxing });
            var search = await Sut.GetTrainersAsync(new TrainerListQuery { Search = "mar" });

            Assert.Equal(2, boxing.TotalCount);
            Assert.Equal(2, search.TotalCount);   // Mariam, Omar
        }

        [Fact]
        public async Task Details_show_upcoming_sessions_and_the_total_count()
        {
            var t = w.AddTrainer();
            w.AddSession(t, startsInHours: 10); w.AddSession(t, startsInHours: 50); w.AddSession(t, startsInHours: -50, hours: 1);

            var d = (await Sut.GetTrainerDetailsAsync(t.Id))!;

            Assert.Equal(2, d.UpcomingSessions.Count);
            Assert.Equal(3, d.TotalSessions);
        }
    }

    public class PlanServiceTests
    {
        private readonly World w = new();
        private PlanService Sut => new(w.Uow, w.Clock);

        [Fact]
        public async Task Plans_with_active_or_upcoming_memberships_cannot_be_edited_or_deactivated()
        {
            var plan = w.AddPlan(); var member = w.AddMember();
            w.AddMembership(member, plan);

            Assert.Null(await Sut.GetPlanToUpdateAsync(plan.Id));
            Assert.False((await Sut.UpdatePlanAsync(plan.Id, new UpdatePlanViewModel { PlanName = "X", Description = "Desc!", DurationDays = 5, Price = 10 })).Success);
            Assert.False((await Sut.ToggleActivationAsync(plan.Id)).Success);
            Assert.True(plan.IsActive);
        }

        [Fact]
        public async Task A_plan_whose_memberships_all_expired_can_be_edited()
        {
            var plan = w.AddPlan(days: 7); var member = w.AddMember();
            w.AddMembership(member, plan, startOffsetDays: -60);

            var vm = (await Sut.GetPlanToUpdateAsync(plan.Id))!;
            vm.Price = 650; vm.PlanName = "Weekly Pass";
            Assert.True((await Sut.UpdatePlanAsync(plan.Id, vm)).Success);
            Assert.Equal(650m, plan.Price);
            Assert.Equal("Weekly Pass", plan.Name);
        }

        [Fact]
        public async Task Plan_names_must_be_unique_on_create_and_rename()
        {
            var a = w.AddPlan("Monthly"); w.AddPlan("Yearly", days: 365);

            var dup = await Sut.CreatePlanAsync(new CreatePlanViewModel { PlanName = " monthly ", Description = "dup plan", DurationDays = 30, Price = 1 });
            var ok = await Sut.CreatePlanAsync(new CreatePlanViewModel { PlanName = "Weekly", Description = "new plan", DurationDays = 7, Price = 150 });
            var vm = (await Sut.GetPlanToUpdateAsync(a.Id))!; vm.PlanName = "Yearly";

            Assert.False(dup.Success);
            Assert.True(ok.Success);
            Assert.True(w.Db.Plans.First(p => p.Name == "Weekly").IsActive);
            Assert.False((await Sut.UpdatePlanAsync(a.Id, vm)).Success);
        }

        [Fact]
        public async Task Activation_toggles_and_the_list_counts_active_members_per_plan()
        {
            var busy = w.AddPlan("Busy"); var idle = w.AddPlan("Idle", days: 90);
            w.AddMembership(w.AddMember(), busy); w.AddMembership(w.AddMember(), busy);

            Assert.True((await Sut.ToggleActivationAsync(idle.Id)).Success);
            Assert.False(idle.IsActive);
            Assert.True((await Sut.ToggleActivationAsync(idle.Id)).Success);
            Assert.True(idle.IsActive);

            var list = await Sut.GetAllPlansAsync();
            Assert.Equal(2, list.First(p => p.Name == "Busy").ActiveMembers);
            Assert.Equal(0, list.First(p => p.Name == "Idle").ActiveMembers);
        }
    }
}
