using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class PlanConfigurations : IEntityTypeConfiguration<PlanEntity>
    {
        public void Configure(EntityTypeBuilder<PlanEntity> builder)
        {
            builder.Property(X => X.Name)
                   .HasMaxLength(50);

            builder.HasIndex(X => X.Name).IsUnique();

            builder.Property(X => X.Description)
                   .HasMaxLength(200);

            builder.Property(X => X.Price)
                   .HasPrecision(10, 2);

            builder.Property(X => X.CreatedAt)
                   .HasDefaultValueSql("GETDATE()");

            builder.ToTable(Tb =>
            {
                Tb.HasCheckConstraint("PlanDurationCheck", "DurationDays Between 1 and 365");
            });
        }
    }
}
