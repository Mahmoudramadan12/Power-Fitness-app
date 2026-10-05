using GymManagementBLL.Common;
using GymManagementBLL.Services.Classes;
using GymManagementBLL.Services.AttachmentService;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.MemberViewModels;
using GymManagementDAL.Data.DataSeed;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize(Roles = RoleNames.SuperAdmin)]
    public class MemberController(IMemberService memberService, IAttachmentService attachments) : Controller
    {
        public async Task<IActionResult> Index([FromQuery] ListQuery query, CancellationToken ct)
        {
            ViewBag.Query = query;
            return View(await memberService.GetMembersAsync(query, ct));
        }

        public async Task<IActionResult> Details(int id, CancellationToken ct)
        {
            var member = await memberService.GetMemberDetailsAsync(id, ct);
            return member is null ? NotFound() : View(member);
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateMemberViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(CreateMemberViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await memberService.CreateMemberAsync(model, ct);
            if (!result.Success)
            {
                this.AddServiceError(result);
                return View(model);
            }

            this.FlashSuccess($"{model.Name} was added as a member.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken ct)
        {
            var model = await memberService.GetMemberToUpdateAsync(id, ct);
            return model is null ? NotFound() : View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, MemberToUpdateViewModel model, CancellationToken ct)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var result = await memberService.UpdateMemberDetailsAsync(id, model, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();
            if (!result.Success)
            {
                this.AddServiceError(result);
                return View(model);
            }

            this.FlashSuccess("Member details were updated.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> HealthRecord(int id, CancellationToken ct)
        {
            var model = await memberService.GetMemberHealthRecordAsync(id, ct);
            if (model is not null) return View(model);

            // A member without a record yet: show an empty form for them.
            var details = await memberService.GetMemberDetailsAsync(id, ct);
            return details is null ? NotFound() : View(new HealthRecordViewModel { MemberId = id, MemberName = details.Name });
        }

        [HttpPost]
        public async Task<IActionResult> HealthRecord(int id, HealthRecordViewModel model, CancellationToken ct)
        {
            model.MemberId = id;
            if (!ModelState.IsValid) return View(model);

            var result = await memberService.UpdateHealthRecordAsync(id, model, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();

            this.FlashSuccess("Health record saved.");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await memberService.RemoveMemberAsync(id, ct);
            if (result.Kind == ResultKind.NotFound) return NotFound();

            this.Flash(result, "Member deleted.");
            return result.Success ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Serves a member's photo. Photos live outside wwwroot, so they're only reachable when signed in.</summary>
        [HttpGet, Authorize]
        public async Task<IActionResult> Picture(int id, CancellationToken ct)
        {
            var fileName = await memberService.GetPhotoFileNameAsync(id, ct);
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            var file = attachments.GetFile(fileName, MemberService.PhotoFolder);
            if (file is null) return NotFound();

            Response.Headers.CacheControl = "private, max-age=3600";
            return File(file.Value.Stream, file.Value.ContentType);
        }
    }
}
