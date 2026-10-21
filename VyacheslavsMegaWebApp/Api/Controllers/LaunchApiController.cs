using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Api.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Api.Controllers
{
    [ApiController]
    public class LaunchApiController : ControllerBase
    {
        private readonly LaunchTicketService _ticketService;
        private readonly ClientBuildFilesRepository _clientBuildRepo;
        private readonly LauncherSessionRepository _sessionRepo;
        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public LaunchApiController(
            LaunchTicketService ticketService,
            ClientBuildFilesRepository clientBuildRepo,
            LauncherSessionRepository sessionRepo,
            UserManager<VmsIdentityUser> userManager,
            IWebHostEnvironment env)
        {
            _ticketService = ticketService;
            _clientBuildRepo = clientBuildRepo;
            _sessionRepo = sessionRepo;
            _userManager = userManager;
            _env = env;
        }

        [HttpPost("api/launch/request-ticket")]
        public async Task<IActionResult> RequestTicket([FromBody] RequestTicketModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username))
                return BadRequest(new { error = "Username required." });

            string cleanUser = Path.GetFileNameWithoutExtension(model.Username).Trim();
            VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanUser);
            if (user == null)
                return NotFound(new { error = "User not found." });

            if (!string.IsNullOrWhiteSpace(model.SessionKey))
            {
                LauncherSession? session = await _sessionRepo.FindValidSessionAsync(model.SessionKey, user.Id);
                if (session == null)
                    return Unauthorized(new { error = "Invalid or expired session." });
            }

            LaunchTicket ticket = _ticketService.CreateTicket(cleanUser, model.Hwid ?? string.Empty);
            return Ok(new
            {
                success = true,
                ticket = ticket.TicketId,
                username = ticket.Username,
                expiresAt = ticket.ExpiresAt
            });
        }

        [HttpPost("api/launch/verify-ticket")]
        public IActionResult VerifyTicket([FromBody] VerifyTicketModel model)
        {
            bool valid = _ticketService.ValidateAndConsumeTicket(model.Ticket, model.Username);
            if (!valid)
                return BadRequest(new { valid = false, error = "Invalid or expired ticket." });

            return Ok(new { valid = true, message = "Ticket verified." });
        }

        [HttpGet("api/launch/check-active/{username}")]
        public IActionResult CheckActiveTicket(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { active = false });

            bool active = _ticketService.HasActiveTicketForUser(username.Trim());
            return Ok(new { active, username = username.Trim() });
        }

        [HttpGet("api/builds/{slug}/manifest")]
        public async Task<IActionResult> GetBuildManifest(string slug)
        {
            string cleanSlug = string.IsNullOrWhiteSpace(slug) ? "vec-fabric-1-21-11" : slug.Trim().ToLowerInvariant();
            List<ManifestFileEntry> files = await GetManifestDataAsync(cleanSlug);
            return Ok(new
            {
                build = cleanSlug,
                version = "1.21.11",
                files = files
            });
        }

        [HttpGet("api/builds/{slug}/mods")]
        public async Task<IActionResult> GetBuildMods(string slug)
        {
            string cleanSlug = string.IsNullOrWhiteSpace(slug) ? "vec-fabric-1-21-11" : slug.Trim().ToLowerInvariant();
            List<ManifestFileEntry> files = await GetManifestDataAsync(cleanSlug);
            List<ManifestFileEntry> mods = files.Where(f => f.Path.StartsWith("mods/", StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(new { build = cleanSlug, files = files, mods = mods });
        }

        private async Task<List<ManifestFileEntry>> GetManifestDataAsync(string cleanSlug)
        {
            List<ClientBuildFile> dbFiles = await _clientBuildRepo.GetFilesByBuildSlugAsync(cleanSlug);

            if (dbFiles.Count == 0)
            {
                List<ClientBuildFile> defaults = new List<ClientBuildFile>
                {
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/fabric-api.jar", FileName = "fabric-api.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = true, Category = "Core", DisplayName = "Fabric API", Description = "Базовый модуль хуков и событий Fabric 1.21.11", SourceType = "Local" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/voicechat-fabric.jar", FileName = "voicechat-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = true, Category = "Core", DisplayName = "Simple Voice Chat", Description = "Позиционный голосовой 3D чат сервера VEC", SourceType = "Local" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/sodium-fabric.jar", FileName = "sodium-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = true, Category = "Optimization", DisplayName = "Sodium", Description = "Движок рендеринга и многократного повышения FPS", SourceType = "Local" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/iris-fabric.jar", FileName = "iris-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = false, Category = "Visual", DisplayName = "Iris Shaders", Description = "Современный движок ультра-шейдеров", SourceType = "Local" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/modmenu-fabric.jar", FileName = "modmenu-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = false, Category = "Utility", DisplayName = "Mod Menu", Description = "Удобный список и настройки модов в главном меню", SourceType = "Local" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/appleskin-fabric.jar", FileName = "appleskin-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = false, Category = "Utility", DisplayName = "AppleSkin", Description = "Показ насыщения еды и очков голода", SourceType = "Local" }
                };

                await _clientBuildRepo.AddRangeAsync(defaults);
                dbFiles = defaults;
            }

            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string buildDir = Path.Combine(root, "builds", cleanSlug);

            List<ManifestFileEntry> result = new List<ManifestFileEntry>();
            using SHA256 sha = SHA256.Create();

            foreach (ClientBuildFile dbf in dbFiles)
            {
                string localFile = Path.Combine(buildDir, dbf.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                string fileSha = dbf.Sha256;
                long fileSize = dbf.FileSizeBytes;

                if (System.IO.File.Exists(localFile))
                {
                    FileInfo fi = new FileInfo(localFile);
                    fileSize = fi.Length;
                    using FileStream stream = System.IO.File.OpenRead(localFile);
                    fileSha = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }

                result.Add(new ManifestFileEntry
                {
                    Path = dbf.RelativePath.Replace('\\', '/'),
                    FileName = string.IsNullOrWhiteSpace(dbf.FileName) ? Path.GetFileName(dbf.RelativePath) : dbf.FileName,
                    Sha256 = fileSha,
                    Size = fileSize,
                    IsRequired = dbf.IsRequired,
                    DownloadUrl = !string.IsNullOrWhiteSpace(dbf.DownloadUrl) ? dbf.DownloadUrl : $"/api/builds/{cleanSlug}/file/{dbf.RelativePath.Replace('\\', '/')}",
                    Category = dbf.Category ?? "Core",
                    DisplayName = dbf.DisplayName ?? Path.GetFileNameWithoutExtension(dbf.RelativePath),
                    Description = dbf.Description ?? string.Empty,
                    IconUrl = dbf.IconUrl,
                    BannerUrl = dbf.BannerUrl,
                    ModrinthSlug = dbf.ModrinthSlug,
                    Source = dbf.SourceType ?? (dbf.DownloadUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true ? "Modrinth" : "Local")
                });
            }

            return result;
        }

        [HttpGet("api/builds/{slug}/file/{**filePath}")]
        public IActionResult GetBuildFile(string slug, string filePath)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(filePath))
                return BadRequest();

            string cleanSlug = Path.GetFileName(slug).ToLowerInvariant();
            string cleanPath = filePath.Replace('\\', '/').TrimStart('/');

            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string fullPath = Path.GetFullPath(Path.Combine(root, "builds", cleanSlug, cleanPath));

            string expectedBase = Path.GetFullPath(Path.Combine(root, "builds", cleanSlug));
            if (!fullPath.StartsWith(expectedBase, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { error = "Build file not found on server." });

            string fileName = Path.GetFileName(fullPath);
            return PhysicalFile(fullPath, "application/octet-stream", fileName);
        }

        [HttpGet("api/launcher/download")]
        public IActionResult DownloadLauncher()
        {
            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string path = Path.Combine(root, "downloads", "VECLauncher.exe");
            if (!System.IO.File.Exists(path))
            {
                return NotFound(new { error = "Launcher binary not found on server." });
            }
            return PhysicalFile(path, "application/vnd.microsoft.portable-executable", "VECLauncher.exe");
        }

        [HttpGet("api/launcher/version")]
        public IActionResult GetLauncherVersion()
        {
            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string manifestPath = Path.Combine(root, "downloads", "manifest.json");

            if (System.IO.File.Exists(manifestPath))
            {
                string content = System.IO.File.ReadAllText(manifestPath, Encoding.UTF8);
                return Content(content, "application/json");
            }

            string zipPath = Path.Combine(root, "downloads", "VECLauncher-win-x64.zip");
            string exePath = Path.Combine(root, "downloads", "VECLauncher.exe");

            string downloadUrl = "/downloads/VECLauncher-win-x64.zip";
            string targetPath = zipPath;
            if (!System.IO.File.Exists(zipPath) && System.IO.File.Exists(exePath))
            {
                downloadUrl = "/downloads/VECLauncher.exe";
                targetPath = exePath;
            }

            if (!System.IO.File.Exists(targetPath))
            {
                return NotFound(new { error = "No launcher release payload found." });
            }

            FileInfo fi = new FileInfo(targetPath);
            using SHA256 sha = SHA256.Create();
            using FileStream stream = System.IO.File.OpenRead(targetPath);
            string hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();

            return Ok(new
            {
                version = "1.0.0",
                downloadUrl = downloadUrl,
                sha256 = hash,
                fileSize = fi.Length,
                changelog = "VEC Launcher official release"
            });
        }
    }
}
