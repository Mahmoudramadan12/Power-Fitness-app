using GymManagementBLL.Services.AttachmentService;
using GymManagementDAL.Entities;
using GymManagementDAL.Entities.Enums;

namespace GymManagement.Tests.Fakes
{
    /// <summary>Records what the services asked the file store to do.</summary>
    public sealed class FakeAttachmentService : IAttachmentService
    {
        public List<string> Uploaded { get; } = [];
        public List<string> Deleted { get; } = [];
        public bool RejectUploads { get; set; }

        public Task<string?> UploadAsync(Stream fileStream, string fileName, string folderName, CancellationToken ct = default)
        {
            if (RejectUploads) return Task.FromResult<string?>(null);
            var name = $"stored-{Uploaded.Count + 1}{Path.GetExtension(fileName)}";
            Uploaded.Add(name);
            return Task.FromResult<string?>(name);
        }

        public bool Delete(string fileName, string folderName) { Deleted.Add(fileName); return true; }
        public (Stream Stream, string ContentType)? GetFile(string fileName, string folderName) => null;
    }

    /// <summary>Test fixture: a fake database, a clock fixed at Wed 2026-10-07 10:00, and entity builders.</summary>
    public sealed class World
    {
        public static readonly DateTime Start = new(2026, 10, 7, 10, 0, 0);

        public FakeClock Clock { get; } = new(Start);
        public InMemoryDb Db { get; }
        public InMemoryUnitOfWork Uow { get; }
        public FakeAttachmentService Files { get; } = new();
        public DateTime Now => Clock.Now;

        public World()
        {
            Db = new InMemoryDb(Clock);
            Uow = new InMemoryUnitOfWork(Db);

            Db.Categories.AddRange(
            [
                new CategoryEntity { Id = 1, CategoryName = "Cardio" },
                new CategoryEntity { Id = 2, CategoryName = "Strength" },
                new CategoryEntity { Id = 3, CategoryName = "Yoga" },
                new CategoryEntity { Id = 4, CategoryName = "Boxing" },
                new CategoryEntity { Id = 5, CategoryName = "CrossFit" },
            ]);
        }

        public CategoryEntity Category(string name) => Db.Categories.First(c => c.CategoryName == name);

        private int _seq;
        public MemberEntity AddMember(string name = "Test Member")
        {
            var n = ++_seq;
            var m = new MemberEntity
            {
                Name = name, Email = $"m{n}@x.test", Phone = $"01012{n:D6}", Gender = Gender.Male,
                DateOfBirth = new DateOnly(1995, 5, 20), CreatedAt = Now.AddDays(-100),
                Address = new Address { BuildingNumber = 1, City = "Cairo", Street = "Main" }
            };
            Db.OnAdd(m); Db.Members.Add(m);
            return m;
        }

        public TrainerEntity AddTrainer(Specialties specialty = Specialties.Boxing, string name = "Coach")
        {
            var n = ++_seq;
            var t = new TrainerEntity
            {
                Name = name, Email = $"t{n}@x.test", Phone = $"01112{n:D6}", Gender = Gender.Male,
                DateOfBirth = new DateOnly(1990, 1, 1), Specialties = specialty, CreatedAt = Now.AddYears(-1),
                Address = new Address { BuildingNumber = 1, City = "Cairo", Street = "Main" }
            };
            Db.OnAdd(t); Db.Trainers.Add(t);
            return t;
        }

        public PlanEntity AddPlan(string name = "Monthly", int days = 30, decimal price = 500, bool active = true)
        {
            var p = new PlanEntity { Name = name, Description = "A plan", DurationDays = days, Price = price, IsActive = active };
            Db.OnAdd(p); Db.Plans.Add(p);
            return p;
        }

        public SessionEntity AddSession(TrainerEntity trainer, string category = "Boxing",
            double startsInHours = 24, double hours = 1, int capacity = 10)
        {
            var s = new SessionEntity
            {
                Description = "A test session", Capacity = capacity, Trainer = trainer, TrainerId = trainer.Id,
                Category = Category(category), CategoryId = Category(category).Id,
                StartDate = Now.AddHours(startsInHours), EndDate = Now.AddHours(startsInHours + hours)
            };
            Db.OnAdd(s); Db.Sessions.Add(s);
            return s;
        }

        /// <summary>A membership running from <paramref name="startOffsetDays"/> (relative to today 10:00) for <paramref name="days"/> days.</summary>
        public MembershipEntity AddMembership(MemberEntity member, PlanEntity plan, int startOffsetDays = -1, int? days = null)
        {
            var start = Now.Date.AddDays(startOffsetDays);
            var m = new MembershipEntity
            {
                Member = member, MemberId = member.Id, Plan = plan, PlanId = plan.Id,
                CreatedAt = start, EndDate = start.AddDays(days ?? plan.DurationDays), PricePaid = plan.Price
            };
            Db.OnAdd(m); Db.Memberships.Add(m);
            return m;
        }

        public BookingEntity AddBooking(SessionEntity session, MemberEntity member, bool attended = false)
        {
            var b = new BookingEntity { Session = session, SessionId = session.Id, Member = member, MemberId = member.Id, IsAttended = attended, CreatedAt = Now.AddDays(-1) };
            Db.Bookings.Add(b);
            return b;
        }
    }
}
