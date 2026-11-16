using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MimeKit;
using VyacheslavsMegaWebApp.Data;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.EmailService;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Data.Repositories.Files;
using VyacheslavsMegaWebApp.Models;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Controllers
{
    [Authorize]
    public partial class AccountController : Controller
    {
        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly SignInManager<VmsIdentityUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly SkinStorageService _skinStorage;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly FriendshipsRepository _friendshipsRepository;
        private readonly EmailRepository _emailRepository;

        #region String constants

        private const string UserNotFound = "UserNotFound";
        private const string UserNameNotFound = "UserNameNotFound";
        private const string UserIsLockedOut = "UserIsLockedOut";
        private const string UserIsNotAllowed = "UserIsNotAllowed";
        private const string NotAuthorized = "NotAuthorized";
        private const string WrongInput = "WrongInput";
        private const string WrongLoginOrPassword = "WrongLoginOrPassword";

        #endregion

        public AccountController(
            UserManager<VmsIdentityUser> userManager,
            SignInManager<VmsIdentityUser> signInManager,
            EmailService emailService,
            IStringLocalizer<AccountController> localizer,
            SkinStorageService skinStorage,
            LauncherSessionRepository sessionRepository,
            FriendshipsRepository friendshipsRepository,
            EmailRepository emailRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _localizer = localizer;
            _skinStorage = skinStorage;
            _sessionRepository = sessionRepository;
            _friendshipsRepository = friendshipsRepository;
            _emailRepository = emailRepository;
        }
        /*
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LauncherAuth(int port = 25588)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                VmsIdentityUser? user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (!user.EmailConfirmed)
                    {
                        TempData["LauncherPort"] = port;
                        return RedirectToAction(nameof(SendEmailConfirmationToken));
                    }

                    ViewBag.LauncherPort = port;
                    ViewBag.UserName = user.UserName;
                    ViewBag.Email = user.Email;
                    return View("LauncherAuthConfirm");
                }
            }

            return RedirectToAction(nameof(Login), new { launcherPort = port });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmLauncherAuth(int port)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction(nameof(Login), new { launcherPort = port });
            }

            string sessionKey = await _sessionRepository.CreateSessionKey(user);
            string skinModel = _skinStorage.GetSkinModel(user.UserName);
            return Redirect($"http://127.0.0.1:{port}/callback?sessionKey={sessionKey}&username={Uri.EscapeDataString(user.UserName)}&skinModel={skinModel}");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SwitchLauncherAccount(int port)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login), new { launcherPort = port });
        }

        [AllowAnonymous]
        public IActionResult Login(string? returnUrl, int? launcherPort)
        {
            ViewBag.ReturnUrl = returnUrl ?? "/";
            ViewBag.LauncherPort = launcherPort;
            return View(new LoginViewModel());
        }
        
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateLauncherSession()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var sessionKey = await _sessionRepository.CreateSessionKey(user);

            return Ok(new
            {
                success = true,
                sessionKey,
                expiresAt = DateTime.UtcNow.AddDays(14).ToString("dd.MM.yyyy HH:mm")
            });
        }
        */




        [AllowAnonymous]
        public IActionResult EmailSent()
        {
            return View();
        }

        #region Friends System & Public Profiles

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            List<Friendship> incoming = await _friendshipsRepository.GetIncomingPendingRequests(user.Id);

            var list = new List<object>();
            foreach (var f in incoming)
            {
                var reqUser = await _userManager.FindByIdAsync(f.RequesterId);
                var rName = reqUser?.UserName ?? "Игрок";
                var (siteName, mcUser, _) = _skinStorage.GetUserProfileData(rName);

                //TODO: make common notification
                list.Add(new
                {
                    id = f.Id,
                    type = "friend_request",
                    title = "Запрос в друзья",
                    friendshipId = f.Id,
                    userId = f.RequesterId,
                    username = string.IsNullOrEmpty(siteName) ? rName : siteName,
                    minecraftUsername = string.IsNullOrEmpty(mcUser) ? rName : mcUser,
                    skinUrl = _skinStorage.GetSkinUrl(rName),
                    date = f.CreatedAt.ToString("dd.MM.yyyy")
                });
            }

            list.Add(new
            {
                id = 999999,
                type = "system",
                title = "Система VEC",
                message = "Добро пожаловать! Регистрация прошла успешно.",
                date = DateTime.UtcNow.ToString("dd.MM.yyyy")
            });

            return Json(new { success = true, count = incoming.Count, notifications = list });
        }
        //TODO: fix public profile
        /*
        [AllowAnonymous]
        [HttpGet("/user/{username}")]
        [HttpGet("/profile/{username}")]
        public async Task<IActionResult> PublicProfile(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return RedirectToAction("Index", "Home");

            var targetUser = await _userManager.FindByNameAsync(username);
            if (targetUser is null)
            {
                var allUsers = _userManager.Users.ToList();
                targetUser = allUsers.FirstOrDefault(u =>
                {
                    var (_, mc, _) = _skinStorage.GetUserProfileData(u.UserName);
                    return string.Equals(mc, username, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (targetUser is null) return NotFound("Пользователь не найден.");

            VmsIdentityUser? currentUser = await _userManager.GetUserAsync(User);
            bool isSelf = currentUser != null && currentUser.Id == targetUser.Id;

            if (isSelf)
            {
                return RedirectToAction(nameof(Profile));
            }

            var (siteName, mcUser, createdAt) = _skinStorage.GetUserProfileData(targetUser.UserName);

            int friendshipsCount = await _friendshipsRepository.GetAcceptedFriendshipsCount(currentUser.Id);

            string status = "None";
            int? fId = null;

            if (currentUser != null)
            {
                var relation = _db.Friendships.FirstOrDefault(f =>
                    (f.RequesterId == currentUser.Id && f.AddresseeId == targetUser.Id) ||
                    (f.RequesterId == targetUser.Id && f.AddresseeId == currentUser.Id));

                if (relation != null)
                {
                    fId = relation.Id;
                    if (relation.Status == Friendship.FriendshipStatus.Accepted) status = "Friends";
                    else if (relation.Status == Friendship.FriendshipStatus.Pending)
                    {
                        status = relation.RequesterId == currentUser.Id ? "PendingOutgoing" : "PendingIncoming";
                    }
                }
            }

            var model = new PublicProfileViewModel
            {
                UserId = targetUser.Id,
                Username = string.IsNullOrEmpty(siteName) ? targetUser.UserName : siteName,
                MinecraftUsername = string.IsNullOrEmpty(mcUser) ? targetUser.UserName : mcUser,
                CreatedAt = createdAt,
                FriendsCount = friendshipsCount,
                BannerUrl = _skinStorage.GetBannerUrl(targetUser.UserName),
                AvatarUrl = _skinStorage.GetAvatarUrl(targetUser.UserName),
                SkinUrl = _skinStorage.GetSkinUrl(targetUser.UserName),
                SkinModel = _skinStorage.GetSkinModel(targetUser.UserName),
                CapeUrl = _skinStorage.GetCapeUrl(targetUser.UserName),
                IsSelf = false,
                FriendshipStatus = status,
                FriendshipId = fId
            };

            return View("PublicProfile", model);
        }
        */
        #endregion
    }
}