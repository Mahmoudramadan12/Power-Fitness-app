using GymManagement.Tests.Fakes;
using GymManagementBLL.Common;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Entities.Enums;
using Microsoft.AspNetCore.Http;

namespace GymManagement.Tests
{
    public class MemberServiceTests
    {
        private readonly World w = new();
        private MemberService Sut => new(w.Uow, w.Files, w.Clock);

        private static IFormFile Photo(string name = "me.jpg") =>
            new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "PhotoFile", name);

        private static CreateMemberViewModel NewMember(string email = "new@x.test", string phone = "01099999999", IFormFile? photo = null) => new()
        {
            Name = "  Layla Hassan ", Email = email, Phone = phone, Gender = Gender.Female,
            DateOfBirth = new DateOnly(1998, 3, 3), BuildingNumber = 4, City = "Giza", Street = "Pyramids Road",
            PhotoFile = photo,
            HealthRecordViewModel = new HealthRecordViewModel { Height = 170, Weight = 62, BloodType = "o+", Note = " " }
        };

        [Fact]
        public async Task Creating_a_member_trims_input_and_creates_the_health_record()
        {
            var result = await Sut.CreateMemberAsync(NewMember());

            Assert.True(result.Success, result.Error);
            var m = Assert.Single(w.Db.Members);
            Assert.Equal("Layla Hassan", m.Name);
            Assert.Null(m.Photo);                                  // photo is optional
            Assert.Equal("O+", m.HealthRecord.BloodType);          // normalised
            Assert.Null(m.HealthRecord.Note);                      // blank note stored as null
        }

        [Fact]
        public async Task Duplicate_email_or_phone_is_rejected()
        {
            w.AddMember();
            var existing = w.Db.Members[0];

            var sameEmail = await Sut.CreateMemberAsync(NewMember(email: existing.Email));
            var samePhone = await Sut.CreateMemberAsync(NewMember(phone: existing.Phone));

            Assert.False(sameEmail.Success);
            Assert.Contains("email", sameEmail.Error!);
            Assert.False(samePhone.Success);
            Assert.Contains("phone", samePhone.Error!);
            Assert.Equal(1, w.Db.Members.Count);
        }

        [Fact]
        public async Task A_rejected_photo_stops_the_member_from_being_created()
        {
            w.Files.RejectUploads = true;
            var result = await Sut.CreateMemberAsync(NewMember(photo: Photo()));

            Assert.False(result.Success);
            Assert.Equal(ResultKind.ValidationFailed, result.Kind);
            Assert.Empty(w.Db.Members);
        }

        [Fact]
        public async Task A_photo_is_stored_and_referenced()
        {
            await Sut.CreateMemberAsync(NewMember(photo: Photo("pic.png")));

            Assert.Equal(w.Files.Uploaded[0], w.Db.Members[0].Photo);
        }

        [Fact]
        public async Task Replacing_a_photo_deletes_the_old_file_only_after_saving()
        {
            var member = w.AddMember(); member.Photo = "old.jpg";
            var vm = (await Sut.GetMemberToUpdateAsync(member.Id))!;
            vm.PhotoFile = Photo();

            var result = await Sut.UpdateMemberDetailsAsync(member.Id, vm);

            Assert.True(result.Success, result.Error);
            Assert.Equal(w.Files.Uploaded[0], member.Photo);
            Assert.Equal("old.jpg", Assert.Single(w.Files.Deleted));
        }

        [Fact]
        public async Task Updating_without_a_new_photo_keeps_the_existing_one()
        {
            var member = w.AddMember(); member.Photo = "keep.jpg";
            var vm = (await Sut.GetMemberToUpdateAsync(member.Id))!;
            vm.Name = "Renamed Person";

            await Sut.UpdateMemberDetailsAsync(member.Id, vm);

            Assert.Equal("keep.jpg", member.Photo);
            Assert.Equal("Renamed Person", member.Name);
            Assert.Empty(w.Files.Deleted);
        }

        [Fact]
        public async Task Update_rejects_an_email_used_by_someone_else_but_allows_keeping_your_own()
        {
            var a = w.AddMember(); var b = w.AddMember();
            var vm = (await Sut.GetMemberToUpdateAsync(a.Id))!;

            Assert.True((await Sut.UpdateMemberDetailsAsync(a.Id, vm)).Success);

            vm.Email = b.Email;
            Assert.False((await Sut.UpdateMemberDetailsAsync(a.Id, vm)).Success);
        }

        [Fact]
        public async Task Removing_a_member_deletes_their_photo_but_not_while_bookings_are_pending()
        {
            var trainer = w.AddTrainer();
            var busy = w.AddMember(); busy.Photo = "busy.jpg";
            var idle = w.AddMember(); idle.Photo = "idle.jpg";
            w.AddBooking(w.AddSession(trainer, startsInHours: 20), busy);
            w.AddBooking(w.AddSession(trainer, startsInHours: -72, hours: 1), idle);   // history only

            Assert.False((await Sut.RemoveMemberAsync(busy.Id)).Success);
            Assert.True((await Sut.RemoveMemberAsync(idle.Id)).Success);

            Assert.Equal("idle.jpg", Assert.Single(w.Files.Deleted));
            Assert.Equal(1, w.Db.Members.Count);
        }

        [Fact]
        public async Task The_list_searches_and_shows_each_members_active_plan()
        {
            var plan = w.AddPlan("Quarterly");
            var active = w.AddMember("Alice Active"); w.AddMembership(active, plan);
            var lapsed = w.AddMember("Bob Lapsed"); w.AddMembership(lapsed, plan, startOffsetDays: -200);

            var all = await Sut.GetMembersAsync(new ListQuery());
            var found = await Sut.GetMembersAsync(new ListQuery { Search = "lapsed" });

            Assert.Equal("Quarterly", all.Items.First(m => m.Name == "Alice Active").PlanName);
            Assert.False(all.Items.First(m => m.Name == "Bob Lapsed").HasActiveMembership);
            Assert.Equal("Bob Lapsed", Assert.Single(found.Items).Name);
        }

        [Fact]
        public async Task Details_include_age_membership_history_health_record_and_recent_bookings()
        {
            var member = w.AddMember();
            member.DateOfBirth = new DateOnly(1996, 10, 8);       // birthday is tomorrow -> still 29
            w.Db.HealthRecords.Add(new HealthRecordEntity { MemberId = member.Id, Member = member, Height = 180, Weight = 81, BloodType = "A+" });
            var plan = w.AddPlan(); w.AddMembership(member, plan);
            w.AddBooking(w.AddSession(w.AddTrainer(), startsInHours: 5), member);

            var d = (await Sut.GetMemberDetailsAsync(member.Id))!;

            Assert.Equal(29, d.Age);
            Assert.NotNull(d.CurrentMembership);
            Assert.Equal(25.0m, d.HealthRecord!.Bmi);
            Assert.Equal(1, d.RecentBookings.Count);
            Assert.Equal(SessionStatus.Upcoming, d.RecentBookings[0].Status);
            Assert.Null(await Sut.GetMemberDetailsAsync(31337));
        }

        [Fact]
        public async Task The_health_record_is_created_when_missing_and_updated_when_present()
        {
            var member = w.AddMember();
            var vm = new HealthRecordViewModel { Height = 175, Weight = 70, BloodType = "b-" };

            Assert.True((await Sut.UpdateHealthRecordAsync(member.Id, vm)).Success);
            Assert.Equal(1, w.Db.HealthRecords.Count);

            vm.Weight = 68;
            Assert.True((await Sut.UpdateHealthRecordAsync(member.Id, vm)).Success);
            Assert.Equal(1, w.Db.HealthRecords.Count);
            Assert.Equal(68m, w.Db.HealthRecords[0].Weight);

            Assert.Equal(ResultKind.NotFound, (await Sut.UpdateHealthRecordAsync(999, vm)).Kind);
        }
    }
}
