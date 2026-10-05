using GymManagementBLL.Common;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.TrainerViewModels;
using GymManagementDAL.Data.DataSeed;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class TrainerController(ITrainerService trainerService) : Controller
    {
        public async Task<IActionResult> Index([FromQuery] TrainerListQuery query, CancellationToken ct)
        {
            ViewBag.Query = query;
            return View(await trainerService.GetTrainersAsync(query, ct));
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var trainer = await trainerService.GetTrainerDetailsAsync(id, ct);
            return trainer is null ? NotFound() : View(trainer);
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateTrainerViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(CreateTrainerViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await trainerService.CreateTrainerAsync(model, ct);
            if (!result.Success)
            {
                this.AddServiceError(result);
                return View(model);
            }

            this.FlashSuccess($"{model.Name} was added as a trainer.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await trainerService.GetTrainerToUpdateAsync(id, ct);
            return model is null ? NotFound() : View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, TrainerToUpdateViewModel model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var result = await trainerService.UpdateTrainerDetailsAsync(id, model, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();
            if (!result.Success)
            {
                this.AddServiceError(result);
                return View(model);
            }

            this.FlashSuccess("Trainer details were updated.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await trainerService.RemoveTrainerAsync(id, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();

            this.Flash(result, "Trainer deleted.");
            return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }
    }
}
