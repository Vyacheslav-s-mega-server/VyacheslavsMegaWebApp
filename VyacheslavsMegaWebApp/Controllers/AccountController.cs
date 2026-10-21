using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
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
using VyacheslavsMegaWebApp.Models;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private static readonly ConcurrentDictionary<string, (string Code, string NewEmail, DateTime ExpiresAt)> _emailChangeCodes = new();

        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly SignInManager<VmsIdentityUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly IStringLocalizer<AccountController> _localizer;
        private readonly SkinStorageService _skinStorage;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly MinecraftLicenseService _licenseService;
        private readonly FriendshipsRepository _friendshipsRepository;
        private readonly CapesRepository _capesRepository;

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
            MinecraftLicenseService licenseService,
            FriendshipsRepository friendshipsRepository,
            CapesRepository capesRepository) 
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _localizer = localizer;
            _skinStorage = skinStorage;
            _sessionRepository = sessionRepository;
            _licenseService = licenseService;
            _friendshipsRepository = friendshipsRepository;
            _capesRepository = capesRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LauncherAuth(int? port = 25588)
        {
            int targetPort = port ?? 25588;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (!user.EmailConfirmed)
                    {
                        TempData["LauncherPort"] = targetPort;
                        return RedirectToAction(nameof(SendEmailConfirmationToken));
                    }

                    ViewBag.LauncherPort = targetPort;
                    ViewBag.UserName = user.UserName;
                    ViewBag.Email = user.Email;
                    return View("LauncherAuthConfirm");
                }
            }

            return RedirectToAction(nameof(Login), new { launcherPort = targetPort });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmLauncherAuth(int port)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { launcherPort = port });
            }

            string sessionKey = await _sessionRepository.CreateSessionKey(user);
            string skinModel = _skinStorage.GetSkinModel(user.UserName);
            string licenseParam = $"&hasLicense={user.HasMinecraftLicense}&mcGamertag={Uri.EscapeDataString(user.MicrosoftGamertag ?? "")}&mcUuid={Uri.EscapeDataString(user.MicrosoftUuid ?? "")}";
            return Redirect($"http://127.0.0.1:{port}/callback?sessionKey={sessionKey}&username={Uri.EscapeDataString(user.UserName)}&skinModel={skinModel}{licenseParam}");
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

        [Authorize]
        public async Task<IActionResult> AccountSettings()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            var roles = await _userManager.GetRolesAsync(user);
            string roleBadge = roles.Contains("Admin") ? "Администратор" : (roles.Contains("Moderator") ? "Модератор" : "Игрок VEC");

            List<LauncherSession> userSessions = await _sessionRepository.GetSessionsForUserAsync(user.Id);
            List<LauncherSessionInfo> sessions = userSessions
                .Select(s => new LauncherSessionInfo
                {
                    SessionKey = s.SessionKey,
                    ExpiresAt = s.ExpiresAt
                })
                .ToList();

            var model = new AccountSettingsViewModel
            {
                Username = user.UserName,
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                UserId = user.Id,
                RoleBadge = roleBadge,
                LauncherSessions = sessions
            };

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            var roles = await _userManager.GetRolesAsync(user);
            string roleBadge = roles.Contains("Admin") ? "Администратор" : (roles.Contains("Moderator") ? "Модератор" : "Игрок VEC");

            var equippedCapeId = _skinStorage.DetectEquippedCapeId(user.UserName);

            var availableCapes = new List<CapeItemViewModel>();
            var mojangCapes = new List<MojangCapeInfo>();
            try
            {
                List<CustomCape> publicCapes = await _capesRepository.GetPublicCapesAsync();
                List<UserCapeOwnership> userOwnerships = await _capesRepository.GetUserCapeOwnershipsAsync(user.Id);
                List<CustomCape> ownedCapes = userOwnerships
                    .Where(o => o.Cape != null)
                    .Select(o => o.Cape!)
                    .ToList();

                var customCapes = publicCapes.Concat(ownedCapes).DistinctBy(c => c.Id).ToList();

                var vecCape = customCapes.FirstOrDefault(c => c.Slug == "vec");
                var isVecEquipped = equippedCapeId == "vec_default" || (vecCape != null && equippedCapeId == $"custom_{vecCape.Id}");

                availableCapes.Add(new CapeItemViewModel
                {
                    Id = "vec_default",
                    Name = vecCape?.Name ?? "Плащ VEC",
                    Description = vecCape?.Description ?? "Официальный плащ проекта VEC.",
                    PreviewUrl = vecCape != null ? $"/textures/{vecCape.TextureHash}" : "/img/capes/cape_vec.png",
                    DownloadUrl = vecCape != null ? $"/textures/{vecCape.TextureHash}" : "/img/capes/cape_vec.png",
                    IsOfficial = true,
                    IsEquipped = isVecEquipped
                });

                foreach (var c in customCapes.Where(c => c.Slug != "vec"))
                {
                    var capeId = $"custom_{c.Id}";
                    availableCapes.Add(new CapeItemViewModel
                    {
                        Id = capeId,
                        Name = c.Name,
                        Description = c.Description ?? "Эксклюзивный плащ VEC",
                        PreviewUrl = $"/textures/{c.TextureHash}",
                        DownloadUrl = $"/textures/{c.TextureHash}",
                        IsOfficial = true,
                        IsEquipped = equippedCapeId == capeId
                    });
                }
            }
            catch { }

            if (user.HasMinecraftLicense && !string.IsNullOrWhiteSpace(user.OwnedMojangCapesJson))
            {
                try
                {
                    mojangCapes = JsonSerializer.Deserialize<List<MojangCapeInfo>>(user.OwnedMojangCapesJson) ?? new();
                    foreach (var mc in mojangCapes)
                    {
                        var capeId = $"mojang_{mc.Id}";
                        availableCapes.Add(new CapeItemViewModel
                        {
                            Id = capeId,
                            Name = !string.IsNullOrWhiteSpace(mc.Alias) ? mc.Alias : "Плащ Mojang",
                            Description = "Официальный лицензионный плащ Minecraft (Mojang / Microsoft)",
                            PreviewUrl = $"/img/capes/mojang/{mc.Id}.png",
                            DownloadUrl = $"/img/capes/mojang/{mc.Id}.png",
                            IsOfficial = true,
                            IsEquipped = equippedCapeId == capeId
                        });
                    }
                }
                catch { }
            }

            List<LauncherSession> userSessions = await _sessionRepository.GetSessionsForUserAsync(user.Id);
            List<LauncherSessionInfo> sessions = userSessions
                .Select(s => new LauncherSessionInfo
                {
                    SessionKey = s.SessionKey,
                    ExpiresAt = s.ExpiresAt
                })
                .ToList();

            var (siteName, mcUser, createdAt) = _skinStorage.GetUserProfileData(user.UserName);

            List<Friendship> friendships = await _friendshipsRepository.GetAllFriendshipsForUserAsync(user.Id);

            List<Friendship> accepted = friendships.Where(f => f.Status == FriendshipStatus.Accepted).ToList();
            List<Friendship> incoming = friendships.Where(f => f.AddresseeId == user.Id && f.Status == FriendshipStatus.Pending).ToList();
            List<Friendship> outgoing = friendships.Where(f => f.RequesterId == user.Id && f.Status == FriendshipStatus.Pending).ToList();

            List<string> userIdsToFetch = friendships
                .Select(f => f.RequesterId == user.Id ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();

            Dictionary<string, string?> usersMap = await _userManager.Users
                .Where(u => userIdsToFetch.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            FriendItemViewModel BuildFriendItem(Friendship f, string otherUserId, bool isIncoming = false)
            {
                var otherName = usersMap.TryGetValue(otherUserId, out var uname) && !string.IsNullOrEmpty(uname) ? uname : "Игрок";
                var (fSiteName, fMcUser, _) = _skinStorage.GetUserProfileData(otherName);
                return new FriendItemViewModel
                {
                    FriendshipId = f.Id,
                    UserId = otherUserId,
                    Username = string.IsNullOrEmpty(fSiteName) ? otherName : fSiteName,
                    MinecraftUsername = string.IsNullOrEmpty(fMcUser) ? otherName : fMcUser,
                    AvatarUrl = _skinStorage.GetAvatarUrl(otherName),
                    SkinUrl = _skinStorage.GetSkinUrl(otherName),
                    Since = f.UpdatedAt ?? f.CreatedAt,
                    IsIncoming = isIncoming
                };
            }

            var friendsList = accepted.Select(f => BuildFriendItem(f, f.RequesterId == user.Id ? f.AddresseeId : f.RequesterId)).ToList();
            var incomingList = incoming.Select(f => BuildFriendItem(f, f.RequesterId, isIncoming: true)).ToList();
            var outgoingList = outgoing.Select(f => BuildFriendItem(f, f.AddresseeId)).ToList();

            var model = new ProfileViewModel
            {
                Username = user.UserName,
                SiteName = siteName,
                MinecraftUsername = mcUser,
                CreatedAt = createdAt,
                FriendsCount = friendsList.Count,
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                UserId = user.Id,
                RoleBadge = roleBadge,
                SkinUrl = _skinStorage.GetSkinUrl(user.UserName),
                SkinModel = _skinStorage.GetSkinModel(user.UserName),
                HasCustomSkin = _skinStorage.HasCustomSkin(user.UserName),
                CapeUrl = _skinStorage.GetCapeUrl(user.UserName),
                HasCape = _skinStorage.HasCape(user.UserName),
                EquippedCapeId = equippedCapeId,
                AvatarUrl = _skinStorage.GetAvatarUrl(user.UserName),
                BannerUrl = _skinStorage.GetBannerUrl(user.UserName),
                HasCustomBanner = _skinStorage.HasCustomBanner(user.UserName),
                AvailableCapes = availableCapes,
                LauncherSessions = sessions,
                Friends = friendsList,
                IncomingRequests = incomingList,
                OutgoingRequests = outgoingList,
                HasMinecraftLicense = user.HasMinecraftLicense,
                MicrosoftGamertag = user.MicrosoftGamertag,
                MicrosoftUuid = user.MicrosoftUuid,
                MicrosoftLicenseLinkedAt = user.MicrosoftLicenseLinkedAt,
                MojangCapes = mojangCapes
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (avatarFile == null || avatarFile.Length == 0)
                return BadRequest(new { error = "Файл аватара не выбран." });

            var ext = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
                return BadRequest(new { error = "Аватар должен быть в формате PNG, JPG или WEBP." });

            if (avatarFile.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Размер файла не должен превышать 5 МБ." });

            var ok = await _skinStorage.SaveAvatarAsync(user.UserName, avatarFile);
            if (!ok) return BadRequest(new { error = "Не удалось сохранить аватар." });

            return Ok(new
            {
                success = true,
                message = "Аватар профиля успешно обновлен!",
                avatarUrl = _skinStorage.GetAvatarUrl(user.UserName)
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateProfileInfo(string siteName, string minecraftUsername)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            siteName = string.IsNullOrWhiteSpace(siteName) ? user.UserName : siteName.Trim();
            minecraftUsername = string.IsNullOrWhiteSpace(minecraftUsername) ? user.UserName : minecraftUsername.Trim();

            _skinStorage.SaveUserProfileData(user.UserName, siteName, minecraftUsername);

            TempData["ProfileSuccess"] = "Информация профиля успешно сохранена!";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadBanner(IFormFile bannerFile)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (bannerFile == null || bannerFile.Length == 0)
                return BadRequest(new { error = "Файл баннера не выбран." });

            var ext = Path.GetExtension(bannerFile.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp")
                return BadRequest(new { error = "Баннер должен быть в формате PNG, JPG или WEBP." });

            if (bannerFile.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "Размер файла не должен превышать 10 МБ." });

            var ok = await _skinStorage.SaveBannerAsync(user.UserName, bannerFile);
            if (!ok) return BadRequest(new { error = "Не удалось сохранить баннер." });

            return Ok(new
            {
                success = true,
                message = "Баннер профиля успешно обновлен!",
                bannerUrl = _skinStorage.GetBannerUrl(user.UserName)
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ResetBanner()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            _skinStorage.ResetBanner(user.UserName);

            return Ok(new
            {
                success = true,
                message = "Баннер сброшен на стандартный.",
                bannerUrl = _skinStorage.GetBannerUrl(user.UserName)
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateUsername(string newUsername)
        {
            if (string.IsNullOrWhiteSpace(newUsername))
            {
                TempData["ProfileError"] = "Имя пользователя не может быть пустым.";
                return RedirectToAction(nameof(Profile));
            }

            newUsername = newUsername.Trim();
            if (newUsername.Length < 3 || newUsername.Length > 20)
            {
                TempData["ProfileError"] = "Длина никнейма должна быть от 3 до 20 символов.";
                return RedirectToAction(nameof(Profile));
            }

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (user.UserName == newUsername)
            {
                TempData["ProfileSuccess"] = "Никнейм не изменен.";
                return RedirectToAction(nameof(Profile));
            }

            var existing = await _userManager.FindByNameAsync(newUsername);
            if (existing != null && existing.Id != user.Id)
            {
                TempData["ProfileError"] = "Пользователь с таким никнеймом уже существует.";
                return RedirectToAction(nameof(Profile));
            }

            user.UserName = newUsername;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["ProfileSuccess"] = "Имя пользователя успешно обновлено!";
            }
            else
            {
                TempData["ProfileError"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadSkin(IFormFile skinFile, string model = "classic")
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (skinFile == null || skinFile.Length == 0)
                return BadRequest(new { error = "Файл скина не выбран." });

            if (!skinFile.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Скин должен быть файлом в формате .png." });

            if (skinFile.Length > 2 * 1024 * 1024)
                return BadRequest(new { error = "Размер файла не должен превышать 2 МБ." });

            var success = await _skinStorage.SaveSkinAsync(user.UserName, skinFile, model);
            if (!success) return BadRequest(new { error = "Не удалось сохранить скин." });

            return Ok(new
            {
                success = true,
                message = "Скин успешно загружен и применен в VEC Launcher!",
                skinUrl = _skinStorage.GetSkinUrl(user.UserName),
                model = _skinStorage.GetSkinModel(user.UserName)
            });
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateSkinModel(string model)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var cleanModel = string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase) ? "slim" : "classic";
            _skinStorage.SetSkinModel(user.UserName, cleanModel);

            return Ok(new
            {
                success = true,
                model = cleanModel
            });
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ResetSkin()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            _skinStorage.ResetSkin(user.UserName);

            return Ok(new
            {
                success = true,
                message = "Скин сброшен на стандартный.",
                skinUrl = _skinStorage.GetSkinUrl(user.UserName),
                model = _skinStorage.GetSkinModel(user.UserName)
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EquipCape([FromForm] string? capeId)
        {
            if (string.IsNullOrEmpty(capeId))
            {
                capeId = Request.Form["capeId"].FirstOrDefault() ?? Request.Query["capeId"].FirstOrDefault();
            }

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                var sessionKey = Request.Headers["X-Session-Key"].FirstOrDefault()
                                 ?? Request.Query["sessionKey"].FirstOrDefault();
                if (!string.IsNullOrEmpty(sessionKey))
                {
                    var session = await _sessionRepository.GetSessionByToken(sessionKey);
                    if (session != null)
                    {
                        user = session.User ?? await _userManager.FindByIdAsync(session.UserId);
                    }
                }
            }
            if (user == null)
            {
                var username = Request.Headers["X-Username"].FirstOrDefault()
                               ?? Request.Query["username"].FirstOrDefault()
                               ?? Request.Headers["X-User"].FirstOrDefault()
                               ?? Request.Form["username"].FirstOrDefault();
                if (!string.IsNullOrEmpty(username))
                {
                    user = await _userManager.FindByNameAsync(username);
                }
            }
            if (user is null)
            {
                return StatusCode(401, new { error = "Пользователь не найден или сессия недействительна." });
            }

            if (string.IsNullOrWhiteSpace(capeId))
                return BadRequest(new { error = "Плащ не указан." });

            if (capeId.StartsWith("mojang_", StringComparison.OrdinalIgnoreCase))
            {
                if (!user.HasMinecraftLicense)
                {
                    return BadRequest(new { error = "Для выбора плащей Mojang требуется привязать лицензию Minecraft." });
                }
            }
            else if (capeId.StartsWith("custom_", StringComparison.OrdinalIgnoreCase))
            {
                var idStr = capeId.Substring(7);
                if (int.TryParse(idStr, out var cId))
                {
                    CustomCape? custom = await _capesRepository.GetCapeByIdAsync(cId);
                    if (custom == null) return BadRequest(new { error = "Плащ не найден." });
                    if (!custom.IsPublic)
                    {
                        bool isOwned = await _capesRepository.IsCapeOwnedByUserAsync(user.Id, cId);
                        if (!isOwned) return BadRequest(new { error = "У вас нет этого эксклюзивного плаща в гардеробе." });
                    }
                }
            }

            var ok = _skinStorage.EquipLauncherCape(user.UserName, capeId);
            if (!ok) return BadRequest(new { error = "Плащ не найден." });

            return Ok(new
            {
                success = true,
                message = "Плащ успешно надет и синхронизирован с VEC Launcher!",
                capeUrl = _skinStorage.GetCapeUrl(user.UserName),
                equippedCapeId = capeId
            });
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveCape()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                var sessionKey = Request.Headers["X-Session-Key"].FirstOrDefault()
                                 ?? Request.Query["sessionKey"].FirstOrDefault();
                if (!string.IsNullOrEmpty(sessionKey))
                {
                    var session = await _sessionRepository.GetSessionByToken(sessionKey);
                    if (session != null)
                    {
                        user = session.User ?? await _userManager.FindByIdAsync(session.UserId);
                    }
                }
            }
            if (user == null)
            {
                var username = Request.Headers["X-Username"].FirstOrDefault()
                               ?? Request.Query["username"].FirstOrDefault()
                               ?? Request.Headers["X-User"].FirstOrDefault();
                if (!string.IsNullOrEmpty(username))
                {
                    user = await _userManager.FindByNameAsync(username);
                }
            }
            if (user is null)
            {
                return StatusCode(401, new { error = "Пользователь не найден или сессия недействительна." });
            }

            _skinStorage.RemoveCape(user.UserName);

            return Ok(new
            {
                success = true,
                message = "Плащ снят.",
                capeUrl = (string?)null,
                equippedCapeId = (string?)null
            });
        }

        public class RedeemCapeCodeModel
        {
            public string Code { get; set; } = string.Empty;
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RedeemCapeCode([FromBody] RedeemCapeCodeModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Code))
            {
                return BadRequest(new { success = false, message = "Введите промокод на плащ." });
            }

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                var sessionKey = Request.Headers["X-Session-Key"].FirstOrDefault();
                if (!string.IsNullOrEmpty(sessionKey))
                {
                    var session = await _sessionRepository.GetSessionByToken(sessionKey);
                    if (session != null)
                    {
                        user = session.User ?? await _userManager.FindByIdAsync(session.UserId);
                    }
                }
            }
            if (user is null) return Unauthorized();

            var cleanCode = model.Code.Trim().ToUpperInvariant();

            CapeRedeemCode? codeEntry = await _capesRepository.GetRedeemCodeAsync(cleanCode);

            if (codeEntry == null || codeEntry.Cape == null)
            {
                return BadRequest(new { success = false, message = "Промокод не найден или введен неверно." });
            }

            if (codeEntry.IsUsed)
            {
                return BadRequest(new { success = false, message = "Этот промокод уже был активирован ранее." });
            }

            if (codeEntry.ExpiresAt.HasValue && codeEntry.ExpiresAt.Value < DateTime.UtcNow)
            {
                return BadRequest(new { success = false, message = "Срок действия этого промокода истек." });
            }

            bool alreadyOwned = await _capesRepository.IsCapeOwnedByUserAsync(user.Id, codeEntry.CapeId);

            if (alreadyOwned)
            {
                return BadRequest(new { success = false, message = $"У вас уже есть плащ «{codeEntry.Cape.Name}» в гардеробе!" });
            }

            codeEntry.IsUsed = true;
            codeEntry.RedeemedByUserId = user.Id;
            codeEntry.RedeemedAt = DateTime.UtcNow;

            UserCapeOwnership ownership = new UserCapeOwnership
            {
                UserId = user.Id,
                CapeId = codeEntry.CapeId,
                AcquiredAt = DateTime.UtcNow,
                AcquiredVia = "RedeemCode"
            };
            await _capesRepository.AddOwnershipAsync(ownership);
            await _capesRepository.UpdateRedeemCodeAsync(codeEntry);

            return Ok(new
            {
                success = true,
                message = $"Поздравляем! Плащ «{codeEntry.Cape.Name}» успешно разблокирован и добавлен в ваш гардероб!",
                cape = new
                {
                    id = $"custom_{codeEntry.Cape.Id}",
                    name = codeEntry.Cape.Name,
                    description = codeEntry.Cape.Description ?? "Эксклюзивный плащ VEC",
                    previewUrl = $"/textures/{codeEntry.Cape.TextureHash}"
                }
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetWardrobeState()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                var sessionKey = Request.Headers["X-Session-Key"].FirstOrDefault()
                                 ?? Request.Query["sessionKey"].FirstOrDefault();
                if (!string.IsNullOrEmpty(sessionKey))
                {
                    var session = await _sessionRepository.GetSessionByToken(sessionKey);
                    if (session != null)
                    {
                        user = session.User ?? await _userManager.FindByIdAsync(session.UserId);
                    }
                }
            }
            if (user == null)
            {
                var username = Request.Headers["X-Username"].FirstOrDefault()
                               ?? Request.Query["username"].FirstOrDefault()
                               ?? Request.Headers["X-User"].FirstOrDefault();
                if (!string.IsNullOrEmpty(username))
                {
                    user = await _userManager.FindByNameAsync(username);
                }
            }
            if (user is null)
            {
                return StatusCode(401, new { success = false, error = "Пользователь не найден или сессия недействительна." });
            }

            var model = _skinStorage.GetSkinModel(user.UserName);
            var equippedCapeId = _skinStorage.DetectEquippedCapeId(user.UserName);
            var capeUrl = _skinStorage.GetCapeUrl(user.UserName);
            var skinUrl = _skinStorage.GetSkinUrl(user.UserName);

            var mojangCapes = new List<MojangCapeInfo>();
            if (user.HasMinecraftLicense && !string.IsNullOrWhiteSpace(user.OwnedMojangCapesJson))
            {
                try
                {
                    mojangCapes = JsonSerializer.Deserialize<List<MojangCapeInfo>>(user.OwnedMojangCapesJson) ?? new();
                }
                catch { }
            }

            var availableCapes = new List<object>();
            try
            {
                List<CustomCape> publicCapes = await _capesRepository.GetPublicCapesAsync();
                List<UserCapeOwnership> userOwnerships = await _capesRepository.GetUserCapeOwnershipsAsync(user.Id);
                List<CustomCape> ownedCapes = userOwnerships
                    .Where(o => o.Cape != null)
                    .Select(o => o.Cape!)
                    .ToList();
                var customCapes = publicCapes.Concat(ownedCapes).DistinctBy(c => c.Id).ToList();
                var vecCape = customCapes.FirstOrDefault(c => c.Slug == "vec");
                var isVecEquipped = equippedCapeId == "vec_default" || (vecCape != null && equippedCapeId == $"custom_{vecCape.Id}");

                availableCapes.Add(new
                {
                    id = "vec_default",
                    name = vecCape?.Name ?? "Плащ VEC",
                    description = vecCape?.Description ?? "Официальный плащ проекта VEC.",
                    previewUrl = vecCape != null ? $"/textures/{vecCape.TextureHash}" : "/img/capes/cape_vec.png",
                    isEquipped = isVecEquipped
                });

                foreach (var c in customCapes.Where(c => c.Slug != "vec"))
                {
                    var capeId = $"custom_{c.Id}";
                    availableCapes.Add(new
                    {
                        id = capeId,
                        name = c.Name,
                        description = c.Description ?? "Эксклюзивный плащ VEC",
                        previewUrl = $"/textures/{c.TextureHash}",
                        isEquipped = equippedCapeId == capeId
                    });
                }

                if (user.HasMinecraftLicense)
                {
                    if (mojangCapes.Count == 0)
                    {
                        var defaultMojangCapes = new List<MojangCapeInfo>
                        {
                            new() { Id = "0a22d0fe-c126-45e3-959d-0908d5fa5830", Alias = "Migrator", DisplayName = "Плащ переселенца (Migrator)" },
                            new() { Id = "2ed5269a-076e-4a3c-834a-2837d5b578f2", Alias = "Vanilla", DisplayName = "Плащ Vanilla" },
                            new() { Id = "64eacd79-e44b-4af4-a894-4f56f5844e2f", Alias = "Cherry", DisplayName = "Плащ сакуры (Cherry)" },
                            new() { Id = "6e600ae7-14ee-438b-addc-713d143d29c4", Alias = "15th Anniversary", DisplayName = "Юбилейный плащ 15 лет" },
                            new() { Id = "b5e2a8a3-b36f-4869-ac4d-f1deac717bbc", Alias = "Follower / TikTok", DisplayName = "Плащ TikTok" },
                            new() { Id = "b9d4f2e0-6109-43f7-97aa-84250ce3c1dd", Alias = "Purple Heart / Twitch", DisplayName = "Фиолетовое сердце Twitch" }
                        };
                        mojangCapes.AddRange(defaultMojangCapes);
                    }

                    foreach (var mc in mojangCapes)
                    {
                        var capeId = $"mojang_{mc.Id}";
                        var mCapeUrl = !string.IsNullOrWhiteSpace(mc.Url) ? mc.Url : $"/img/capes/mojang/{mc.Id}.png";
                        availableCapes.Add(new
                        {
                            id = capeId,
                            name = !string.IsNullOrWhiteSpace(mc.DisplayName) ? mc.DisplayName : (!string.IsNullOrWhiteSpace(mc.Alias) ? mc.Alias : "Плащ Mojang"),
                            description = "Официальный лицензионный плащ Minecraft (Mojang / Microsoft)",
                            previewUrl = mCapeUrl,
                            isEquipped = equippedCapeId == capeId
                        });
                    }
                }
            }
            catch { }

            return Ok(new
            {
                success = true,
                username = user.UserName,
                skinUrl,
                capeUrl,
                skinModel = model,
                equippedCapeId,
                hasMinecraftLicense = user.HasMinecraftLicense,
                microsoftGamertag = user.MicrosoftGamertag,
                microsoftUuid = user.MicrosoftUuid,
                microsoftLinkedAt = user.MicrosoftLicenseLinkedAt?.ToString("o"),
                mojangCapes,
                availableCapes
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> StartMicrosoftDeviceCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            try
            {
                var initResult = await _licenseService.RequestDeviceCodeAsync();
                if (initResult == null || string.IsNullOrEmpty(initResult.DeviceCode))
                {
                    return BadRequest(new { error = "Не удалось начать авторизацию через Microsoft. Попробуйте позже." });
                }

                return Ok(new
                {
                    success = true,
                    deviceCode = initResult.DeviceCode,
                    userCode = initResult.UserCode,
                    verificationUri = initResult.VerificationUri,
                    expiresIn = initResult.ExpiresIn,
                    interval = initResult.Interval,
                    message = initResult.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Ошибка связи с Microsoft: " + ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PollMicrosoftDeviceCode([FromForm] string deviceCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(deviceCode))
                return BadRequest(new { error = "deviceCode не передан." });

            try
            {
                var licResult = await _licenseService.CheckDeviceCodeAsync(deviceCode);
                if (licResult == null)
                {
                    return Ok(new { status = "pending" });
                }

                if (!licResult.Success || !licResult.HasMinecraftLicense)
                {
                    return BadRequest(new
                    {
                        status = "no_license",
                        error = licResult.ErrorMessage ?? "На этой учетной записи Microsoft не найдена купленная лицензия Minecraft: Java Edition."
                    });
                }

                user.HasMinecraftLicense = true;
                user.MicrosoftGamertag = licResult.Gamertag;
                user.MicrosoftUuid = licResult.Uuid;
                user.MicrosoftLicenseLinkedAt = DateTime.UtcNow;
                user.OwnedMojangCapesJson = JsonSerializer.Serialize(licResult.Capes);

                var updateRes = await _userManager.UpdateAsync(user);
                if (!updateRes.Succeeded)
                {
                    return StatusCode(500, new { error = "Не удалось сохранить привязку в базе данных." });
                }

                if (!string.IsNullOrWhiteSpace(licResult.Gamertag))
                {
                    try
                    {
                        var prof = _skinStorage.GetUserProfileData(user.UserName);
                        _skinStorage.SaveUserProfileData(user.UserName, prof.siteName, licResult.Gamertag, prof.createdAt);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PROFILE-SYNC] Error updating Minecraft username to gamertag: {ex.Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(licResult.SkinUrl))
                {
                    try
                    {
                        var skinBytes = await _licenseService.DownloadSkinBytesAsync(licResult.SkinUrl);
                        if (skinBytes != null && skinBytes.Length > 0)
                        {
                            await _skinStorage.SaveSkinBytesAsync(user.UserName, skinBytes, licResult.SkinModel ?? "classic");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SKIN-SYNC] Error importing skin during link: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    status = "success",
                    gamertag = licResult.Gamertag,
                    uuid = licResult.Uuid,
                    capes = licResult.Capes,
                    skinUrl = licResult.SkinUrl,
                    minecraftUsername = licResult.Gamertag,
                    message = $"Лицензия Minecraft ({licResult.Gamertag}) успешно привязана! Найдено официальных плащей: {licResult.Capes.Count}."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SyncMojangSkin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!user.HasMinecraftLicense || string.IsNullOrWhiteSpace(user.MicrosoftUuid))
            {
                return BadRequest(new { error = "У вас не привязана официальная лицензия Microsoft / Xbox." });
            }

            var (skinBytes, model, skinUrl) = await _licenseService.FetchSkinByMojangUuidAsync(user.MicrosoftUuid);
            if (skinBytes == null || skinBytes.Length == 0)
            {
                return BadRequest(new { error = "Не удалось загрузить официальный скин с серверов Mojang." });
            }

            var ok = await _skinStorage.SaveSkinBytesAsync(user.UserName, skinBytes, model);
            if (!ok)
            {
                return BadRequest(new { error = "Не удалось сохранить скин в профиле." });
            }

            if (!string.IsNullOrWhiteSpace(user.MicrosoftGamertag))
            {
                try
                {
                    var prof = _skinStorage.GetUserProfileData(user.UserName);
                    _skinStorage.SaveUserProfileData(user.UserName, prof.siteName, user.MicrosoftGamertag, prof.createdAt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PROFILE-SYNC] Error updating Minecraft username to gamertag: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = true,
                message = "Официальный скин, модель Mojang и игровой ник успешно синхронизированы!",
                skinUrl = _skinStorage.GetSkinUrl(user.UserName),
                model = model,
                minecraftUsername = user.MicrosoftGamertag
            });
        }

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UnlinkMicrosoftLicense()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            user.HasMinecraftLicense = false;
            user.MicrosoftGamertag = null;
            user.MicrosoftUuid = null;
            user.MicrosoftLicenseLinkedAt = null;
            user.OwnedMojangCapesJson = null;

            var equipped = _skinStorage.DetectEquippedCapeId(user.UserName);
            if (equipped != null && equipped.StartsWith("mojang_", StringComparison.OrdinalIgnoreCase))
            {
                _skinStorage.RemoveCape(user.UserName);
            }

            await _userManager.UpdateAsync(user);

            return Ok(new { success = true, message = "Привязка лицензии Microsoft успешно удалена." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadCustomCape(IFormFile capeFile)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (capeFile == null || capeFile.Length == 0)
                return BadRequest(new { error = "Файл плаща не выбран." });

            if (!capeFile.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Плащ должен быть в формате .png." });

            var ok = await _skinStorage.SaveCustomCapeAsync(user.UserName, capeFile);
            if (!ok) return BadRequest(new { error = "Не удалось сохранить плащ." });

            return Ok(new
            {
                success = true,
                message = "Пользовательский плащ успешно надет!",
                capeUrl = _skinStorage.GetCapeUrl(user.UserName),
                equippedCapeId = "custom"
            });
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] = "Пожалуйста, проверьте правильность введенных паролей.";
                return RedirectToAction(nameof(AccountSettings));
            }

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);

            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["PasswordSuccess"] = "Пароль успешно изменен!";
                return RedirectToAction(nameof(AccountSettings));
            }
            
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            TempData["PasswordError"] = errors;
            return RedirectToAction(nameof(AccountSettings));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RequestEmailChangeCode(string newEmail)
        {
            if (string.IsNullOrWhiteSpace(newEmail) || !newEmail.Contains('@'))
            {
                return Json(new { success = false, error = "Введите корректный адрес электронной почты." });
            }

            var cleanEmail = newEmail.Trim().ToLowerInvariant();
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, error = "Пользователь не авторизован." });
            }

            if (string.Equals(user.Email, cleanEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, error = "Новый адрес совпадает с текущим." });
            }

            var existing = await _userManager.FindByEmailAsync(cleanEmail);
            if (existing != null && existing.Id != user.Id)
            {
                return Json(new { success = false, error = "Данный адрес электронной почты уже используется другим аккаунтом." });
            }

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            _emailChangeCodes[user.Id] = (code, cleanEmail, DateTime.UtcNow.AddMinutes(15));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(EmailServiceConfig.SenderName, EmailServiceConfig.Email));
            message.To.Add(new MailboxAddress("", cleanEmail));
            message.Subject = "Код подтверждения смены email — VEC";
            message.Body = new TextPart("plain")
            {
                Text = $"Здравствуйте, {user.UserName}!\n\nВаш код для подтверждения смены почты на сервере VEC:\n\n{code}\n\nКод действителен 15 минут. Если вы не запрашивали смену почты, просто проигнорируйте это письмо."
            };

            var sent = await _emailService.SendEmail(message);
            if (!sent)
            {
                Console.WriteLine($"[EmailService] Код смены почты для {user.UserName} ({cleanEmail}): {code}");
            }

            return Json(new { success = true, message = $"Код подтверждения отправлен на {cleanEmail}." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ConfirmEmailChange(string newEmail, string code)
        {
            if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(code))
            {
                TempData["EmailError"] = "Пожалуйста, заполните новый email и код подтверждения.";
                return RedirectToAction(nameof(AccountSettings));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var cleanEmail = newEmail.Trim().ToLowerInvariant();
            var cleanCode = code.Trim();

            if (!_emailChangeCodes.TryGetValue(user.Id, out var entry))
            {
                TempData["EmailError"] = "Код подтверждения не запрашивался или срок его действия истек. Запросите код снова.";
                return RedirectToAction(nameof(AccountSettings));
            }

            if (entry.ExpiresAt < DateTime.UtcNow)
            {
                _emailChangeCodes.TryRemove(user.Id, out _);
                TempData["EmailError"] = "Срок действия кода истек. Запросите новый код.";
                return RedirectToAction(nameof(AccountSettings));
            }

            if (!string.Equals(entry.NewEmail, cleanEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["EmailError"] = "Указанный email не совпадает с адресом, для которого запрашивался код.";
                return RedirectToAction(nameof(AccountSettings));
            }

            if (!string.Equals(entry.Code, cleanCode, StringComparison.OrdinalIgnoreCase))
            {
                TempData["EmailError"] = "Неверный код подтверждения.";
                return RedirectToAction(nameof(AccountSettings));
            }

            _emailChangeCodes.TryRemove(user.Id, out _);

            user.Email = cleanEmail;
            user.NormalizedEmail = _userManager.NormalizeEmail(cleanEmail);
            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["EmailSuccess"] = "Электронная почта успешно обновлена!";
            }
            else
            {
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

                await _emailService.SendEmail(await _emailService.CreateConfirmMessage(user, url));

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
            await _emailService.SendEmail(await _emailService.CreatePasswordResetMessage(user, url));
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

        [AllowAnonymous]
        public IActionResult EmailSent()
        {
            return View();
        }

        #region Friends System & Public Profiles

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string query)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(query))
                return Json(new { success = true, results = new List<FriendSearchUserResult>() });

            var cleanQuery = query.Trim();

            var matchedUsers = _userManager.Users
                .Where(u => u.UserName != null && EF.Functions.Like(u.UserName, $"%{cleanQuery}%"))
                .Take(15)
                .ToList();

            var matchedIds = matchedUsers.Select(u => u.Id).ToList();

            List<Friendship> friendships = await _friendshipsRepository.GetFriendshipsBetweenUserAndTargetsAsync(user.Id, matchedIds);

            var matched = new List<FriendSearchUserResult>();

            foreach (var u in matchedUsers)
            {
                var (siteName, mcUser, _) = _skinStorage.GetUserProfileData(u.UserName);
                string status = "None";
                int? fId = null;

                if (u.Id == user.Id)
                {
                    status = "Self";
                }
                else
                {
                    var f = friendships.FirstOrDefault(x => x.RequesterId == u.Id || x.AddresseeId == u.Id);
                    if (f != null)
                    {
                        fId = f.Id;
                        if (f.Status == FriendshipStatus.Accepted) status = "Friends";
                        else if (f.Status == FriendshipStatus.Pending)
                        {
                            status = f.RequesterId == user.Id ? "PendingOutgoing" : "PendingIncoming";
                        }
                    }
                }

                matched.Add(new FriendSearchUserResult
                {
                    UserId = u.Id,
                    Username = string.IsNullOrEmpty(siteName) ? (u.UserName ?? "") : siteName,
                    MinecraftUsername = string.IsNullOrEmpty(mcUser) ? (u.UserName ?? "") : mcUser,
                    SkinUrl = _skinStorage.GetSkinUrl(u.UserName ?? ""),
                    FriendshipStatus = status,
                    FriendshipId = fId
                });
            }

            return Json(new { success = true, results = matched });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string targetUserId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (string.IsNullOrEmpty(targetUserId) || targetUserId == user.Id)
                return BadRequest(new { error = "Недопустимый пользователь." });

            var target = await _userManager.FindByIdAsync(targetUserId);
            if (target is null) return NotFound(new { error = "Пользователь не найден." });

            Friendship? existing = await _friendshipsRepository.GetFriendshipBetweenAsync(user.Id, targetUserId);

            if (existing != null)
            {
                if (existing.Status == FriendshipStatus.Accepted)
                    return BadRequest(new { error = "Вы уже в друзьях." });

                if (existing.Status == FriendshipStatus.Pending)
                {
                    if (existing.RequesterId == targetUserId)
                    {
                        existing.Status = FriendshipStatus.Accepted;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _friendshipsRepository.UpdateFriendshipAsync(existing);
                        return Ok(new { success = true, status = "Friends", message = "Заявка принята! Теперь вы друзья." });
                    }
                    return BadRequest(new { error = "Заявка уже отправлена." });
                }

                existing.RequesterId = user.Id;
                existing.AddresseeId = targetUserId;
                existing.Status = FriendshipStatus.Pending;
                existing.UpdatedAt = DateTime.UtcNow;
                await _friendshipsRepository.UpdateFriendshipAsync(existing);
                return Ok(new { success = true, status = "PendingOutgoing", message = "Заявка в друзья отправлена!", friendshipId = existing.Id });
            }

            Friendship friendship = new Friendship
            {
                RequesterId = user.Id,
                AddresseeId = targetUserId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipsRepository.AddFriendshipAsync(friendship);

            return Ok(new { success = true, status = "PendingOutgoing", message = "Заявка в друзья отправлена!", friendshipId = friendship.Id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            Friendship? f = await _friendshipsRepository.GetFriendshipByIdAsync(friendshipId);
            if (f is null || f.AddresseeId != user.Id) return NotFound(new { error = "Заявка не найдена." });

            f.Status = FriendshipStatus.Accepted;
            f.UpdatedAt = DateTime.UtcNow;
            await _friendshipsRepository.UpdateFriendshipAsync(f);

            return Ok(new { success = true, message = "Заявка в друзья принята!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RejectFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            Friendship? f = await _friendshipsRepository.GetFriendshipByIdAsync(friendshipId);
            if (f is null || f.AddresseeId != user.Id) return NotFound(new { error = "Заявка не найдена." });

            await _friendshipsRepository.RemoveFriendshipAsync(f);

            return Ok(new { success = true, message = "Заявка отклонена." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            Friendship? f = await _friendshipsRepository.GetFriendshipByIdAsync(friendshipId);
            if (f is null || (f.RequesterId != user.Id && f.AddresseeId != user.Id)) return NotFound(new { error = "Друг не найден." });

            await _friendshipsRepository.RemoveFriendshipAsync(f);

            return Ok(new { success = true, message = "Пользователь удален из друзей." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            Friendship? f = await _friendshipsRepository.GetFriendshipByIdAsync(friendshipId);
            if (f is null || f.RequesterId != user.Id) return NotFound(new { error = "Заявка не найдена." });

            await _friendshipsRepository.RemoveFriendshipAsync(f);

            return Ok(new { success = true, message = "Заявка отменена." });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            List<Friendship> incoming = await _friendshipsRepository.GetIncomingPendingRequestsAsync(user.Id);

            var list = new List<object>();
            foreach (var f in incoming)
            {
                var reqUser = await _userManager.FindByIdAsync(f.RequesterId);
                var rName = reqUser?.UserName ?? "Игрок";
                var (siteName, mcUser, _) = _skinStorage.GetUserProfileData(rName);

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

            int friendshipsCount = await _friendshipsRepository.GetAcceptedCountAsync(targetUser.Id);

            string status = "None";
            int? fId = null;

            if (currentUser != null)
            {
                Friendship? relation = await _friendshipsRepository.GetFriendshipBetweenAsync(currentUser.Id, targetUser.Id);

                if (relation != null)
                {
                    fId = relation.Id;
                    if (relation.Status == FriendshipStatus.Accepted) status = "Friends";
                    else if (relation.Status == FriendshipStatus.Pending)
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

        #endregion
    }
}
