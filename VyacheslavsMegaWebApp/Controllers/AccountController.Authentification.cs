using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Controllers
{
    public partial class AccountController
    {
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl, int? launcherPort)
        {
            ViewBag.ReturnUrl = returnUrl ?? "/";
            ViewBag.LauncherPort = launcherPort;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, int? launcherPort)
        {
            ViewBag.LauncherPort = launcherPort;
            if (!ModelState.IsValid)
                return View(model);

            VmsIdentityUser? user = await _userManager.FindByNameAsync(model.UserName);
            if (user is null)
            {
                ModelState.AddModelError(UserNameNotFound, string.Format(_localizer[UserNameNotFound], model.UserName));
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Для входа необходимо подтвердить адрес электронной почты! Проверьте ваш почтовый ящик.");
                return View(model);
            }

            await _signInManager.SignOutAsync();
            Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (signInResult.Succeeded)
            {
                if (launcherPort.HasValue)
                {
                    string sessionKey = await _sessionRepository.CreateSessionKey(user);
                    string skinModel = _skinStorage.GetSkinModel(user.UserName);
                    return Redirect($"http://127.0.0.1:{launcherPort.Value}/callback?sessionKey={sessionKey}&username={Uri.EscapeDataString(user.UserName)}&skinModel={skinModel}");
                }
                return Redirect(returnUrl ?? "/");
            }

            bool wrongParrwordCheck = !(signInResult.IsLockedOut || signInResult.IsNotAllowed);
            if (wrongParrwordCheck)
            {
                ModelState.AddModelError(WrongLoginOrPassword, _localizer[WrongLoginOrPassword]);
            }

            if (signInResult.IsLockedOut)
                ModelState.AddModelError(UserIsLockedOut, string.Format(_localizer[UserIsLockedOut], model.UserName));
            if (signInResult.IsNotAllowed)
                ModelState.AddModelError(UserIsNotAllowed, string.Format(_localizer[UserIsNotAllowed], model.UserName));
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Register(int? launcherPort)
        {
            ViewBag.LauncherPort = launcherPort;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, int? launcherPort)
        {
            ViewBag.LauncherPort = launcherPort;
            if (!ModelState.IsValid)
                return View(model);

            VmsIdentityUser user = new VmsIdentityUser { Email = model.Email, UserName = model.UserName };
            IdentityResult result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                TempData["LauncherPort"] = launcherPort;
                return RedirectToAction(nameof(SendEmailConfirmationToken));
            }

            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            VmsIdentityUser? user;

            switch (model.GetUserInputType())
            {
                case ForgotPasswordViewModel.UserInputType.Username:
                    user = await _userManager.FindByNameAsync(model.UserInput);
                    break;
                case ForgotPasswordViewModel.UserInputType.Email:
                    user = await _userManager.FindByEmailAsync(model.UserInput);
                    break;
                default:
                    ModelState.AddModelError(WrongInput, string.Format(_localizer[WrongInput], model.UserInput));
                    return View(model);
            }

            if (user is null)
            {
                ModelState.AddModelError(UserNotFound, _localizer[UserNotFound]);
                return View(model);
            }
            string token = await _userManager.GeneratePasswordResetTokenAsync(user);
            string userId = user.Id;
            string url = Url.Action(nameof(ResetPassword), "Account", new { userId, token }, Request.Scheme)!;
            await _emailService.SendEmail(await _emailRepository.GetPasswordResetMessage(user, url));
            TempData["email"] = user.Email;
            TempData["resetUrl"] = url;
            return RedirectToAction(nameof(EmailSent));
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            VmsIdentityUser? user = await _userManager.FindByIdAsync(model.UserId);

            if (user is null)
                return NotFound(_localizer[UserNotFound]);

            IdentityResult result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return View(model);
        }
    }
}
