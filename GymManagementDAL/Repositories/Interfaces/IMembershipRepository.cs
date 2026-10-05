using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using System.Linq.Expressions;

namespace GymManagementDAL.Repositories.Interfaces
{
    public interface IMembershipRepository : IGenericRepository<MembershipEntity>
    {
        Task<List<MembershipEntity>> GetWithMemberAndPlanAsync(
            Expression<Func<MembershipEntity, bool>>? predicate,
            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> orderBy,
            int? take = null, CancellationToken ct = default);

        Task<PagedResult<MembershipEntity>> GetPagedWithMemberAndPlanAsync(
            Expression<Func<MembershipEntity, bool>>? predicate,
            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> orderBy,
            int page, int pageSize, CancellationToken ct = default);

        /// <summary>Every membership a member has ever had (newest first), with the plan loaded.</summary>
        Task<List<MembershipEntity>> GetHistoryForMemberAsync(int memberId, CancellationToken ct = default);

        /// <summary>Currently-active memberships for a set of members, with the plan loaded.</summary>
        Task<List<MembershipEntity>> GetActiveForMembersAsync(IReadOnlyCollection<int> memberIds, DateTime now, CancellationToken ct = default);
    }
}
