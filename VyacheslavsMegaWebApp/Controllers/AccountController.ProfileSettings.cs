using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Controllers
{
    public partial class AccountController
    {
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

            var accepted = _friendshipsRepository.GetAcceptedFriendshipsCount(user.Id);
            var incoming = _friendshipsRepository.GetIncomingPendingRequests(user.Id);
            var outgoing = _friendshipsRepository.GetOutgoingRequestsAsync(user.Id);

            /*
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
            */
            var model = new ProfileViewModel
            {
                Username = user.UserName,
                SiteName = siteName,
                MinecraftUsername = mcUser,
                CreatedAt = createdAt,
                //FriendsCount = friendsList.Count,
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
                //Friends = friendsList,
                //IncomingRequests = incomingList,
                //OutgoingRequests = outgoingList
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
    }
}