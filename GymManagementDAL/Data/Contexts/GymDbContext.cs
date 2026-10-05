using GymManagementDAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GymManagementDAL.Data.Contexts
{
    public class GymDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly TimeProvider _time;

        public GymDbContext(DbContextOptions<GymDbContext> dbContextOptions, TimeProvider? timeProvider = null)
            : base(dbContextOptions)
        {
            _time = timeProvider ?? TimeProvider.System;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.Entity<ApplicationUser>(EB =>
            {
                EB.Property(X => X.FirstName)
                  .HasColumnType("varchar")
                  .HasMaxLength(50);

                EB.Property(X => X.LastName)
                  .HasColumnType("varchar")
                  .HasMaxLength(50);

                EB.Ignore(X => X.FullName);
            });
        }

        #region Audit timestamps

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampAuditColumns();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampAuditColumns();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Single place that maintains CreatedAt / UpdatedAt so services can't forget (or disagree).
        /// An explicit CreatedAt (e.g. a membership's chosen start date) is respected on insert.
        /// </summary>
        private void StampAuditColumns()
        {
            var now = _time.GetLocalNow().DateTime;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added when entry.Entity.CreatedAt == default:
                        entry.Entity.CreatedAt = now;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = now;
                        entry.Property(e => e.CreatedAt).IsModified = false;
                        break;
                }
            }
        }

        #endregion

        #region DbSets
        public DbSet<TrainerEntity> Trainers { get; set; } = null!;
        public DbSet<BookingEntity> Bookings { get; set; } = null!;
        public DbSet<CategoryEntity> Categories { get; set; } = null!;
        public DbSet<HealthRecordEntity> HealthRecords { get; set; } = null!;
        public DbSet<MemberEntity> Members { get; set; } = null!;
        public DbSet<MembershipEntity> Memberships { get; set; } = null!;
        public DbSet<PlanEntity> Plans { get; set; } = null!;
        public DbSet<SessionEntity> Sessions { get; set; } = null!;
        #endregion
    }
}
