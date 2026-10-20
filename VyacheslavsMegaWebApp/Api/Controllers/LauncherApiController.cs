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
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Api.Models;
using VyacheslavsMegaWebApp.Data;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

namespace VyacheslavsMegaWebApp.Api.Controllers
{
    [ApiController]
    public class LauncherApiController : ControllerBase
    {
        private readonly LauncherTicketService _ticketService;
        private readonly LauncherSessionRepository _sessionRepository;
        private readonly ClientBuildsRepository _buildsRepository;
        private readonly UserManager<VmsIdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public LauncherApiController(
            LauncherTicketService ticketService,
            LauncherSessionRepository sessionRepository,
            ClientBuildsRepository buildsRepository,
            UserManager<VmsIdentityUser> userManager,
            IWebHostEnvironment env)
        {
            _ticketService = ticketService;
            _sessionRepository = sessionRepository;
            _buildsRepository = buildsRepository;
            _userManager = userManager;
            _env = env;
        }

        public class RequestTicketModel
        {
            public string Username { get; set; } = string.Empty;
            public string SessionKey { get; set; } = string.Empty;
            public string Hwid { get; set; } = string.Empty;
        }

        [HttpPost("api/launch/request-ticket")]
        public async Task<IActionResult> RequestTicket([FromBody] RequestTicketModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username))
                return BadRequest(new { error = "Username required." });

            string cleanUser = Path.GetFileNameWithoutExtension(model.Username).Trim();
            VmsIdentityUser? user = await _userManager.FindByNameAsync(cleanUser);
            if (user is null)
                return NotFound(new { error = "User not found." });

            if (!string.IsNullOrWhiteSpace(model.SessionKey))
            {
                LauncherSession? session = await _sessionRepository.GetSessionByKey(model.SessionKey);
                if (session is null || session.ExpiresAt < DateTime.UtcNow || session.UserId != user.Id)
                    return Unauthorized(new { error = "Invalid or expired session." });
            }

            LauncherTicket ticket = _ticketService.CreateTicket(cleanUser, model.Hwid ?? string.Empty);
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

        [HttpGet("api/launcher/version")]
        public IActionResult GetLauncherVersion()
        {
            return Ok(new
            {
                version = "3.0.0",
                minVersion = "2.0.0",
                downloadUrl = "/downloads/VEC_Launcher.exe",
                changelog = "VEC Ecosystem 3.0: Unified Yggdrasil API, Friends Tab, Launch Tickets, Wardrobe Sync."
            });
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
            var cleanSlug = string.IsNullOrWhiteSpace(slug) ? "vec-fabric-1-21-11" : slug.Trim().ToLowerInvariant();
            var files = await GetManifestDataAsync(cleanSlug);
            var mods = files.Where(f => f.Path.StartsWith("mods/", StringComparison.OrdinalIgnoreCase)).ToList();
            return Ok(new { build = cleanSlug, files = files, mods = mods });
        }

        private async Task<List<ManifestFileEntry>> GetManifestDataAsync(string cleanSlug)
        {
            List<ClientBuildFile> dbFiles = await _buildsRepository.GetBuildFilesBySlug(cleanSlug);

            if (dbFiles.Count == 0)
            {
                List<ClientBuildFile> defaults = new List<ClientBuildFile>
                {
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/fabric-api.jar", FileName = "fabric-api.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = true, Category = "Core", DisplayName = "Fabric API", Description = "Базовый модуль хуков и событий Fabric 1.21.11" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/voicechat-fabric.jar", FileName = "voicechat-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = true, Category = "Core", DisplayName = "Simple Voice Chat", Description = "Позиционный голосовой 3D чат сервера VEC" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/sodium-fabric.jar", FileName = "sodium-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = true, Category = "Optimization", DisplayName = "Sodium", Description = "Движок рендеринга и многократного повышения FPS" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/iris-fabric.jar", FileName = "iris-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = false, Category = "Visual", DisplayName = "Iris Shaders", Description = "Современный движок ультра-шейдеров" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/modmenu-fabric.jar", FileName = "modmenu-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = false, Category = "Utility", DisplayName = "Mod Menu", Description = "Удобный список и настройки модов в главном меню" },
                    new() { BuildSlug = cleanSlug, RelativePath = "mods/appleskin-fabric.jar", FileName = "appleskin-fabric.jar", Sha256 = "", FileSizeBytes = 0, IsRequired = false, Category = "Utility", DisplayName = "AppleSkin", Description = "Показ насыщения еды и очков голода" }
                };

                await _buildsRepository.AddBuildFiles(defaults);
                dbFiles = defaults;
            }

            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string buildDir = Path.Combine(root, "builds", cleanSlug);

            List<ManifestFileEntry> result = new List<ManifestFileEntry>();
            using SHA256 sha = SHA256.Create();

            foreach (var dbf in dbFiles)
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
                    Description = dbf.Description ?? string.Empty
                });
            }

            return result;
        }

        [HttpGet("api/builds/{slug}/file/{**filePath}")]
        public IActionResult GetBuildFile(string slug, string filePath)
        {
            if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(filePath))
                return BadRequest();

            var cleanSlug = Path.GetFileName(slug).ToLowerInvariant();
            var cleanPath = filePath.Replace('\\', '/').TrimStart('/');

            var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var fullPath = Path.GetFullPath(Path.Combine(root, "builds", cleanSlug, cleanPath));

            var expectedBase = Path.GetFullPath(Path.Combine(root, "builds", cleanSlug));
            if (!fullPath.StartsWith(expectedBase, StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { error = "Build file not found on server." });

            var fileName = Path.GetFileName(fullPath);
            return PhysicalFile(fullPath, "application/octet-stream", fileName);
        }

        [HttpGet("api/launcher/download")]
        public IActionResult DownloadLauncher()
        {
            var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var path = Path.Combine(root, "downloads", "VECLauncher.exe");
            if (!System.IO.File.Exists(path))
            {
                return NotFound(new { error = "Launcher binary not found on server." });
            }
            return PhysicalFile(path, "application/vnd.microsoft.portable-executable", "VECLauncher.exe");
        }
    }
}
