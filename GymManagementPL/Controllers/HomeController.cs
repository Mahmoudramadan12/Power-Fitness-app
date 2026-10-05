using GymManagementBLL.Services.Interfaces;
using GymManagementPL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GymManagementPL.Controllers
{
    [Authorize]
    public class HomeController(IAnalyticsService analytics) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken ct)
            => View(await analytics.GetDashboardAsync(ct));

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        /// <summary>Friendly 403/404/etc. pages (wired up through UseStatusCodePagesWithReExecute).</summary>
        [AllowAnonymous, ActionName("StatusCode")]
        public IActionResult HttpStatus(int code = 404)
        {
            Response.StatusCode = code;
            return View(new ErrorViewModel { StatusCode = code, RequestId = HttpContext.TraceIdentifier });
        }
    }
}
