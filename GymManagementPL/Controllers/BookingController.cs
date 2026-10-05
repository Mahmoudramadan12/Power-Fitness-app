using GymManagementBLL.Common;
using GymManagementBLL.Services.Interfaces;
using GymManagementBLL.ViewModels.BookingViewModels;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class BookingController(IBookingService bookingService) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken ct)
            => View(await bookingService.GetScheduleAsync(ct));

        public async Task<IActionResult> Roster(int id, CancellationToken ct)
        {
            var roster = await bookingService.GetRosterAsync(id, ct);
            return roster is null ? NotFound() : View(roster);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBookingViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                this.FlashError("Select a member to book.");
                return RedirectToAction(nameof(Roster), new { id = model.SessionId });
            }

            var result = await bookingService.CreateNewBookingAsync(model, ct);
            this.Flash(result, "Member booked into the session.");
            return RedirectToAction(nameof(Roster), new { id = model.SessionId });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int memberId, int sessionId, CancellationToken ct)
        {
            var result = await bookingService.CancelBookingAsync(memberId, sessionId, ct);
            this.Flash(result, "Booking cancelled.");
            return RedirectToAction(nameof(Roster), new { id = sessionId });
        }

        [HttpPost]
        public async Task<IActionResult> Attended(int memberId, int sessionId, CancellationToken ct)
        {
            var result = await bookingService.MarkAttendedAsync(memberId, sessionId, ct);
            this.Flash(result, "Attendance recorded.");
            return RedirectToAction(nameof(Roster), new { id = sessionId });
        }
    }
}
