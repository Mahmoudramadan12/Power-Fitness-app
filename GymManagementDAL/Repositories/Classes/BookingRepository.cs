using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymManagementDAL.Repositories.Classes
{
    public class BookingRepository(GymDbContext dbContext) : GenericRepository<BookingEntity>(dbContext), IBookingRepository
    {
        public Task<List<BookingEntity>> GetBySessionIdAsync(int sessionId, CancellationToken ct = default)
            => DbContext.Bookings.AsNoTracking()
                .Include(b => b.Member)
                .Where(b => b.SessionId == sessionId)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync(ct);

        public Task<List<BookingEntity>> GetRecentByMemberIdAsync(int memberId, int take, CancellationToken ct = default)
            => DbContext.Bookings.AsNoTracking()
                .Include(b => b.Session).ThenInclude(s => s.Category)
                .Include(b => b.Session).ThenInclude(s => s.Trainer)
                .Where(b => b.MemberId == memberId)
                .OrderByDescending(b => b.Session.StartDate)
                .Take(take)
                .ToListAsync(ct);
    }
}
