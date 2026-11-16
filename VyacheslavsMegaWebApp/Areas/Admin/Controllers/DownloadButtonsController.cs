using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VyacheslavsMegaWebApp.Areas.Admin.Models;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Repositories;

namespace VyacheslavsMegaWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DownloadButtonsController : Controller
    {
        private readonly DownloadButtonsRepository _downloadButtonssRepository;
        private readonly IWebHostEnvironment _env;

        public DownloadButtonsController(DownloadButtonsRepository buttonssRepository, IWebHostEnvironment env)
        {
            _downloadButtonssRepository = buttonssRepository;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var launcherPath = Path.Combine(root, "downloads", "VECLauncher.exe");
            var launcherInfo = new FileInfo(launcherPath);

            var setupPath = Path.Combine(root, "downloads", "VEC_Setup.exe");
            var setupInfo = new FileInfo(setupPath);

            var zipPath = Path.Combine(root, "downloads", "VECLauncher-win-x64.zip");
            var zipInfo = new FileInfo(zipPath);

            var model = new DownloadButtonsViewModel(await _downloadButtonssRepository.GetAllButtons())
            {
                LauncherFileExists = launcherInfo.Exists,
                LauncherFileSizeBytes = launcherInfo.Exists ? launcherInfo.Length : 0,
                LauncherLastModified = launcherInfo.Exists ? launcherInfo.LastWriteTime : null,

                SetupFileExists = setupInfo.Exists,
                SetupFileSizeBytes = setupInfo.Exists ? setupInfo.Length : 0,
                SetupLastModified = setupInfo.Exists ? setupInfo.LastWriteTime : null,

                ZipFileExists = zipInfo.Exists,
                ZipFileSizeBytes = zipInfo.Exists ? zipInfo.Length : 0
            };
            return View(model);
        }

        [HttpPost]
        [RequestSizeLimit(350 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 350 * 1024 * 1024)]
        public async Task<IActionResult> UploadLauncher(IFormFile? launcherFile)
        {
            if (launcherFile == null || launcherFile.Length == 0)
            {
                TempData["Error"] = "Пожалуйста, выберите .exe файл для загрузки.";
                return RedirectToAction(nameof(Index));
            }

            var ext = Path.GetExtension(launcherFile.FileName).ToLowerInvariant();
            if (ext != ".exe")
            {
                TempData["Error"] = "Разрешена загрузка только исполняемых файлов с расширением .exe.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                var dir = Path.Combine(root, "downloads");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var isSetup = launcherFile.FileName.Contains("Setup", StringComparison.OrdinalIgnoreCase);
                var targetFileName = isSetup ? "VEC_Setup.exe" : "VECLauncher.exe";

                var targetPath = Path.Combine(dir, targetFileName);
                var tempPath = Path.Combine(dir, targetFileName + ".tmp");

                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }

                await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await launcherFile.CopyToAsync(stream);
                }

                if (System.IO.File.Exists(targetPath))
                {
                    System.IO.File.Delete(targetPath);
                }

                System.IO.File.Move(tempPath, targetPath);

                var mb = launcherFile.Length / (1024.0 * 1024.0);
                TempData["Success"] = $"Файл {targetFileName} успешно обновлён ({mb:F2} МБ)!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ошибка при сохранении файла: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ButtonDetails(int? buttonId)
        {
            if (buttonId == null)
                return View(new DownloadButton());
            return View(await _downloadButtonssRepository.GetButtonById(buttonId.Value));
        }

        [HttpPost]
        public async Task<IActionResult> ButtonDetails(DownloadButton model)
        {
            if (!ModelState.IsValid) return View(model);
            await _downloadButtonssRepository.SaveButton(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveButton(int buttonId)
        {
            await _downloadButtonssRepository.RemoveButtonById(buttonId);
            return RedirectToAction(nameof(Index));
        }
    }
}
