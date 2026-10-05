using GymManagementDAL.Entities;

namespace GymManagementDAL.Repositories.Interfaces
{
    public interface IBookingRepository : IGenericRepository<BookingEntity>
    {
        /// <summary>Bookings of a session with the member loaded, ordered by booking time.</summary>
        Task<List<BookingEntity>> GetBySessionIdAsync(int sessionId, CancellationToken ct = default);

        /// <summary>A member's most recent bookings with session, category and trainer loaded.</summary>
        Task<List<BookingEntity>> GetRecentByMemberIdAsync(int memberId, int take, CancellationToken ct = default);
    }
}
