using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class HealthRecordEntityConfigurations : IEntityTypeConfiguration<HealthRecordEntity>
    {
        public void Configure(EntityTypeBuilder<HealthRecordEntity> builder)
        {
            builder.Property(x => x.Height).HasPrecision(6, 2);   // cm
            builder.Property(x => x.Weight).HasPrecision(6, 2);   // kg

            builder.Property(x => x.BloodType)
                   .HasMaxLength(5);

            builder.Property(x => x.Note)
                   .HasMaxLength(500);
        }
    }
}
