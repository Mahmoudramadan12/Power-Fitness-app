using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;
using System.Linq.Expressions;

namespace GymManagementBLL.Services.Classes
{
    public class MembershipService(IUnitOfWork unitOfWork, TimeProvider time) : IMembershipService
    {
        public async Task<PagedList<MembershipViewModel>> GetMembershipsAsync(MembershipListQuery query, CancellationToken ct = default)
        {
            var now = time.Now();
            var term = query.SearchTerm;

            Expression<Func<MembershipEntity, bool>> predicate = query.Status switch
            {
                MembershipStatus.Active => m => m.CreatedAt <= now && m.EndDate > now,
                MembershipStatus.Upcoming => m => m.CreatedAt > now,
                MembershipStatus.Expired => m => m.EndDate <= now,
                _ => m => true
            };

            if (term is not null)
            {
                var statusPredicate = predicate;
                var param = statusPredicate.Parameters[0];
                Expression<Func<MembershipEntity, bool>> search = m => m.Member.Name.Contains(term) || m.Plan.Name.Contains(term);
                var body = Expression.AndAlso(
                    statusPredicate.Body,
                    new Rebind(search.Parameters[0], param).Visit(search.Body)!);
                predicate = Expression.Lambda<Func<MembershipEntity, bool>>(body, param);
            }

            Func<IQueryable<MembershipEntity>, IOrderedQueryable<MembershipEntity>> order = query.Status switch
            {
                MembershipStatus.Active => q => q.OrderBy(m => m.EndDate).ThenBy(m => m.Id),          // expiring soonest first
                MembershipStatus.Upcoming => q => q.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id),
                _ => q => q.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            };

            var page = await unitOfWork.MembershipRepository.GetPagedWithMemberAndPlanAsync(
                predicate, order, query.Page, query.PageSize, ct);

            return new PagedList<MembershipViewModel>(
                page.Items.Select(m => m.ToViewModel(now)).ToList(), page.TotalCount, page.Page, page.PageSize);
        }

        public async Task<IReadOnlyList<PlanSelectListViewModel>> GetPlansForDropDownAsync(CancellationToken ct = default)
        {
            var plans = await unitOfWork.GetRepository<PlanEntity>().GetAllAsync(p => p.IsActive, ct: ct);
            return plans.OrderBy(p => p.DurationDays)
                .Select(p => new PlanSelectListViewModel { Id = p.Id, Name = p.Name, DurationDays = p.DurationDays, Price = p.Price })
                .ToList();
        }

        public async Task<IReadOnlyList<MemberSelectListViewModel>> GetMembersForDropDownAsync(CancellationToken ct = default)
        {
            var members = await unitOfWork.GetRepository<MemberEntity>().GetAllAsync(ct: ct);
            return members.OrderBy(m => m.Name)
                .Select(m => new MemberSelectListViewModel { Id = m.Id, Name = m.Name, Phone = m.Phone })
                .ToList();
        }

        public async Task<Result> CreateMembershipAsync(CreateMembershipViewModel model, CancellationToken ct = default)
        {
            var now = time.Now();
            var memberId = model.MemberId!.Value;

            if (!await unitOfWork.GetRepository<MemberEntity>().AnyAsync(m => m.Id == memberId, ct))
                return Result.NotFound("Member not found.");

            var plan = await unitOfWork.GetRepository<PlanEntity>().GetByIdAsync(model.PlanId!.Value, ct);
            if (plan is null) return Result.NotFound("Plan not found.");
            if (!plan.IsActive) return Result.Fail("This plan is not active.");

            // Memberships run in whole days: start at midnight of the chosen day (default: today).
            var start = (model.StartDate ?? now).Date;
            if (start < now.Date) return Result.Validation("The start date can't be in the past.");
            var end = start.AddDays(plan.DurationDays);

            // A member can only hold one membership at a time (including one that starts later).
            if (await unitOfWork.MembershipRepository.AnyAsync(
                    m => m.MemberId == memberId && m.CreatedAt < end && m.EndDate > start, ct))
                return Result.Fail("This member already has a membership that overlaps these dates.");

            unitOfWork.MembershipRepository.Add(new MembershipEntity
            {
                MemberId = memberId,
                PlanId = plan.Id,
                CreatedAt = start,
                EndDate = end,
                PricePaid = plan.Price      // snapshot: later price changes must not rewrite revenue history
            });

            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        public async Task<Result> CancelMembershipAsync(int membershipId, CancellationToken ct = default)
        {
            var membership = await unitOfWork.MembershipRepository.GetByIdAsync(membershipId, ct);
            if (membership is null) return Result.NotFound("Membership not found.");

            var now = time.Now();
            if (membership.EndDate <= now) return Result.Fail("This membership has already expired.");

            if (membership.StartDate > now)
            {
                unitOfWork.MembershipRepository.Delete(membership);     // never started: nothing to keep
            }
            else
            {
                membership.EndDate = now;                               // keep the record (and its price) for history
                unitOfWork.MembershipRepository.Update(membership);
            }

            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        private sealed class Rebind(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
        }
    }
}
