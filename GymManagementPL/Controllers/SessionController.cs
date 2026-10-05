using GymManagementBLL.Common;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.SessionViewModels;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class SessionController(ISessionService sessionService) : Controller
    {
        public async Task<IActionResult> Index([FromQuery] SessionListQuery query, CancellationToken ct)
        {
            ViewBag.Query = query;
            ViewBag.Categories = await sessionService.GetCategoriesForDropDownAsync(ct);
            return View(await sessionService.GetSessionsAsync(query, ct));
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var session = await sessionService.GetSessionByIdAsync(id, ct);
            return session is null ? NotFound() : View(session);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadDropDownsAsync(ct);
            return View(new CreateSessionViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSessionViewModel model, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var result = await sessionService.CreateSessionAsync(model, ct);
                if (result.Success)
                {
                    this.FlashSuccess("Session created.");
                    return RedirectToAction(nameof(Index));
                }
                this.AddServiceError(result);
            }

            await LoadDropDownsAsync(ct);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await sessionService.GetSessionToUpdateAsync(id, ct);
            if (model is null)
            {
                this.FlashError("This session can't be edited: it doesn't exist, has already started, or already has bookings.");
                return RedirectToAction(nameof(Index));
            }

            await LoadDropDownsAsync(ct);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, UpdateSessionViewModel model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var result = await sessionService.UpdateSessionAsync(id, model, ct);
                if (result.Kind == ResultKind.NotFound) return NotFound();
                if (result.Success)
                {
                    this.FlashSuccess("Session updated.");
                    return RedirectToAction(nameof(Index));
                }
                this.AddServiceError(result);
            }

            await LoadDropDownsAsync(ct);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await sessionService.RemoveSessionAsync(id, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();

            this.Flash(result, "Session deleted.");
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropDownsAsync(CancellationToken ct)
        {
            ViewBag.Trainers = await sessionService.GetTrainersForDropDownAsync(ct);
            ViewBag.Categories = await sessionService.GetCategoriesForDropDownAsync(ct);
        }
    }
}
