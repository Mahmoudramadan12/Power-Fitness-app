using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymManagementDAL.Data.DataSeed
{
    /// <summary>Sample data so a fresh Development database is not an empty shell. Never runs when members exist.</summary>
    public static class DemoDataSeeding
    {
        public static async Task SeedAsync(GymDbContext db, TimeProvider time, ILogger logger, CancellationToken ct = default)
        {
            if (await db.Members.AnyAsync(ct) || await db.Trainers.AnyAsync(ct)) return;

            var now = time.GetLocalNow().DateTime;
            var today = now.Date;
            var plans = await db.Plans.OrderBy(p => p.Id).ToListAsync(ct);
            if (plans.Count == 0) return;

            TrainerEntity T(string name, string email, string phone, Gender g, Specialties s, int dobYear) => new()
            {
                Name = name, Email = email, Phone = phone, Gender = g, Specialties = s,
                DateOfBirth = new DateOnly(dobYear, 3, 14),
                Address = new Address { BuildingNumber = 12, Street = "Gameat El Dowal", City = "Giza" },
                CreatedAt = today.AddYears(-2)
            };

            var boxing = T("Omar Hassan", "omar.hassan@powerfitness.local", "01012345601", Gender.Male, Specialties.Boxing, 1991);
            var yoga = T("Mariam Adel", "mariam.adel@powerfitness.local", "01112345602", Gender.Female, Specialties.Yoga, 1994);
            var crossfit = T("Karim Nabil", "karim.nabil@powerfitness.local", "01212345603", Gender.Male, Specialties.CrossFit, 1989);
            var general = T("Salma Ibrahim", "salma.ibrahim@powerfitness.local", "01512345604", Gender.Female, Specialties.GeneralFitness, 1993);
            db.Trainers.AddRange(boxing, yoga, crossfit, general);

            var names = new (string Name, Gender G)[]
            {
                ("Ahmed Samir", Gender.Male), ("Nada Mahmoud", Gender.Female), ("Mostafa Ali", Gender.Male),
                ("Hana Youssef", Gender.Female), ("Tarek Fathy", Gender.Male), ("Laila Hossam", Gender.Female),
                ("Amr Khaled", Gender.Male), ("Dina Osama", Gender.Female), ("Hossam Adel", Gender.Male),
                ("Rana Wael", Gender.Female), ("أحمد سمير", Gender.Male), ("سارة محمد", Gender.Female),
                ("Ziad Mansour", Gender.Male), ("Farah Nasser", Gender.Female)
            };
            string[] bloods = ["A+", "O+", "B+", "AB+", "O-", "A-"];
            var members = new List<MemberEntity>();
            for (var i = 0; i < names.Length; i++)
            {
                var prefix = new[] { "010", "011", "012", "015" }[i % 4];
                members.Add(new MemberEntity
                {
                    Name = names[i].Name,
                    Gender = names[i].G,
                    Email = $"member{i + 1}@powerfitness.local",
                    Phone = $"{prefix}{20000000 + i * 137:D8}",
                    DateOfBirth = new DateOnly(1985 + i, 1 + i % 12, 5 + i),
                    Address = new Address { BuildingNumber = 3 + i, Street = "Tahrir Street", City = "Cairo" },
                    CreatedAt = today.AddDays(-30 - i * 20),
                    HealthRecord = new HealthRecordEntity
                    {
                        Height = 160 + i * 2, Weight = 58 + i * 2.5m, BloodType = bloods[i % bloods.Length],
                        Note = i % 5 == 0 ? "Mild knee sensitivity" : null,
                        CreatedAt = today.AddDays(-30 - i * 20)
                    }
                });
            }
            db.Members.AddRange(members);

            MembershipEntity M(MemberEntity m, PlanEntity p, int startOffsetDays) => new()
            {
                Member = m, Plan = p, PricePaid = p.Price,
                CreatedAt = today.AddDays(startOffsetDays),
                EndDate = today.AddDays(startOffsetDays + p.DurationDays)
            };
            var memberships = new List<MembershipEntity>();
            for (var i = 0; i < 8; i++)   // active; the first two expire within a week
            {
                var plan = plans[i % plans.Count];
                memberships.Add(M(members[i], plan, i < 2 ? -(plan.DurationDays - 3 - i) : -(i * 4 + 10)));
            }
            for (var i = 8; i < 11; i++)  // expired
                memberships.Add(M(members[i], plans[0], -(plans[0].DurationDays + 20 + i)));
            memberships.Add(M(members[11], plans[1 % plans.Count], 5));  // starts in the future
            memberships.AddRange(new[] { M(members[0], plans[0], -400), M(members[1], plans[0], -300) }); // history
            db.Memberships.AddRange(memberships);

            var categories = await db.Categories.ToDictionaryAsync(c => c.CategoryName, c => c, ct);

            SessionEntity S(string desc, TrainerEntity t, string cat, DateTime start, int minutes, int capacity) => new()
            {
                Description = desc, Trainer = t, Category = categories[cat], Capacity = capacity,
                StartDate = start, EndDate = start.AddMinutes(minutes), CreatedAt = start.AddDays(-7)
            };

            var past = new[]
            {
                S("Beginner boxing fundamentals", boxing, "Boxing", today.AddDays(-6).AddHours(18), 60, 12),
                S("Morning vinyasa flow", yoga, "Yoga", today.AddDays(-5).AddHours(8), 60, 15),
                S("Full body strength circuit", general, "Strength", today.AddDays(-3).AddHours(17), 60, 15),
                S("CrossFit WOD - engine builder", crossfit, "CrossFit", today.AddDays(-2).AddHours(19), 75, 10),
                S("Cardio blast", general, "Cardio", today.AddDays(-1).AddHours(7), 45, 20),
            };
            var ongoing = S("Lunch-hour yoga reset", yoga, "Yoga", now.AddMinutes(-15), 60, 12);
            var upcoming = new[]
            {
                S("Pad work & footwork", boxing, "Boxing", today.AddDays(1).AddHours(18), 60, 12),
                S("Sunrise yoga", yoga, "Yoga", today.AddDays(1).AddHours(7), 60, 15),
                S("Strength basics: squat & deadlift", general, "Strength", today.AddDays(2).AddHours(17), 60, 10),
                S("CrossFit WOD - heavy day", crossfit, "CrossFit", today.AddDays(2).AddHours(19), 75, 8),
                S("HIIT cardio", general, "Cardio", today.AddDays(3).AddHours(8), 45, 20),
                S("Sparring technique", boxing, "Boxing", today.AddDays(4).AddHours(19), 60, 6),
                S("Restorative yoga", yoga, "Yoga", today.AddDays(5).AddHours(20), 60, 12),
            };
            db.Sessions.AddRange(past);
            db.Sessions.Add(ongoing);
            db.Sessions.AddRange(upcoming);

            void Book(SessionEntity s, int memberCount, bool attended)
            {
                for (var i = 0; i < memberCount; i++)
                    db.Bookings.Add(new BookingEntity
                    {
                        Session = s, Member = members[i], IsAttended = attended && i % 4 != 3,
                        CreatedAt = s.StartDate.AddDays(-2)
                    });
            }
            foreach (var s in past) Book(s, 5, attended: true);
            Book(ongoing, 4, attended: false);
            Book(upcoming[0], 3, false); Book(upcoming[1], 5, false); Book(upcoming[3], 8, false);

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded demo data: {T} trainers, {M} members, {S} sessions.",
                4, members.Count, past.Length + 1 + upcoming.Length);
        }
    }
}
