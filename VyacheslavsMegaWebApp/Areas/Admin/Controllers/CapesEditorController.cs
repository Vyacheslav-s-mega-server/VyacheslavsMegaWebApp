using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CapesEditorController : Controller
    {
        private readonly CapesRepository _capesRepo;
        private readonly SkinStorageService _skinStorage;
        private readonly IWebHostEnvironment _env;

        public CapesEditorController(CapesRepository capesRepo, SkinStorageService skinStorage, IWebHostEnvironment env)
        {
            _capesRepo = capesRepo;
            _skinStorage = skinStorage;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            _capesRepo.SeedDefaultCapesIfNeeded(_env);
            List<CustomCape> capes = await _capesRepo.GetAllCapesWithDetailsAsync();
            return View(capes);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string name, string? slug, string? description, bool isPublic, IFormFile? capeImage)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Укажите название плаща.";
                return View();
            }

            if (capeImage == null || capeImage.Length == 0)
            {
                TempData["Error"] = "Загрузите файл текстуры плаща (.png).";
                return View();
            }

            if (!capeImage.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Файл плаща должен быть в формате .png.";
                return View();
            }

            string cleanSlug;
            if (string.IsNullOrWhiteSpace(slug))
            {
                cleanSlug = "cape_" + Guid.NewGuid().ToString("N")[..8];
            }
            else
            {
                cleanSlug = new string(slug.Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-').ToArray());
                if (string.IsNullOrEmpty(cleanSlug))
                {
                    cleanSlug = "cape_" + Guid.NewGuid().ToString("N")[..8];
                }
            }

            if (await _capesRepo.AnyCapeWithSlugAsync(cleanSlug))
            {
                TempData["Error"] = $"Плащ с идентификатором «{cleanSlug}» уже существует.";
                return View();
            }

            byte[] bytes;
            using (MemoryStream ms = new MemoryStream())
            {
                await capeImage.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            string hash = _skinStorage.SaveTextureBytes(bytes);

            CustomCape cape = new CustomCape
            {
                Name = name.Trim(),
                Slug = cleanSlug,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                TextureHash = hash,
                IsPublic = isPublic,
                CreatedAt = DateTime.UtcNow
            };

            await _capesRepo.AddCapeAsync(cape);

            TempData["Success"] = $"Плащ «{cape.Name}» успешно добавлен.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            CustomCape? cape = await _capesRepo.GetCapeByIdAsync(id);
            if (cape == null)
            {
                TempData["Error"] = "Плащ не найден.";
                return RedirectToAction(nameof(Index));
            }

            return View(cape);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string name, string? slug, string? description, bool isPublic, IFormFile? newImage)
        {
            CustomCape? cape = await _capesRepo.GetCapeByIdAsync(id);
            if (cape == null)
            {
                TempData["Error"] = "Плащ не найден.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Название плаща не может быть пустым.";
                return View(cape);
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                string cleanSlug = new string(slug.Trim().ToLowerInvariant().Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-').ToArray());
                if (!string.IsNullOrEmpty(cleanSlug) && cleanSlug != cape.Slug)
                {
                    if (await _capesRepo.AnyCapeWithSlugExceptIdAsync(cleanSlug, id))
                    {
                        TempData["Error"] = $"Идентификатор «{cleanSlug}» уже используется другим плащом.";
                        return View(cape);
                    }
                    cape.Slug = cleanSlug;
                }
            }

            cape.Name = name.Trim();
            cape.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            cape.IsPublic = isPublic;

            if (newImage != null && newImage.Length > 0)
            {
                if (!newImage.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Файл должен быть в формате .png.";
                    return View(cape);
                }

                byte[] bytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    await newImage.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                string hash = _skinStorage.SaveTextureBytes(bytes);
                cape.TextureHash = hash;
            }

            await _capesRepo.UpdateCapeAsync(cape);

            TempData["Success"] = $"Плащ «{cape.Name}» успешно обновлен.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Codes(int id)
        {
            CustomCape? cape = await _capesRepo.GetCapeWithCodesByIdAsync(id);

            if (cape == null)
            {
                TempData["Error"] = "Плащ не найден.";
                return RedirectToAction(nameof(Index));
            }

            List<string> userIds = cape.Codes.Where(c => c.IsUsed && !string.IsNullOrEmpty(c.RedeemedByUserId))
                .Select(c => c.RedeemedByUserId!)
                .Distinct()
                .ToList();

            Dictionary<string, string?> usersMap = await _capesRepo.GetUsersMapAsync(userIds);

            ViewBag.UsersMap = usersMap;

            return View(cape);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCodes(int capeId, int count, string? prefix)
        {
            CustomCape? cape = await _capesRepo.GetCapeByIdAsync(capeId);
            if (cape == null)
            {
                TempData["Error"] = "Плащ не найден.";
                return RedirectToAction(nameof(Index));
            }

            count = Math.Clamp(count, 1, 500);
            string cleanPrefix = string.IsNullOrWhiteSpace(prefix) ? "VMS" : prefix.Trim().ToUpperInvariant();

            const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            List<CapeRedeemCode> codes = new List<CapeRedeemCode>();
            HashSet<string> existingCodes = await _capesRepo.GetExistingCodesHashSetAsync();

            for (int i = 0; i < count; i++)
            {
                string code;
                int attempts = 0;
                do
                {
                    code = $"{cleanPrefix}-{RandomString(chars, 4)}-{RandomString(chars, 4)}-{RandomString(chars, 4)}";
                    attempts++;
                } while (existingCodes.Contains(code) && attempts < 20);

                existingCodes.Add(code);
                codes.Add(new CapeRedeemCode
                {
                    CapeId = capeId,
                    Code = code,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _capesRepo.AddCodesRangeAsync(codes);

            TempData["Success"] = $"Сгенерировано промокодов: {codes.Count}.";
            return RedirectToAction(nameof(Codes), new { id = capeId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCode(int id, int capeId)
        {
            bool deleted = await _capesRepo.DeleteCodeAsync(id, capeId);
            if (deleted)
            {
                TempData["Success"] = "Промокод удален.";
            }

            return RedirectToAction(nameof(Codes), new { id = capeId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCape(int id)
        {
            CustomCape? cape = await _capesRepo.GetCapeByIdAsync(id);
            if (cape == null)
            {
                TempData["Error"] = "Плащ не найден.";
                return RedirectToAction(nameof(Index));
            }

            if (cape.Slug == "vec")
            {
                TempData["Error"] = "Базовый плащ VEC не может быть удален.";
                return RedirectToAction(nameof(Index));
            }

            await _capesRepo.DeleteCapeAsync(id);

            TempData["Success"] = $"Плащ «{cape.Name}» удален.";
            return RedirectToAction(nameof(Index));
        }

        private static string RandomString(string chars, int length)
        {
            char[] res = new char[length];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[length];
                rng.GetBytes(bytes);
                for (int i = 0; i < length; i++)
                {
                    res[i] = chars[bytes[i] % chars.Length];
                }
            }
            return new string(res);
        }
    }
}
