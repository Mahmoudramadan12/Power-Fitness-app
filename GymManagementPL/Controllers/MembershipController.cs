using GymManagementBLL.Common;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MembershipViewModels;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class MembershipController(IMembershipService membershipService, TimeProvider clock) : Controller
    {
        public async Task<IActionResult> Index([FromQuery] MembershipListQuery query, CancellationToken ct)
        {
            ViewBag.Query = query;
            return View(await membershipService.GetMembershipsAsync(query, ct));
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? memberId, CancellationToken ct)
        {
            await LoadDropDownsAsync(ct);
            return View(new CreateMembershipViewModel { MemberId = memberId, StartDate = clock.GetLocalNow().Date });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateMembershipViewModel model, CancellationToken ct)
        {
            if (ModelState.IsValid)
            {
                var result = await membershipService.CreateMembershipAsync(model, ct);
                if (result.Success)
                {
                    this.FlashSuccess("Membership created.");
                    return RedirectToAction(nameof(Index));
                }
                this.AddServiceError(result);
            }

            await LoadDropDownsAsync(ct);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, CancellationToken ct)
        {
            var result = await membershipService.CancelMembershipAsync(id, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();

            this.Flash(result, "Membership ended.");
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropDownsAsync(CancellationToken ct)
        {
            ViewBag.Members = await membershipService.GetMembersForDropDownAsync(ct);
            ViewBag.Plans = await membershipService.GetPlansForDropDownAsync(ct);
        }
    }
}
