using GymManagementBLL.Common;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementBLL.ViewModels.TrainerViewModels;
using GymManagementDAL.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GymManagement.Tests
{
    public class ValidationAndRulesTests
    {
        private static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }

        private static bool Fails(object model, string member) => Validate(model).Any(r => r.MemberNames.Contains(member));

        private static CreateMemberViewModel ValidMember() => new()
        {
            Name = "Ahmed Samir", Email = "a@b.co", Phone = "01012345678", Gender = Gender.Male,
            DateOfBirth = new DateOnly(1990, 1, 1), BuildingNumber = 1, City = "Cairo", Street = "Tahrir St.",
            HealthRecordViewModel = new HealthRecordViewModel { Height = 170, Weight = 70, BloodType = "O+" }
        };

        [Fact]
        public void A_complete_member_is_valid()
            => Assert.Empty(Validate(ValidMember()));

        [Fact]
        public void Arabic_names_and_realistic_street_names_are_accepted()
        {
            var m = ValidMember(); m.Name = "أحمد سمير"; m.Street = "Sheikh Zayed, 10th District"; m.City = "6th of October";
            // City starts with a digit, which is intentionally not allowed; the rest must pass.
            Assert.False(Fails(m, nameof(m.Name)));
            Assert.False(Fails(m, nameof(m.Street)));
        }

        [Fact]
        public void Names_with_digits_or_symbols_are_rejected()
        {
            var m = ValidMember(); m.Name = "Bob<script>"; Assert.True(Fails(m, nameof(m.Name)));
            m.Name = "R2D2"; Assert.True(Fails(m, nameof(m.Name)));
        }

        [Fact]
        public void Phone_must_be_an_eleven_digit_Egyptian_mobile()
        {
            var m = ValidMember();
            foreach (var bad in new[] { "0101234567", "01312345678", "010123456789", "0101234567a", "+201012345678" })
            { m.Phone = bad; Assert.True(Fails(m, nameof(m.Phone)), bad); }
            foreach (var good in new[] { "01012345678", "01112345678", "01212345678", "01512345678" })
            { m.Phone = good; Assert.False(Fails(m, nameof(m.Phone)), good); }
        }

        [Fact]
        public void Date_of_birth_must_be_in_a_sensible_range()
        {
            var m = ValidMember();
            m.DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1)); Assert.True(Fails(m, nameof(m.DateOfBirth)));
            m.DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-5)); Assert.True(Fails(m, nameof(m.DateOfBirth)));
            m.DateOfBirth = new DateOnly(1850, 1, 1); Assert.True(Fails(m, nameof(m.DateOfBirth)));
            m.DateOfBirth = new DateOnly(1985, 6, 1); Assert.False(Fails(m, nameof(m.DateOfBirth)));
        }

        [Fact]
        public void Trainers_must_be_adults()
        {
            var t = new CreateTrainerViewModel
            {
                Name = "Omar Hassan", Email = "o@x.co", Phone = "01012345678", Gender = Gender.Male, Specialty = Specialties.Boxing,
                BuildingNumber = 1, City = "Cairo", Street = "Main", DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-16))
            };
            Assert.True(Fails(t, nameof(t.DateOfBirth)));
            t.DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-30));
            Assert.Empty(Validate(t));
        }

        [Fact]
        public void Blood_type_must_be_a_real_group()
        {
            var h = new HealthRecordViewModel { Height = 170, Weight = 70 };
            foreach (var good in new[] { "A+", "O-", "AB+", "B-" }) { h.BloodType = good; Assert.False(Fails(h, nameof(h.BloodType)), good); }
            foreach (var bad in new[] { "C+", "A", "ABC+", "" }) { h.BloodType = bad; Assert.True(Fails(h, nameof(h.BloodType)), bad); }
        }

        [Fact]
        public void Session_capacity_zero_is_rejected_by_validation_not_only_by_the_database()
        {
            // Regression: Range(0, 25) let 0 through to a database constraint of 1..25.
            var s = new CreateSessionViewModel
            {
                Description = "A valid description", Capacity = 0, CategoryId = 1, TrainerId = 1,
                StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1).AddHours(1)
            };
            Assert.True(Fails(s, nameof(s.Capacity)));
            s.Capacity = 26; Assert.True(Fails(s, nameof(s.Capacity)));
            s.Capacity = 1; Assert.False(Fails(s, nameof(s.Capacity)));
        }

        [Fact]
        public void Session_end_must_be_after_start()
        {
            var s = new CreateSessionViewModel
            {
                Description = "A valid description", Capacity = 5, CategoryId = 1, TrainerId = 1,
                StartDate = DateTime.Today.AddDays(1), EndDate = DateTime.Today.AddDays(1)
            };
            Assert.True(Fails(s, nameof(s.EndDate)));
        }

        [Fact]
        public void Photo_uploads_are_checked_for_size_and_extension()
        {
            IFormFile File(string name, long length) => new FormFile(new MemoryStream(), 0, length, "PhotoFile", name);
            var m = ValidMember();

            m.PhotoFile = File("a.jpg", 1000); Assert.False(Fails(m, nameof(m.PhotoFile)));
            m.PhotoFile = File("a.JPEG", 1000); Assert.False(Fails(m, nameof(m.PhotoFile)));
            m.PhotoFile = File("a.gif", 1000); Assert.True(Fails(m, nameof(m.PhotoFile)));
            m.PhotoFile = File("a.jpg", 6 * 1024 * 1024); Assert.True(Fails(m, nameof(m.PhotoFile)));
            m.PhotoFile = null; Assert.False(Fails(m, nameof(m.PhotoFile)));
        }

        [Fact]
        public void Trainer_category_rules()
        {
            Assert.True(TrainerCategoryRules.CanLead(Specialties.Yoga, "Yoga"));
            Assert.True(TrainerCategoryRules.CanLead(Specialties.CrossFit, "crossfit"));
            Assert.False(TrainerCategoryRules.CanLead(Specialties.Yoga, "Boxing"));
            Assert.True(TrainerCategoryRules.CanLead(Specialties.GeneralFitness, "Cardio"));
            Assert.True(TrainerCategoryRules.CanLead(Specialties.GeneralFitness, "Strength"));
            Assert.False(TrainerCategoryRules.CanLead(Specialties.GeneralFitness, "Yoga"));
            Assert.False(TrainerCategoryRules.CanLead(Specialties.Boxing, "Cardio"));
        }

        [Fact]
        public void Age_is_computed_around_birthdays()
        {
            var now = new DateTime(2026, 10, 7, 10, 0, 0);
            Assert.Equal(30, new DateOnly(1996, 10, 7).AgeOn(now));   // birthday today
            Assert.Equal(29, new DateOnly(1996, 10, 8).AgeOn(now));   // birthday tomorrow
            Assert.Equal(30, new DateOnly(1996, 1, 1).AgeOn(now));
        }

        [Fact]
        public void Bmi_is_calculated_from_height_and_weight()
        {
            Assert.Equal(22.9m, new HealthRecordViewModel { Height = 175, Weight = 70 }.Bmi);
            Assert.Null(new HealthRecordViewModel { Height = null, Weight = 70 }.Bmi);
        }

        [Fact]
        public void Session_capacity_helpers()
        {
            var s = new SessionViewModel { Capacity = 8, BookedCount = 6, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMinutes(90) };
            Assert.Equal(2, s.AvailableSlots);
            Assert.False(s.IsFull);
            Assert.Equal(75, s.FillPercent);
            Assert.Equal("1 h 30 min", s.DurationDisplay);
            s.BookedCount = 8; Assert.True(s.IsFull);
        }
    }
}
