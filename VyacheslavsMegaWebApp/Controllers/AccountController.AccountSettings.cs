using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Models;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Controllers
{
    public partial class AccountController
    {
        [Authorize]
        public async Task<IActionResult> AccountSettings()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            var roles = await _userManager.GetRolesAsync(user);
            // TODO: localize roles
            string roleBadge = roles.Contains("Admin") ? "Администратор" : (roles.Contains("Moderator") ? "Модератор" : "Игрок VMS");

            AccountSettingsViewModel model = new AccountSettingsViewModel
            {
                Username = user.UserName,
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                UserId = user.Id,
                RoleBadge = roleBadge
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // TODO: localize strings
            if (!ModelState.IsValid)
                return PartialView("_ChangePasswordPartial", model);

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["PasswordSuccess"] = "Пароль успешно изменен!";
                return Json(new { redirectUrl = Url.Action(nameof(AccountSettings), "Account") });
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return PartialView("_ChangePasswordPartial", model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RequestEmailChange(ChangeEmailViewModel model)
        {
            // TODO: Localize strings
            if (!VmsUserValidator.ValidateEmail(model.NewEmail))
            {
                ModelState.AddModelError("1", "Адрес почты написан неверно или данный почтовый домен запрещён");
                return PartialView("_ChangeEmailPartial", model);
            }

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized();

            if (string.Equals(user.Email, model.NewEmail, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("2", "Новый адрес совпадает с текущим.");
                return PartialView("_ChangeEmailPartial", model);
            }

            VmsIdentityUser? existing = await _userManager.FindByEmailAsync(model.NewEmail);
            if (existing != null)
            {
                ModelState.AddModelError("3", "Данный адрес электронной почты уже используется другим аккаунтом.");
                return PartialView("_ChangeEmailPartial", model);
            }

            if (user.EmailConfirmedCodeCreationTime is null ||
                DateTime.UtcNow - user.EmailConfirmedCodeCreationTime >= new TimeSpan(0, 3, 0))
            {
                string token = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
                string userId = user.Id;
                string url = Url.Action(nameof(ConfirmEmailChange), "Account", new { userId, model.NewEmail, token }, Request.Scheme)!;

                bool success = await _emailService.SendEmail(await _emailRepository.GetEmailChangeMessage(user, url));

                user.EmailConfirmedCodeCreationTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                if (success)
                {
                    TempData["email"] = user.Email;
                    return Json(new { redirectUrl = Url.Action(nameof(EmailSent), "Account") });
                }
            }

            // TODO: Make not success page
            return NotFound();
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(string userId, string newEmail, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || 
                string.IsNullOrWhiteSpace(newEmail) ||
                string.IsNullOrWhiteSpace(token))
                return BadRequest();

            VmsIdentityUser? user = await _userManager.FindByIdAsync(userId);
            if (user is null) return NotFound();

            IdentityResult result = await _userManager.ChangeEmailAsync(user, newEmail, token);

            if (result.Succeeded)
            {
                if (!_signInManager.IsSignedIn(User))
                    await _signInManager.SignInAsync(user, false);
                // TODO: Remove TempData but send notifications
                TempData["EmailSuccess"] = "Электронная почта успешно обновлена!";
            }
            else
            {
                // also here
                TempData["EmailError"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(AccountSettings));
        }

        [Authorize]
        public async Task<IActionResult> SendEmailConfirmationToken()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            int? launcherPort = TempData["LauncherPort"] as int?;
            TempData.Keep("LauncherPort");

            if (user.EmailConfirmedCodeCreationTime is null ||
                DateTime.UtcNow - user.EmailConfirmedCodeCreationTime >= new TimeSpan(0, 3, 0))
            {
                string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                string userId = user.Id;
                string url = Url.Action(nameof(ConfirmEmail), "Account", new { userId, token, launcherPort }, Request.Scheme)!;

                await _emailService.SendEmail(await _emailRepository.GetEmailConfirmMessage(user, url));
                
                user.EmailConfirmedCodeCreationTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                TempData["confirmUrl"] = url;
            }

            TempData["email"] = user.Email;
            return RedirectToAction(nameof(EmailSent));
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token, int? launcherPort)
        {
            VmsIdentityUser? user = await _userManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound();

            IdentityResult confirmationResult = await _userManager.ConfirmEmailAsync(user, token);

            if (confirmationResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                if (launcherPort.HasValue)
                {
                    string sessionKey = await _sessionRepository.CreateSessionKey(user);
                    string skinModel = _skinStorage.GetSkinModel(user.UserName);
                    return Redirect($"http://127.0.0.1:{launcherPort.Value}/callback?sessionKey={sessionKey}&username={Uri.EscapeDataString(user.UserName)}&skinModel={skinModel}");
                }
                return RedirectToAction("Index", "Home");
            }

            return BadRequest();
        }
    }
}
