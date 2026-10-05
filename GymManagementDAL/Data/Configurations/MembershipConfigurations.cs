using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class MembershipConfigurations : IEntityTypeConfiguration<MembershipEntity>
    {
        public void Configure(EntityTypeBuilder<MembershipEntity> builder)
        {
            builder.HasKey(m => m.Id);

            // CreatedAt doubles as the membership start date.
            builder.Property(X => X.CreatedAt)
                   .HasColumnName("StartDate")
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(m => m.PricePaid)
                   .HasPrecision(10, 2);

            builder.Ignore(m => m.StartDate);

            builder.HasOne(m => m.Plan)
                   .WithMany(p => p.PlanMembers)
                   .HasForeignKey(m => m.PlanId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Member)
                   .WithMany(me => me.MemberPlans)
                   .HasForeignKey(m => m.MemberId)
                   .OnDelete(DeleteBehavior.Cascade);

            // "Does this member have an active membership?" is the hottest question in the app.
            builder.HasIndex(m => new { m.MemberId, m.EndDate });
            builder.HasIndex(m => m.EndDate);
        }
    }
}
