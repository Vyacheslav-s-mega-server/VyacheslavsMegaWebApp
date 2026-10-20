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
        private readonly AppDbContext _db;

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
            AppDbContext db) 
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _localizer = localizer;
            _skinStorage = skinStorage;
            _sessionRepository = sessionRepository;
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LauncherAuth(int? port = 25588)
        {
            int targetPort = port ?? 25588;
            if (User.Identity?.IsAuthenticated == true)
            {
                VmsIdentityUser? user = await _userManager.GetUserAsync(User);
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
        public async Task<IActionResult> Profile()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Unauthorized(_localizer[NotAuthorized]);

            var roles = await _userManager.GetRolesAsync(user);
            string roleBadge = roles.Contains("Admin") ? "Администратор" : (roles.Contains("Moderator") ? "Модератор" : "Игрок VMS");

            string? equippedCapeId = _skinStorage.DetectEquippedCapeId(user.UserName);

            List<CapeItemViewModel> availableCapes = new List<CapeItemViewModel>
            {
                new CapeItemViewModel
                {
                    Id = "vec_default",
                    Name = "Плащ VEC",
                    Description = "Официальный плащ проекта VEC.",
                    PreviewUrl = "/img/capes/cape_vec.png",
                    DownloadUrl = "/img/capes/cape_vec.png",
                    IsOfficial = true,
                    IsEquipped = equippedCapeId == "vec_default"
                }
            };

            

            var (siteName, mcUser, createdAt) = _skinStorage.GetUserProfileData(user.UserName);

            var friendships = _db.Friendships
                .Where(f => f.RequesterId == user.Id || f.AddresseeId == user.Id)
                .ToList();

            var accepted = friendships.Where(f => f.Status == FriendshipStatus.Accepted).ToList();
            var incoming = friendships.Where(f => f.AddresseeId == user.Id && f.Status == FriendshipStatus.Pending).ToList();
            var outgoing = friendships.Where(f => f.RequesterId == user.Id && f.Status == FriendshipStatus.Pending).ToList();

            var userIdsToFetch = friendships
                .Select(f => f.RequesterId == user.Id ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();

            var usersMap = _db.Users
                .Where(u => userIdsToFetch.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.UserName);

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
                Friends = friendsList,
                IncomingRequests = incomingList,
                OutgoingRequests = outgoingList
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

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EquipCape([FromForm] string? capeId)
        {
            if (string.IsNullOrEmpty(capeId))
            {
                capeId = Request.Form["capeId"].FirstOrDefault() ?? Request.Query["capeId"].FirstOrDefault();
            }

            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(capeId))
                return BadRequest(new { error = "Плащ не указан." });

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

        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RemoveCape()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            _skinStorage.RemoveCape(user.UserName);

            return Ok(new
            {
                success = true,
                message = "Плащ снят.",
                capeUrl = (string?)null,
                equippedCapeId = (string?)null
            });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetWardrobeState()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var model = _skinStorage.GetSkinModel(user.UserName);
            var equippedCapeId = _skinStorage.DetectEquippedCapeId(user.UserName);
            var capeUrl = _skinStorage.GetCapeUrl(user.UserName);
            var skinUrl = _skinStorage.GetSkinUrl(user.UserName);

            return Ok(new
            {
                success = true,
                username = user.UserName,
                skinUrl,
                capeUrl,
                skinModel = model,
                equippedCapeId
            });
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

            var friendships = _db.Friendships
                .Where(f => (f.RequesterId == user.Id && matchedIds.Contains(f.AddresseeId)) ||
                            (f.AddresseeId == user.Id && matchedIds.Contains(f.RequesterId)))
                .ToList();

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

            var existing = _db.Friendships
                .FirstOrDefault(f => (f.RequesterId == user.Id && f.AddresseeId == targetUserId) ||
                                     (f.RequesterId == targetUserId && f.AddresseeId == user.Id));

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
                        await _db.SaveChangesAsync();
                        return Ok(new { success = true, status = "Friends", message = "Заявка принята! Теперь вы друзья." });
                    }
                    return BadRequest(new { error = "Заявка уже отправлена." });
                }

                existing.RequesterId = user.Id;
                existing.AddresseeId = targetUserId;
                existing.Status = FriendshipStatus.Pending;
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { success = true, status = "PendingOutgoing", message = "Заявка в друзья отправлена!", friendshipId = existing.Id });
            }

            var friendship = new Friendship
            {
                RequesterId = user.Id,
                AddresseeId = targetUserId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Friendships.Add(friendship);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, status = "PendingOutgoing", message = "Заявка в друзья отправлена!", friendshipId = friendship.Id });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && x.AddresseeId == user.Id);
            if (f is null) return NotFound(new { error = "Заявка не найдена." });

            f.Status = FriendshipStatus.Accepted;
            f.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Заявка в друзья принята!" });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RejectFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && x.AddresseeId == user.Id);
            if (f is null) return NotFound(new { error = "Заявка не найдена." });

            _db.Friendships.Remove(f);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Заявка отклонена." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFriend(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && (x.RequesterId == user.Id || x.AddresseeId == user.Id));
            if (f is null) return NotFound(new { error = "Друг не найден." });

            _db.Friendships.Remove(f);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Пользователь удален из друзей." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelFriendRequest(int friendshipId)
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var f = _db.Friendships.FirstOrDefault(x => x.Id == friendshipId && x.RequesterId == user.Id);
            if (f is null) return NotFound(new { error = "Заявка не найдена." });

            _db.Friendships.Remove(f);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Заявка отменена." });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            VmsIdentityUser? user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var incoming = _db.Friendships
                .Where(f => f.AddresseeId == user.Id && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

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

            var friendshipsCount = _db.Friendships
                .Where(f => (f.RequesterId == targetUser.Id || f.AddresseeId == targetUser.Id) && f.Status == FriendshipStatus.Accepted)
                .Count();

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
