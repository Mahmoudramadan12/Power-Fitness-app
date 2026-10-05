using GymManagementDAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManagementDAL.Data.Configurations
{
    internal class BookingConfigurations : IEntityTypeConfiguration<BookingEntity>
    {
        public void Configure(EntityTypeBuilder<BookingEntity> builder)
        {
            // A booking is identified by (session, member): a member can book a session only once.
            builder.Ignore(X => X.Id);
            builder.HasKey(X => new { X.SessionId, X.MemberId });

            builder.Property(X => X.CreatedAt)
                   .HasColumnName("BookingDate")
                   .HasDefaultValueSql("GETDATE()");

            builder.HasOne(X => X.Session)
                   .WithMany(X => X.SessionMembers)
                   .HasForeignKey(X => X.SessionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(X => X.Member)
                   .WithMany(X => X.MemberSessions)
                   .HasForeignKey(X => X.MemberId);
        }
    }
}
