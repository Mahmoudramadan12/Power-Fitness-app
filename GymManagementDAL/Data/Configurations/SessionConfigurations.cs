using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class SessionConfigurations : IEntityTypeConfiguration<SessionEntity>
    {
        public void Configure(EntityTypeBuilder<SessionEntity> builder)
        {
            builder.ToTable(T =>
            {
                T.HasCheckConstraint("SessionCapacityConstraint", "Capacity between 1 and 25");
                T.HasCheckConstraint("SessionEndDateAfterStartDate", "EndDate > StartDate");
            });

            builder.Property(X => X.Description)
                   .HasMaxLength(500);

            // Restrict (not cascade): deleting a trainer or category must never silently wipe the
            // sessions - and through them every booking and attendance record - that reference it.
            builder.HasOne(X => X.Trainer)
                   .WithMany(X => X.Sessions)
                   .HasForeignKey(X => X.TrainerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(X => X.Category)
                   .WithMany(X => X.Sessions)
                   .HasForeignKey(X => X.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(X => X.StartDate);
        }
    }
}
