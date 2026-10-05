using GymManagementBLL.Common;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementBLL.ViewModels.PlanViewModels;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementBLL.ViewModels.TrainerViewModels;
using GymManagementDAL.Entities;

namespace GymManagementBLL.Mapping
{
    /// <summary>
    /// Explicit entity &lt;-&gt; view-model mapping. Anything that depends on "now" (statuses, ages,
    /// remaining days) takes the current time as a parameter instead of reading the system clock.
    /// </summary>
    public static class Mappers
    {
        public static string FormatAddress(Address a) => $"{a.BuildingNumber} {a.Street}, {a.City}";

        public static string Initials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpperInvariant(),
                _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
            };
        }

        #region Status helpers

        public static SessionStatus StatusOf(SessionEntity s, DateTime now) =>
            s.StartDate > now ? SessionStatus.Upcoming :
            s.EndDate >= now ? SessionStatus.Ongoing :
            SessionStatus.Completed;

        public static MembershipStatus StatusOf(MembershipEntity m, DateTime now) =>
            m.IsUpcomingAt(now) ? MembershipStatus.Upcoming :
            m.IsActiveAt(now) ? MembershipStatus.Active :
            MembershipStatus.Expired;

        public static int RemainingDays(MembershipEntity m, DateTime now) =>
            m.EndDate <= now ? 0 : (int)Math.Ceiling((m.EndDate - now).TotalDays);

        #endregion

        #region Members

        public static MemberViewModel ToViewModel(this MemberEntity m, MembershipEntity? active = null) => new()
        {
            Id = m.Id,
            Photo = m.Photo,
            Name = m.Name,
            Email = m.Email,
            Phone = m.Phone,
            Gender = m.Gender.ToString(),
            PlanName = active?.Plan.Name,
            MembershipEndDate = active?.EndDate
        };

        public static MemberEntity ToEntity(this CreateMemberViewModel vm, string? photo) => new()
        {
            Name = vm.Name.Trim(),
            Email = vm.Email.Trim(),
            Phone = vm.Phone.Trim(),
            DateOfBirth = vm.DateOfBirth!.Value,
            Gender = vm.Gender!.Value,
            Photo = photo,
            Address = new Address
            {
                BuildingNumber = vm.BuildingNumber!.Value,
                Street = vm.Street.Trim(),
                City = vm.City.Trim()
            },
            HealthRecord = vm.HealthRecordViewModel.ToEntity()
        };

        public static MemberToUpdateViewModel ToUpdateViewModel(this MemberEntity m) => new()
        {
            Id = m.Id,
            Name = m.Name,
            Photo = m.Photo,
            Email = m.Email,
            Phone = m.Phone,
            BuildingNumber = m.Address.BuildingNumber,
            City = m.Address.City,
            Street = m.Address.Street
        };

        public static void Apply(this MemberToUpdateViewModel vm, MemberEntity m)
        {
            m.Name = vm.Name.Trim();
            m.Email = vm.Email.Trim();
            m.Phone = vm.Phone.Trim();
            m.Address.BuildingNumber = vm.BuildingNumber!.Value;
            m.Address.City = vm.City.Trim();
            m.Address.Street = vm.Street.Trim();
        }

        public static HealthRecordEntity ToEntity(this HealthRecordViewModel vm) => new()
        {
            Height = vm.Height!.Value,
            Weight = vm.Weight!.Value,
            BloodType = vm.BloodType.Trim().ToUpperInvariant(),
            Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim()
        };

        public static void Apply(this HealthRecordViewModel vm, HealthRecordEntity e)
        {
            e.Height = vm.Height!.Value;
            e.Weight = vm.Weight!.Value;
            e.BloodType = vm.BloodType.Trim().ToUpperInvariant();
            e.Note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
        }

        public static HealthRecordViewModel ToViewModel(this HealthRecordEntity e, string memberName) => new()
        {
            MemberId = e.MemberId,
            MemberName = memberName,
            Height = e.Height,
            Weight = e.Weight,
            BloodType = e.BloodType,
            Note = e.Note
        };

        public static MembershipSummaryViewModel ToSummary(this MembershipEntity m, DateTime now) => new()
        {
            MembershipId = m.Id,
            PlanName = m.Plan.Name,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            PricePaid = m.PricePaid,
            Status = StatusOf(m, now),
            RemainingDays = RemainingDays(m, now)
        };

        #endregion

        #region Trainers

        public static TrainerViewModel ToViewModel(this TrainerEntity t, DateTime now) => t.FillTrainer(new TrainerViewModel(), now);

        public static TrainerDetailsViewModel ToDetailsViewModel(this TrainerEntity t, DateTime now) => t.FillTrainer(new TrainerDetailsViewModel(), now);

        private static T FillTrainer<T>(this TrainerEntity t, T vm, DateTime now) where T : TrainerViewModel
        {
            vm.Id = t.Id;
            vm.Name = t.Name;
            vm.Email = t.Email;
            vm.Phone = t.Phone;
            vm.DateOfBirth = t.DateOfBirth;
            vm.Age = t.DateOfBirth.AgeOn(now);
            vm.Gender = t.Gender.ToString();
            vm.Address = FormatAddress(t.Address);
            vm.Specialty = t.Specialties;
            vm.HireDate = t.CreatedAt;
            return vm;
        }

        public static TrainerEntity ToEntity(this CreateTrainerViewModel vm) => new()
        {
            Name = vm.Name.Trim(),
            Email = vm.Email.Trim(),
            Phone = vm.Phone.Trim(),
            DateOfBirth = vm.DateOfBirth!.Value,
            Gender = vm.Gender!.Value,
            Specialties = vm.Specialty!.Value,
            Address = new Address
            {
                BuildingNumber = vm.BuildingNumber!.Value,
                Street = vm.Street.Trim(),
                City = vm.City.Trim()
            }
        };

        public static TrainerToUpdateViewModel ToUpdateViewModel(this TrainerEntity t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Email = t.Email,
            Phone = t.Phone,
            BuildingNumber = t.Address.BuildingNumber,
            City = t.Address.City,
            Street = t.Address.Street,
            Specialty = t.Specialties
        };

        public static void Apply(this TrainerToUpdateViewModel vm, TrainerEntity t)
        {
            t.Name = vm.Name.Trim();
            t.Email = vm.Email.Trim();
            t.Phone = vm.Phone.Trim();
            t.Specialties = vm.Specialty!.Value;
            t.Address.BuildingNumber = vm.BuildingNumber!.Value;
            t.Address.City = vm.City.Trim();
            t.Address.Street = vm.Street.Trim();
        }

        #endregion

        #region Plans

        public static PlanViewModel ToViewModel(this PlanEntity p, int activeMembers = 0) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            DurationDays = p.DurationDays,
            Price = p.Price,
            IsActive = p.IsActive,
            ActiveMembers = activeMembers
        };

        public static UpdatePlanViewModel ToUpdateViewModel(this PlanEntity p) => new()
        {
            Id = p.Id,
            PlanName = p.Name,
            Description = p.Description,
            DurationDays = p.DurationDays,
            Price = p.Price
        };

        public static PlanEntity ToEntity(this CreatePlanViewModel vm) => new()
        {
            Name = vm.PlanName.Trim(),
            Description = vm.Description.Trim(),
            DurationDays = vm.DurationDays!.Value,
            Price = vm.Price!.Value,
            IsActive = true
        };

        public static void Apply(this UpdatePlanViewModel vm, PlanEntity p)
        {
            p.Name = vm.PlanName.Trim();
            p.Description = vm.Description.Trim();
            p.DurationDays = vm.DurationDays!.Value;
            p.Price = vm.Price!.Value;
        }

        #endregion

        #region Sessions & memberships

        public static SessionViewModel ToViewModel(this SessionEntity s, int bookedCount, DateTime now) => new()
        {
            Id = s.Id,
            Description = s.Description,
            Capacity = s.Capacity,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            TrainerId = s.TrainerId,
            TrainerName = s.Trainer.Name,
            CategoryId = s.CategoryId,
            CategoryName = s.Category.CategoryName,
            BookedCount = bookedCount,
            Status = StatusOf(s, now)
        };

        public static MembershipViewModel ToViewModel(this MembershipEntity m, DateTime now) => new()
        {
            MembershipId = m.Id,
            MemberId = m.MemberId,
            PlanId = m.PlanId,
            MemberName = m.Member.Name,
            PlanName = m.Plan.Name,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            PricePaid = m.PricePaid,
            Status = StatusOf(m, now),
            RemainingDays = RemainingDays(m, now)
        };

        #endregion
    }
}
