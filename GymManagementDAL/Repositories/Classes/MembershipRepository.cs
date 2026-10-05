using GymManagementDAL.Common;
using GymManagementDAL.Data.Contexts;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GymManagementDAL.Repositories.Classes
{
    public class MembershipRepository(GymDbContext dbContext) : GenericRepository<MembershipEntity>(dbContext), IMembershipRepository
    {
        private IQueryable<MembershipEntity> WithRelations() =>
            DbContext.Memberships.AsNoTracking().Include(m => m.Plan).Include(m => m.Member);

        public async Task<List<MembershipEntity>> GetWithMemberAndPlanAsync(
            Expression<Func<MembershipEntity, bool>>? predicate,
            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> orderBy,
            int? take = null, CancellationToken ct = default)
        {
            var query = WithRelations();
            if (predicate is not null) query = query.Where(predicate);

            IQueryable<MembershipEntity> ordered = orderBy(query);
            if (take is > 0) ordered = ordered.Take(take.Value);

            return await ordered.ToListAsync(ct);
        }

        public Task<PagedResult<MembershipEntity>> GetPagedWithMemberAndPlanAsync(
            Expression<Func<MembershipEntity, bool>>? predicate,
            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> orderBy,
            int page, int pageSize, CancellationToken ct = default)
        {
            var query = WithRelations();
            if (predicate is not null) query = query.Where(predicate);
            return PageAsync(query, orderBy, page, pageSize, ct);
        }

        public Task<List<MembershipEntity>> GetHistoryForMemberAsync(int memberId, CancellationToken ct = default)
            => DbContext.Memberships.AsNoTracking()
                .Include(m => m.Plan)
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(ct);

        public Task<List<MembershipEntity>> GetActiveForMembersAsync(IReadOnlyCollection<int> memberIds, DateTime now, CancellationToken ct = default)
        {
            var ids = memberIds.ToList();
            return DbContext.Memberships.AsNoTracking()
                .Include(m => m.Plan)
                .Where(m => ids.Contains(m.MemberId) && m.CreatedAt <= now && m.EndDate > now)
                .ToListAsync(ct);
        }
    }
}
