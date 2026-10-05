using GymManagementDAL.Common;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GymManagementDAL.Repositories.Classes
{
    public class SessionRepository(GymDbContext dbContext) : GenericRepository<SessionEntity>(dbContext), ISessionRepository
    {
        private IQueryable<SessionEntity> WithRelations() =>
            DbContext.Sessions.AsNoTracking().Include(s => s.Trainer).Include(s => s.Category);

        private static Func<IQueryable<SessionEntity>, IOrderedQueryable<SessionEntity>> Order(bool newestFirst) =>
            newestFirst
                ? q => q.OrderByDescending(s => s.StartDate).ThenByDescending(s => s.Id)
                : q => q.OrderBy(s => s.StartDate).ThenBy(s => s.Id);

        public async Task<List<SessionEntity>> GetWithTrainerAndCategoryAsync(
            Expression<Func<SessionEntity, bool>>? predicate = null,
            bool newestFirst = false, int? take = null, CancellationToken ct = default)
        {
            var query = WithRelations();
            if (predicate is not null) query = query.Where(predicate);

            IQueryable<SessionEntity> ordered = Order(newestFirst)(query);
            if (take is > 0) ordered = ordered.Take(take.Value);

            return await ordered.ToListAsync(ct);
        }

        public Task<PagedResult<SessionEntity>> GetPagedWithTrainerAndCategoryAsync(
            Expression<Func<SessionEntity, bool>>? predicate, bool newestFirst,
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = WithRelations();
            if (predicate is not null) query = query.Where(predicate);
            return PageAsync(query, Order(newestFirst), page, pageSize, ct);
        }

        public Task<SessionEntity?> GetSessionWithTrainerAndCategoryAsync(int sessionId, CancellationToken ct = default)
            => WithRelations().FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        public Task<int> GetCountOfBookedSlotsAsync(int sessionId, CancellationToken ct = default)
            => DbContext.Bookings.AsNoTracking().CountAsync(b => b.SessionId == sessionId, ct);

        public async Task<Dictionary<int, int>> GetBookedCountsAsync(IReadOnlyCollection<int> sessionIds, CancellationToken ct = default)
        {
            if (sessionIds.Count == 0) return [];

            var ids = sessionIds.ToList();
            var rows = await DbContext.Bookings.AsNoTracking()
                .Where(b => ids.Contains(b.SessionId))
                .GroupBy(b => b.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.SessionId, r => r.Count);
        }

        public Task<bool> HasTrainerConflictAsync(int trainerId, DateTime start, DateTime end, int? excludeSessionId = null, CancellationToken ct = default)
            => DbContext.Sessions.AsNoTracking().AnyAsync(s =>
                   s.TrainerId == trainerId
                   && (excludeSessionId == null || s.Id != excludeSessionId)
                   && s.StartDate < end
                   && s.EndDate > start, ct);
    }
}
