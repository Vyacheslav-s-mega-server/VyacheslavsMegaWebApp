using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminAreaPolicy")]
    public class ClientBuildsEditorController : Controller
    {
        private readonly ClientBuildFilesRepository _repo;
        private readonly IWebHostEnvironment _env;
        private readonly ModrinthService _modrinth;

        public ClientBuildsEditorController(ClientBuildFilesRepository repo, IWebHostEnvironment env, ModrinthService modrinth)
        {
            _repo = repo;
            _env = env;
            _modrinth = modrinth;
        }

        private string GetBuildsDir(string slug, string folder = "mods")
        {
            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string dir = Path.Combine(root, "builds", slug, folder);
            Directory.CreateDirectory(dir);
            return dir;
        }

        public async Task<IActionResult> Index(string slug = "vec-fabric-1-21-11")
        {
            ViewBag.Slug = slug;
            List<ClientBuildFile> files = await _repo.GetFilesByBuildSlugOrderedAsync(slug);
            return View(files);
        }

        [HttpGet]
        public async Task<IActionResult> LookupModrinth(string query, string? category)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Укажите ссылку или ID мода на Modrinth" });
            }

            ModrinthProjectInfo? info = await _modrinth.FetchModrinthInfoAsync(query, category ?? "Core");
            if (info == null)
            {
                return NotFound(new { error = "Проект не найден на Modrinth или не содержит подходящих файлов для Fabric" });
            }

            return Ok(new
            {
                slug = info.Slug,
                title = info.Title,
                description = info.Description,
                iconUrl = info.IconUrl,
                category = info.Category,
                fileName = info.FileName,
                downloadUrl = info.DownloadUrl,
                fileSizeBytes = info.FileSizeBytes,
                sha256 = info.Sha256,
                relativePath = info.RelativePath
            });
        }

        public async Task<IActionResult> ModDetails(int? id, string slug = "vec-fabric-1-21-11")
        {
            ViewBag.Slug = slug;
            if (id == null)
            {
                return View(new ClientBuildFile
                {
                    BuildSlug = slug,
                    IsRequired = true,
                    Category = "Optimization",
                    SourceType = "Local"
                });
            }

            ClientBuildFile? item = await _repo.GetFileByIdAsync(id.Value);
            if (item == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(item.SourceType))
            {
                item.SourceType = item.DownloadUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true ? "Modrinth" : "Local";
            }

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> ModDetails(ClientBuildFile model, IFormFile? modFile, string slug = "vec-fabric-1-21-11")
        {
            ViewBag.Slug = slug;

            bool isModrinth = string.Equals(model.SourceType, "Modrinth", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(model.Category))
            {
                model.Category = "Core";
            }

            string subFolder = "mods";
            if (string.Equals(model.Category, "Shaders", StringComparison.OrdinalIgnoreCase))
            {
                subFolder = "shaderpacks";
            }
            else if (string.Equals(model.Category, "Resourcepacks", StringComparison.OrdinalIgnoreCase))
            {
                subFolder = "resourcepacks";
            }

            if (isModrinth)
            {
                if (string.IsNullOrWhiteSpace(model.ModrinthSlug) && string.IsNullOrWhiteSpace(model.DownloadUrl))
                {
                    ModelState.AddModelError("", "Для компонента из Modrinth необходимо указать ID или ссылку на проект.");
                    return View(model);
                }

                if (!string.IsNullOrWhiteSpace(model.ModrinthSlug) && (string.IsNullOrWhiteSpace(model.DownloadUrl) || model.Id == 0))
                {
                    ModrinthProjectInfo? info = await _modrinth.FetchModrinthInfoAsync(model.ModrinthSlug, model.Category);
                    if (info == null && string.IsNullOrWhiteSpace(model.DownloadUrl))
                    {
                        ModelState.AddModelError("", "Не удалось получить данные с Modrinth API. Проверьте ID/ссылку.");
                        return View(model);
                    }

                    if (info != null)
                    {
                        model.DownloadUrl = info.DownloadUrl;
                        model.FileName = info.FileName;
                        model.RelativePath = info.RelativePath;
                        model.Sha256 = info.Sha256;
                        model.FileSizeBytes = info.FileSizeBytes;
                        if (string.IsNullOrEmpty(model.IconUrl)) model.IconUrl = info.IconUrl;
                        if (string.IsNullOrWhiteSpace(model.DisplayName)) model.DisplayName = info.Title;
                        if (string.IsNullOrWhiteSpace(model.Description)) model.Description = info.Description;
                    }
                }

                if (string.IsNullOrWhiteSpace(model.FileName))
                {
                    model.FileName = $"{ModrinthService.ExtractSlug(model.ModrinthSlug ?? "component") ?? "component"}.jar";
                }

                if (string.IsNullOrWhiteSpace(model.RelativePath))
                {
                    model.RelativePath = $"{subFolder}/{model.FileName}";
                }
            }
            else
            {
                model.SourceType = "Local";

                if (model.Id == 0 && (modFile == null || modFile.Length == 0))
                {
                    ModelState.AddModelError("", "Для добавления локального компонента необходимо выбрать файл (.jar или .zip).");
                    return View(model);
                }

                if (modFile != null && modFile.Length > 0)
                {
                    string ext = Path.GetExtension(modFile.FileName);
                    if (!string.Equals(ext, ".jar", StringComparison.OrdinalIgnoreCase) && !string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("", "Разрешена загрузка только файлов с расширением .jar или .zip.");
                        return View(model);
                    }

                    string cleanFileName = Path.GetFileName(modFile.FileName);
                    string targetDir = GetBuildsDir(slug, subFolder);
                    string targetPath = Path.Combine(targetDir, cleanFileName);

                    using (FileStream fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        await modFile.CopyToAsync(fileStream);
                    }

                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(targetPath);
                    string sha256;
                    using (SHA256 sha = SHA256.Create())
                    {
                        sha256 = Convert.ToHexString(sha.ComputeHash(fileBytes)).ToLowerInvariant();
                    }

                    model.FileName = cleanFileName;
                    model.RelativePath = $"{subFolder}/{cleanFileName}";
                    model.Sha256 = sha256;
                    model.FileSizeBytes = fileBytes.LongLength;
                    model.DownloadUrl = $"/api/builds/{slug}/file/{subFolder}/{cleanFileName}";
                }
            }

            model.BuildSlug = slug;
            if (string.IsNullOrWhiteSpace(model.DisplayName))
            {
                model.DisplayName = model.FileName ?? "Компонент сборки";
            }

            if (model.Id == 0)
            {
                await _repo.AddFileAsync(model);
            }
            else
            {
                ClientBuildFile? existing = await _repo.GetFileByIdAsync(model.Id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.DisplayName = model.DisplayName;
                existing.Description = model.Description ?? string.Empty;
                existing.Category = model.Category;
                existing.IsRequired = model.IsRequired;
                existing.SourceType = model.SourceType;
                existing.ModrinthSlug = model.ModrinthSlug;
                existing.IconUrl = model.IconUrl;

                if (!string.IsNullOrEmpty(model.FileName))
                {
                    existing.FileName = model.FileName;
                    existing.RelativePath = model.RelativePath;
                    existing.Sha256 = model.Sha256;
                    existing.FileSizeBytes = model.FileSizeBytes;
                    existing.DownloadUrl = model.DownloadUrl;
                }

                await _repo.SaveChangesAsync();
            }

            TempData["Success"] = $"Компонент «{model.DisplayName}» успешно сохранен.";
            return RedirectToAction(nameof(Index), new { slug });
        }

        public async Task<IActionResult> DeleteMod(int id, string slug = "vec-fabric-1-21-11")
        {
            ClientBuildFile? item = await _repo.GetFileByIdAsync(id);
            if (item != null)
            {
                if (item.DownloadUrl == null || !item.DownloadUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                    string rel = item.RelativePath.Replace('/', Path.DirectorySeparatorChar);
                    string targetPath = Path.Combine(root, "builds", item.BuildSlug, rel);
                    if (System.IO.File.Exists(targetPath))
                    {
                        try { System.IO.File.Delete(targetPath); } catch { }
                    }
                }

                await _repo.DeleteFileAsync(id);
                TempData["Success"] = $"Компонент «{item.DisplayName}» удален из сборки.";
            }

            return RedirectToAction(nameof(Index), new { slug });
        }

        public async Task<IActionResult> ToggleRequired(int id, string slug = "vec-fabric-1-21-11")
        {
            bool? newRequired = await _repo.ToggleRequiredAsync(id);
            if (newRequired != null)
            {
                TempData["Success"] = $"Статус обязательности для компонента изменен на: {(newRequired.Value ? "Обязательный" : "Опциональный")}.";
            }

            return RedirectToAction(nameof(Index), new { slug });
        }
    }
}
