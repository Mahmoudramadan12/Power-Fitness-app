using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using System.Linq.Expressions;

namespace GymManagementDAL.Repositories.Interfaces
{
    public interface ISessionRepository : IGenericRepository<SessionEntity>
    {
        /// <summary>Sessions with trainer and category loaded; ordered by start date.</summary>
        Task<List<SessionEntity>> GetWithTrainerAndCategoryAsync(
            Expression<Func<SessionEntity, bool>>? predicate = null,
            bool newestFirst = false, int? take = null, CancellationToken ct = default);

        Task<PagedResult<SessionEntity>> GetPagedWithTrainerAndCategoryAsync(
            Expression<Func<SessionEntity, bool>>? predicate, bool newestFirst,
            int page, int pageSize, CancellationToken ct = default);

        Task<SessionEntity?> GetSessionWithTrainerAndCategoryAsync(int sessionId, CancellationToken ct = default);

        Task<int> GetCountOfBookedSlotsAsync(int sessionId, CancellationToken ct = default);

        /// <summary>Booked-seat counts for many sessions in a single query (avoids N+1).</summary>
        Task<Dictionary<int, int>> GetBookedCountsAsync(IReadOnlyCollection<int> sessionIds, CancellationToken ct = default);

        /// <summary>True when the trainer already leads another session overlapping [start, end).</summary>
        Task<bool> HasTrainerConflictAsync(int trainerId, DateTime start, DateTime end, int? excludeSessionId = null, CancellationToken ct = default);
    }
}
