using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class MemberConfigurations : GymUserConfigurations<MemberEntity>, IEntityTypeConfiguration<MemberEntity>
    {
        public new void Configure(EntityTypeBuilder<MemberEntity> builder)
        {
            builder.Property(X => X.CreatedAt)
                   .HasColumnName("JoinDate")
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(X => X.Photo)
                   .HasMaxLength(100);

            builder.HasOne(M => M.HealthRecord)
                   .WithOne(HR => HR.Member)
                   .HasForeignKey<HealthRecordEntity>(M => M.MemberId);

            base.Configure(builder);
        }
    }
}
