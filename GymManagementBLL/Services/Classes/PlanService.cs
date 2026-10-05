using GymManagementBLL.Common;
using GymManagementBLL.Mapping;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.PlanViewModels;
using GymManagementDAL.Common;
using GymManagementDAL.Entities;
using GymManagementDAL.Repositories.Interfaces;

namespace GymManagementBLL.Services.Classes
{
    public class PlanService(IUnitOfWork unitOfWork, TimeProvider time) : IPlanService
    {
        public async Task<IReadOnlyList<PlanViewModel>> GetAllPlansAsync(CancellationToken ct = default)
        {
            var now = time.Now();
            var plans = (await unitOfWork.GetRepository<PlanEntity>().GetAllAsync(ct: ct))
                .OrderByDescending(p => p.IsActive).ThenBy(p => p.DurationDays).ToList();

            // Active-member counts for every plan from a single query.
            var activePlanIds = await unitOfWork.MembershipRepository.SelectAsync(
                m => m.PlanId, m => m.CreatedAt <= now && m.EndDate > now, ct);
            var counts = activePlanIds.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            return plans.Select(p => p.ToViewModel(counts.GetValueOrDefault(p.Id))).ToList();
        }

        public async Task<PlanViewModel?> GetPlanByIdAsync(int planId, CancellationToken ct = default)
        {
            var plan = await unitOfWork.GetRepository<PlanEntity>().FirstOrDefaultAsync(p => p.Id == planId, ct: ct);
            if (plan is null) return null;

            var now = time.Now();
            var active = await unitOfWork.MembershipRepository.CountAsync(
                m => m.PlanId == planId && m.CreatedAt <= now && m.EndDate > now, ct);
            return plan.ToViewModel(active);
        }

        public async Task<UpdatePlanViewModel?> GetPlanToUpdateAsync(int planId, CancellationToken ct = default)
        {
            var plan = await unitOfWork.GetRepository<PlanEntity>().FirstOrDefaultAsync(p => p.Id == planId, ct: ct);
            if (plan is null || !plan.IsActive) return null;
            if (await HasLiveMembershipsAsync(planId, ct)) return null;
            return plan.ToUpdateViewModel();
        }

        public async Task<Result> CreatePlanAsync(CreatePlanViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<PlanEntity>();
            var name = model.PlanName.Trim();
            if (await repo.AnyAsync(p => p.Name == name, ct))
                return Result.Fail("A plan with this name already exists.");

            try
            {
                repo.Add(model.ToEntity());
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (DuplicateKeyException)
            {
                return Result.Fail("A plan with this name already exists.");
            }
        }

        public async Task<Result> UpdatePlanAsync(int id, UpdatePlanViewModel model, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<PlanEntity>();
            var plan = await repo.GetByIdAsync(id, ct);
            if (plan is null) return Result.NotFound("Plan not found.");

            if (await HasLiveMembershipsAsync(id, ct))
                return Result.Fail("This plan has active or upcoming memberships and can't be edited. Deactivate it and create a new plan instead.");

            var name = model.PlanName.Trim();
            if (await repo.AnyAsync(p => p.Name == name && p.Id != id, ct))
                return Result.Fail("Another plan already uses this name.");

            try
            {
                model.Apply(plan);
                repo.Update(plan);
                await unitOfWork.SaveChangesAsync(ct);
                return Result.Ok();
            }
            catch (DuplicateKeyException)
            {
                return Result.Fail("Another plan already uses this name.");
            }
        }

        public async Task<Result> ToggleActivationAsync(int planId, CancellationToken ct = default)
        {
            var repo = unitOfWork.GetRepository<PlanEntity>();
            var plan = await repo.GetByIdAsync(planId, ct);
            if (plan is null) return Result.NotFound("Plan not found.");

            if (plan.IsActive && await HasLiveMembershipsAsync(planId, ct))
                return Result.Fail("Cannot deactivate a plan that has active or upcoming memberships.");

            plan.IsActive = !plan.IsActive;
            repo.Update(plan);
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Ok();
        }

        /// <summary>Active or not-yet-started memberships (i.e. anything that hasn't expired).</summary>
        private Task<bool> HasLiveMembershipsAsync(int planId, CancellationToken ct)
        {
            var now = time.Now();
            return unitOfWork.MembershipRepository.AnyAsync(m => m.PlanId == planId && m.EndDate > now, ct);
        }
    }
}
