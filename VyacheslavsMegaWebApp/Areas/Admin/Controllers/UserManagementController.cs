using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Areas.Admin.Models;
using VyacheslavsMegaWebApp.Data;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly SkinStorageService _skinStorage;
        private readonly IWebHostEnvironment _env;

        public UserManagementController(
            UserManager<VmsIdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            LauncherSessionRepository sessionRepository,
            SkinStorageService skinStorage,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _sessionRepository = sessionRepository;
            _skinStorage = skinStorage;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            return View(new UserManagerViewModel()
            {
                Users = await _userManager.Users.ToListAsync()
            });
        }

        public async Task<IActionResult> UserDetails(string linkId)
        {
            if (string.IsNullOrEmpty(linkId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(linkId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

            LauncherSession? lastSession = await _sessionRepository.GetLastSessionForUserAsync(user.Id);

            var username = user.UserName ?? string.Empty;
            var skinModel = _skinStorage.GetSkinModel(username);
            var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var skinDiskPath = Path.Combine(root, "uploads", "skins", username + ".png");
            var capeDiskPath = Path.Combine(root, "uploads", "capes", username + ".png");

            var model = new UserDetailsViewModel
            {
                Id = user.Id,
                UserName = username,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                IsAdmin = roles.Contains("Admin"),
                LockoutEnabled = user.LockoutEnabled,
                IsLockedOut = isLocked,
                LastHwid = lastSession?.ClientToken,
                LastSessionDate = lastSession?.ExpiresAt,
                HasSkin = System.IO.File.Exists(skinDiskPath),
                HasCape = System.IO.File.Exists(capeDiskPath),
                SkinUrl = "/uploads/skins/" + username + ".png",
                CapeUrl = "/uploads/capes/" + username + ".png",
                SkinModel = skinModel
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UserDetails(UserDetailsViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.EmailConfirmed = model.EmailConfirmed;

            if (model.IsLockedOut)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            }
            else
            {
                user.LockoutEnd = null;
            }

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            if (model.IsAdmin && !roles.Contains("Admin"))
            {
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                }
                await _userManager.AddToRoleAsync(user, "Admin");
            }
            else if (!model.IsAdmin && roles.Contains("Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }

            TempData["Success"] = $"Данные пользователя {user.UserName} сохранены.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ResetHwid(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            await _sessionRepository.DeleteSessionsForUserAsync(id);
            TempData["Success"] = "Привязка HWID и активные сессии лаунчера успешно сброшены.";

            return RedirectToAction(nameof(UserDetails), new { linkId = id });
        }
    }
}
