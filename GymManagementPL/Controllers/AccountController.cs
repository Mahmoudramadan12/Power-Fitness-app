using GymManagementBLL.ViewModels.AccountViewModels;
using GymManagementDAL.Entities;
using GymManagementPL.Extensions;
using GymManagementPL.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymManagementPL.Controllers
{
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger) : Controller
    {
        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous]
        [EnableRateLimiting(GymManagementPL.Extensions.ServiceCollectionExtensions.LoginRateLimitPolicy)]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Same message for "no such user" and "wrong password" so the form can't be used to enumerate accounts.
            var user = await userManager.FindByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                logger.LogInformation("User {Email} signed in.", user.Email);
                return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                    ? LocalRedirect(model.ReturnUrl)
                    : RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User {Email} is locked out.", user.Email);
                ModelState.AddModelError(string.Empty, "Too many failed attempts. This account is locked for a few minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            return View(model);
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet, Authorize]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost, Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction(nameof(Login));

            var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await signInManager.RefreshSignInAsync(user);
            this.FlashSuccess("Your password was changed.");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
