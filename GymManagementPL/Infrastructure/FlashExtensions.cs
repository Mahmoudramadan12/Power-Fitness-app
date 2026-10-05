using GymManagementBLL.Common;
using Microsoft.AspNetCore.Mvc;

namespace GymManagementPL.Infrastructure
{
    public static class FlashExtensions
    {
        public const string SuccessKey = "SuccessMessage";
        public const string ErrorKey = "ErrorMessage";

        public static void FlashSuccess(this Controller c, string message) => c.TempData[SuccessKey] = message;
        public static void FlashError(this Controller c, string message) => c.TempData[ErrorKey] = message;

        /// <summary>Flashes the success message or the failure reason, and returns whether it succeeded.</summary>
        public static bool Flash(this Controller c, Result result, string successMessage)
        {
            if (result.Success) c.FlashSuccess(successMessage);
            else c.FlashError(result.Error ?? "Something went wrong.");
            return result.Success;
        }

        /// <summary>Shows a service failure on the form (instead of a flash) so the user keeps their input.</summary>
        public static void AddServiceError(this Controller c, Result result)
        {
            c.ModelState.AddModelError(string.Empty, result.Error ?? "Something went wrong.");
        }
    }
}
