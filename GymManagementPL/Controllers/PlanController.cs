using GymManagementBLL.Common;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.PlanViewModels;
using GymManagementDAL.Data.DataSeed;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class PlanController(IPlanService planService) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken ct)
            => View(await planService.GetAllPlansAsync(ct));

        [HttpGet, Authorize(Roles = RoleNames.SuperAdmin)]
        public IActionResult Create() => View(new CreatePlanViewModel());

        [HttpPost, Authorize(Roles = RoleNames.SuperAdmin)]
        public async Task<IActionResult> Create(CreatePlanViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await planService.CreatePlanAsync(model, ct);
            if (!result.Success)
            {
                this.AddServiceError(result);
                return View(model);
            }

            this.FlashSuccess($"Plan \"{model.PlanName}\" was created.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet, Authorize(Roles = RoleNames.SuperAdmin)]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await planService.GetPlanToUpdateAsync(id, ct);
            if (model is not null) return View(model);

            this.FlashError("This plan can't be edited: it is inactive or still has active/upcoming memberships.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize(Roles = RoleNames.SuperAdmin)]
        public async Task<IActionResult> Edit(int id, UpdatePlanViewModel model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var result = await planService.UpdatePlanAsync(id, model, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();
            if (!result.Success)
            {
                this.AddServiceError(result);
                return View(model);
            }

            this.FlashSuccess("Plan updated.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, Authorize(Roles = RoleNames.SuperAdmin)]
        public async Task<IActionResult> Activate(int id, CancellationToken ct)
        {
            var result = await planService.ToggleActivationAsync(id, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();

            this.Flash(result, "Plan status changed.");
            return RedirectToAction(nameof(Index));
        }
    }
}
