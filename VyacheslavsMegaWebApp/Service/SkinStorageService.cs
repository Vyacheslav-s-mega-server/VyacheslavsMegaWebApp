using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace VyacheslavsMegaWebApp.Service
{
    public class SkinStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IServiceProvider _serviceProvider;

        public SkinStorageService(IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            _env = env;
            _serviceProvider = serviceProvider;
            EnsureDirectories();
        }

        private string SafeWebRoot
        {
            get
            {
                var candidate = Path.Combine(_env.ContentRootPath, "VyacheslavsMegaWebApp-dev", "VyacheslavsMegaWebApp-dev", "VyacheslavsMegaWebApp", "wwwroot");
                if (Directory.Exists(candidate)) return candidate;
                return _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            }
        }

        private string? FallbackWebRoot
        {
            get
            {
                var candidate = Path.Combine(_env.ContentRootPath, "wwwroot");
                if (Directory.Exists(candidate) && !string.Equals(candidate, SafeWebRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
                return null;
            }
        }

        private void SyncFileToFallback(string targetPath)
        {
            try
            {
                if (FallbackWebRoot != null && targetPath.StartsWith(SafeWebRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var rel = Path.GetRelativePath(SafeWebRoot, targetPath);
                    var dest = Path.Combine(FallbackWebRoot, rel);
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.Copy(targetPath, dest, true);
                }
            }
            catch { }
        }

        private void SyncDeleteToFallback(string targetPath)
        {
            try
            {
                if (FallbackWebRoot != null && targetPath.StartsWith(SafeWebRoot, StringComparison.OrdinalIgnoreCase))
                {
                    var rel = Path.GetRelativePath(SafeWebRoot, targetPath);
                    var dest = Path.Combine(FallbackWebRoot, rel);
                    if (File.Exists(dest)) File.Delete(dest);
                }
            }
            catch { }
        }

        private string WebUploadsSkinDir => Path.Combine(SafeWebRoot, "uploads", "skins");
        private string WebUploadsCapeDir => Path.Combine(SafeWebRoot, "uploads", "capes");
        private string WebUploadsTexturesDir => Path.Combine(SafeWebRoot, "uploads", "textures");
        private string WebUploadsBannerDir => Path.Combine(SafeWebRoot, "uploads", "banners");
        private string WebUploadsAvatarDir => Path.Combine(SafeWebRoot, "uploads", "avatars");
        private string WebUploadsProfileDir => Path.Combine(SafeWebRoot, "uploads", "profiles");
        private string WebImgCapesDir => Path.Combine(SafeWebRoot, "img", "capes");
        private string WebImgSkinsDir => Path.Combine(SafeWebRoot, "img", "skins");

        private string? LocalLauncherRoot
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrEmpty(appData)) return null;
                var path = Path.Combine(appData, "VECLauncher");
                try { Directory.CreateDirectory(path); } catch { }
                return path;
            }
        }

        private void EnsureDirectories()
        {
            try
            {
                Directory.CreateDirectory(WebUploadsSkinDir);
                Directory.CreateDirectory(WebUploadsCapeDir);
                Directory.CreateDirectory(WebUploadsTexturesDir);
                Directory.CreateDirectory(WebUploadsBannerDir);
                Directory.CreateDirectory(WebUploadsAvatarDir);
                Directory.CreateDirectory(WebUploadsProfileDir);
                Directory.CreateDirectory(WebImgCapesDir);
                Directory.CreateDirectory(WebImgSkinsDir);

                if (LocalLauncherRoot != null)
                {
                    Directory.CreateDirectory(Path.Combine(LocalLauncherRoot, "textures"));
                }
            }
            catch { }
        }

        public static string ComputeSha256(byte[] data)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
        }

        public string SaveTextureBytes(byte[] bytes)
        {
            EnsureDirectories();
            var hash = ComputeSha256(bytes);
            var path = Path.Combine(WebUploadsTexturesDir, $"{hash}.png");
            if (!File.Exists(path))
            {
                SafeWriteBytes(path, bytes);
            }
            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "textures", $"{hash}.png");
                if (!File.Exists(launcherPath))
                {
                    try { SafeWriteBytes(launcherPath, bytes); } catch { }
                }
            }
            return hash;
        }

        public byte[]? GetTextureBytesByHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return null;
            var cleanHash = Path.GetFileNameWithoutExtension(hash).ToLowerInvariant();
            var path = Path.Combine(WebUploadsTexturesDir, $"{cleanHash}.png");
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            if (LocalLauncherRoot != null)
            {
                var launcherTexture = Path.Combine(LocalLauncherRoot, "textures", $"{cleanHash}.png");
                if (File.Exists(launcherTexture))
                {
                    try { SafeWriteBytes(path, File.ReadAllBytes(launcherTexture)); } catch { }
                    return File.ReadAllBytes(launcherTexture);
                }
            }
            return null;
        }

        private void UpdateUserSkinMeta(string username, string? model = null, string? hash = null, string? capeHash = null, bool clearCape = false)
        {
            try
            {
                EnsureDirectories();
                var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
                string currentModel = "classic";
                string? currentSkinHash = null;
                string? currentCapeHash = null;

                if (File.Exists(metaFile))
                {
                    try
                    {
                        var json = File.ReadAllText(metaFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("model", out var m)) currentModel = m.GetString() ?? "classic";
                        if (doc.RootElement.TryGetProperty("skinHash", out var sh)) currentSkinHash = sh.GetString();
                        if (doc.RootElement.TryGetProperty("capeHash", out var ch)) currentCapeHash = ch.GetString();
                    }
                    catch { }
                }

                if (model != null) currentModel = model;
                if (hash != null) currentSkinHash = hash;
                if (clearCape) currentCapeHash = null;
                else if (capeHash != null) currentCapeHash = capeHash;

                var data = new
                {
                    model = currentModel,
                    skinHash = currentSkinHash,
                    capeHash = currentCapeHash
                };

                var newJson = JsonSerializer.Serialize(data);
                File.WriteAllText(metaFile, newJson);

                if (LocalLauncherRoot != null)
                {
                    var launcherMeta = Path.Combine(LocalLauncherRoot, "skins", $"{username}.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(launcherMeta)!);
                    File.WriteAllText(launcherMeta, newJson);
                }
            }
            catch { }
        }

        public string ResolveUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return username;
            var clean = username.Trim();

            var directSkin = Path.Combine(WebUploadsSkinDir, $"{clean}.png");
            var directMeta = Path.Combine(WebUploadsSkinDir, $"{clean}.json");
            var directProfile = Path.Combine(WebUploadsProfileDir, $"{clean}.json");
            if (File.Exists(directSkin) || File.Exists(directMeta) || File.Exists(directProfile))
            {
                return clean;
            }

            try
            {
                if (Directory.Exists(WebUploadsProfileDir))
                {
                    foreach (var file in Directory.EnumerateFiles(WebUploadsProfileDir, "*.json"))
                    {
                        try
                        {
                            var json = File.ReadAllText(file);
                            using var doc = JsonDocument.Parse(json);
                            if (doc.RootElement.TryGetProperty("minecraftUsername", out var m))
                            {
                                var mcNick = m.GetString();
                                if (!string.IsNullOrEmpty(mcNick) && string.Equals(mcNick, clean, StringComparison.OrdinalIgnoreCase))
                                {
                                    return Path.GetFileNameWithoutExtension(file);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return clean;
        }

        public string? GetSkinHash(string username)
        {
            username = ResolveUsername(username);
            var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("skinHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h)) return h;
                    }
                }
                catch { }
            }

            var skinBytes = GetSkinBytes(username);
            if (skinBytes != null && skinBytes.Length > 0)
            {
                var hash = SaveTextureBytes(skinBytes);
                UpdateUserSkinMeta(username, hash: hash);
                return hash;
            }

            return null;
        }

        public string? GetCapeHash(string username)
        {
            username = ResolveUsername(username);
            var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("capeHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h)) return h;
                    }
                }
                catch { }
            }

            var capeBytes = GetCapeBytes(username);
            if (capeBytes != null && capeBytes.Length > 0)
            {
                var hash = SaveTextureBytes(capeBytes);
                UpdateUserSkinMeta(username, capeHash: hash);
                return hash;
            }

            return null;
        }

        public string GetSkinModel(string username)
        {
            username = ResolveUsername(username);
            var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("model", out var modelProp))
                    {
                        var model = modelProp.GetString();
                        if (string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase))
                            return "slim";
                    }
                }
                catch { }
            }
            return "classic";
        }

        public void SetSkinModel(string username, string model)
        {
            var cleanModel = string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase) ? "slim" : "classic";
            UpdateUserSkinMeta(username, model: cleanModel);
        }

        public bool HasCustomSkin(string username)
        {
            var webPath = Path.Combine(WebUploadsSkinDir, $"{username}.png");
            if (File.Exists(webPath)) return true;

            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "skins", $"{username}.png");
                if (File.Exists(launcherPath)) return true;
            }

            return false;
        }

        public byte[] GetSkinBytes(string username)
        {
            username = ResolveUsername(username);
            var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("skinHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h))
                        {
                            var bytes = GetTextureBytesByHash(h);
                            if (bytes != null && bytes.Length > 0) return bytes;
                        }
                    }
                }
                catch { }
            }

            var webPath = Path.Combine(WebUploadsSkinDir, $"{username}.png");
            if (File.Exists(webPath))
            {
                var b = File.ReadAllBytes(webPath);
                SaveTextureBytes(b);
                return b;
            }

            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "skins", $"{username}.png");
                if (File.Exists(launcherPath))
                {
                    try
                    {
                        File.Copy(launcherPath, webPath, true);
                    }
                    catch { }
                    var b = File.ReadAllBytes(launcherPath);
                    SaveTextureBytes(b);
                    return b;
                }
            }

            var model = GetSkinModel(username);
            var defaultFile = model == "slim" ? "alex_slim.png" : "steve_classic.png";
            var defaultPath = Path.Combine(WebImgSkinsDir, defaultFile);
            if (!File.Exists(defaultPath))
            {
                var fallback = Path.Combine(_env.ContentRootPath, "wwwroot", "img", "skins", defaultFile);
                if (File.Exists(fallback)) defaultPath = fallback;
            }

            if (File.Exists(defaultPath))
                return File.ReadAllBytes(defaultPath);

            return Array.Empty<byte>();
        }

        public string GetSkinUrl(string username)
        {
            var hash = GetSkinHash(username);
            if (!string.IsNullOrEmpty(hash))
                return $"/textures/{hash}";
            return $"/api/skin/{Uri.EscapeDataString(username)}.png?t={DateTime.UtcNow.Ticks}";
        }

        public async Task<bool> SaveSkinAsync(string username, IFormFile file, string model = "classic")
        {
            if (file == null || file.Length == 0) return false;

            EnsureDirectories();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var hash = SaveTextureBytes(bytes);
            var cleanModel = string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase) ? "slim" : "classic";
            UpdateUserSkinMeta(username, model: cleanModel, hash: hash);

            var webPath = Path.Combine(WebUploadsSkinDir, $"{username}.png");
            SafeWriteBytes(webPath, bytes);
            SyncSkinToLauncher(username, webPath, cleanModel);

            return true;
        }

        public async Task<bool> SaveSkinBytesAsync(string username, byte[] bytes, string model = "classic")
        {
            if (bytes == null || bytes.Length == 0) return false;

            EnsureDirectories();
            var hash = SaveTextureBytes(bytes);
            var cleanModel = string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase) ? "slim" : "classic";
            UpdateUserSkinMeta(username, model: cleanModel, hash: hash);

            var webPath = Path.Combine(WebUploadsSkinDir, $"{username}.png");
            SafeWriteBytes(webPath, bytes);
            SyncSkinToLauncher(username, webPath, cleanModel);

            return true;
        }

        public void ResetSkin(string username)
        {
            UpdateUserSkinMeta(username, model: "classic", hash: string.Empty);

            var webPath = Path.Combine(WebUploadsSkinDir, $"{username}.png");
            if (File.Exists(webPath))
            {
                try { File.Delete(webPath); } catch { }
            }

            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "skins", $"{username}.png");
                if (File.Exists(launcherPath))
                {
                    try { File.Delete(launcherPath); } catch { }
                }
            }
        }

        public bool HasCape(string username)
        {
            username = ResolveUsername(username);
            var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("capeHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h)) return true;
                    }
                }
                catch { }
            }

            var webPath = Path.Combine(WebUploadsCapeDir, $"{username}.png");
            if (File.Exists(webPath)) return true;

            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "capes", $"{username}.png");
                if (File.Exists(launcherPath)) return true;
            }

            return false;
        }

        public byte[]? GetCapeBytes(string username)
        {
            username = ResolveUsername(username);
            var metaFile = Path.Combine(WebUploadsSkinDir, $"{username}.json");
            if (File.Exists(metaFile))
            {
                try
                {
                    var json = File.ReadAllText(metaFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("capeHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h))
                        {
                            var b = GetTextureBytesByHash(h);
                            if (b != null && b.Length > 0) return b;
                        }
                    }
                }
                catch { }
            }

            var webPath = Path.Combine(WebUploadsCapeDir, $"{username}.png");
            if (File.Exists(webPath))
            {
                var b = File.ReadAllBytes(webPath);
                SaveTextureBytes(b);
                return b;
            }

            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "capes", $"{username}.png");
                if (File.Exists(launcherPath))
                {
                    try
                    {
                        File.Copy(launcherPath, webPath, true);
                    }
                    catch { }
                    var b = File.ReadAllBytes(launcherPath);
                    SaveTextureBytes(b);
                    return b;
                }
            }

            return null;
        }

        public string? GetCapeUrl(string username)
        {
            if (!HasCape(username)) return null;
            var hash = GetCapeHash(username);
            if (!string.IsNullOrEmpty(hash))
                return $"/textures/{hash}";
            return $"/api/cape/{Uri.EscapeDataString(username)}.png?t={DateTime.UtcNow.Ticks}";
        }

        private void SafeWriteBytes(string path, byte[] bytes)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    var dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    fs.Write(bytes, 0, bytes.Length);
                    SyncFileToFallback(path);
                    return;
                }
                catch (IOException)
                {
                    if (attempt == 4) throw;
                    System.Threading.Thread.Sleep(50);
                }
            }
        }

        public string? DetectEquippedCapeId(string username)
        {
            var userCape = GetCapeBytes(username);
            if (userCape == null || userCape.Length == 0) return null;

            var vecPath = Path.Combine(WebImgCapesDir, "cape_vec.png");
            if (!File.Exists(vecPath))
            {
                var fallback = Path.Combine(_env.ContentRootPath, "wwwroot", "img", "capes", "cape_vec.png");
                if (File.Exists(fallback)) vecPath = fallback;
            }

            if (File.Exists(vecPath))
            {
                var vecBytes = File.ReadAllBytes(vecPath);
                if (userCape.SequenceEqual(vecBytes) || (userCape.Length == vecBytes.Length && userCape[0] == vecBytes[0]))
                    return "vec_default";
            }

            var mojangDir = Path.Combine(WebImgCapesDir, "mojang");
            if (!Directory.Exists(mojangDir))
            {
                var fallback = Path.Combine(_env.ContentRootPath, "wwwroot", "img", "capes", "mojang");
                if (Directory.Exists(fallback)) mojangDir = fallback;
            }

            if (Directory.Exists(mojangDir))
            {
                foreach (var file in Directory.GetFiles(mojangDir, "*.png"))
                {
                    try
                    {
                        var mcBytes = File.ReadAllBytes(file);
                        if (userCape.SequenceEqual(mcBytes))
                        {
                            var mcId = Path.GetFileNameWithoutExtension(file);
                            return $"mojang_{mcId}";
                        }
                    }
                    catch { }
                }
            }

            try
            {
                var userCapeHash = GetCapeHash(username);
                if (!string.IsNullOrEmpty(userCapeHash))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                    var custom = db.CustomCapes.FirstOrDefault(c => c.TextureHash == userCapeHash);
                    if (custom != null)
                    {
                        return custom.Slug == "vec" ? "vec_default" : $"custom_{custom.Id}";
                    }
                }
            }
            catch { }

            return "custom";
        }

        public bool EquipLauncherCape(string username, string capeId)
        {
            EnsureDirectories();
            string? srcPath = null;
            byte[]? directBytes = null;

            if (capeId.Equals("vec_default", StringComparison.OrdinalIgnoreCase) || capeId.Equals("vec", StringComparison.OrdinalIgnoreCase))
            {
                var p = Path.Combine(WebImgCapesDir, "cape_vec.png");
                if (!File.Exists(p))
                {
                    var fallback = Path.Combine(_env.ContentRootPath, "wwwroot", "img", "capes", "cape_vec.png");
                    if (File.Exists(fallback)) p = fallback;
                }
                if (File.Exists(p)) srcPath = p;
            }
            else if (capeId.StartsWith("mojang_", StringComparison.OrdinalIgnoreCase))
            {
                var mcId = capeId.Substring(7);
                var p = Path.Combine(WebImgCapesDir, "mojang", $"{mcId}.png");
                if (!File.Exists(p))
                {
                    var fallback = Path.Combine(_env.ContentRootPath, "wwwroot", "img", "capes", "mojang", $"{mcId}.png");
                    if (File.Exists(fallback)) p = fallback;
                }
                if (File.Exists(p))
                {
                    srcPath = p;
                }
                else
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var userMgr = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Data.Entities.Identity.VmsIdentityUser>>();
                        var usr = userMgr.FindByNameAsync(username).GetAwaiter().GetResult();
                        if (usr != null && !string.IsNullOrWhiteSpace(usr.OwnedMojangCapesJson))
                        {
                            var list = JsonSerializer.Deserialize<List<Models.MojangCapeInfo>>(usr.OwnedMojangCapesJson);
                            var match = list?.FirstOrDefault(c => c.Id.Equals(mcId, StringComparison.OrdinalIgnoreCase) || c.Alias.Equals(mcId, StringComparison.OrdinalIgnoreCase));
                            if (match != null && !string.IsNullOrEmpty(match.Url))
                            {
                                using var http = new HttpClient();
                                directBytes = http.GetByteArrayAsync(match.Url).GetAwaiter().GetResult();
                                if (directBytes != null && directBytes.Length > 0)
                                {
                                    var dir = Path.Combine(WebImgCapesDir, "mojang");
                                    Directory.CreateDirectory(dir);
                                    File.WriteAllBytes(Path.Combine(dir, $"{mcId}.png"), directBytes);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            else if (capeId.StartsWith("custom_", StringComparison.OrdinalIgnoreCase))
            {
                var idStr = capeId.Substring(7);
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                    int.TryParse(idStr, out var cId);
                    var cape = db.CustomCapes.FirstOrDefault(c => c.Id == cId || c.Slug == idStr);
                    if (cape != null)
                    {
                        directBytes = GetTextureBytesByHash(cape.TextureHash);
                    }
                }
                catch { }
            }

            byte[]? bytes = directBytes;
            if (bytes == null && srcPath != null && File.Exists(srcPath))
            {
                bytes = File.ReadAllBytes(srcPath);
            }

            if (bytes == null || bytes.Length == 0) return false;

            var hash = SaveTextureBytes(bytes);
            UpdateUserSkinMeta(username, capeHash: hash);

            var targetPath = Path.Combine(WebUploadsCapeDir, $"{username}.png");
            SafeWriteBytes(targetPath, bytes);

            SyncCapeToLauncher(username, targetPath);
            return true;
        }

        public List<object> GetCapesCatalog()
        {
            EnsureDirectories();
            var list = new List<object>();

            var vecPath = Path.Combine(WebImgCapesDir, "cape_vec.png");
            if (!File.Exists(vecPath))
            {
                var fallback = Path.Combine(_env.ContentRootPath, "wwwroot", "img", "capes", "cape_vec.png");
                if (File.Exists(fallback)) vecPath = fallback;
            }

            if (File.Exists(vecPath))
            {
                var bytes = File.ReadAllBytes(vecPath);
                var hash = SaveTextureBytes(bytes);
                list.Add(new
                {
                    id = "vec_default",
                    name = "VEC Official Cape",
                    url = $"/textures/{hash}"
                });
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                var publicCapes = db.CustomCapes.Where(c => c.IsPublic).ToList();
                foreach (var c in publicCapes)
                {
                    if (c.Slug == "vec") continue;
                    list.Add(new
                    {
                        id = $"custom_{c.Id}",
                        name = c.Name,
                        url = $"/textures/{c.TextureHash}"
                    });
                }
            }
            catch { }

            return list;
        }

        public async Task<bool> SaveCustomCapeAsync(string username, IFormFile file)
        {
            if (file == null || file.Length == 0) return false;

            EnsureDirectories();
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var hash = SaveTextureBytes(bytes);
            UpdateUserSkinMeta(username, capeHash: hash);

            var targetPath = Path.Combine(WebUploadsCapeDir, $"{username}.png");
            SafeWriteBytes(targetPath, bytes);

            SyncCapeToLauncher(username, targetPath);
            return true;
        }

        public async Task<bool> SaveCapeBytesAsync(string username, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return false;

            EnsureDirectories();
            var hash = SaveTextureBytes(bytes);
            UpdateUserSkinMeta(username, capeHash: hash);

            var targetPath = Path.Combine(WebUploadsCapeDir, $"{username}.png");
            SafeWriteBytes(targetPath, bytes);

            SyncCapeToLauncher(username, targetPath);
            return true;
        }

        public void RemoveCape(string username)
        {
            UpdateUserSkinMeta(username, clearCape: true);

            var webPath = Path.Combine(WebUploadsCapeDir, $"{username}.png");
            if (File.Exists(webPath))
            {
                try { File.Delete(webPath); } catch { }
            }

            if (LocalLauncherRoot != null)
            {
                var launcherPath = Path.Combine(LocalLauncherRoot, "capes", $"{username}.png");
                if (File.Exists(launcherPath))
                {
                    try { File.Delete(launcherPath); } catch { }
                }
            }
        }

        private void SyncSkinToLauncher(string username, string skinFilePath, string model)
        {
            if (LocalLauncherRoot == null) return;
            try
            {
                var bytes = File.ReadAllBytes(skinFilePath);
                var launcherSkinsDir = Path.Combine(LocalLauncherRoot, "skins");
                Directory.CreateDirectory(launcherSkinsDir);
                var destPath = Path.Combine(launcherSkinsDir, $"{username}.png");
                SafeWriteBytes(destPath, bytes);

                var isSlim = string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase);
                var json = JsonSerializer.Serialize(new { model = isSlim ? "slim" : "default" });
                File.WriteAllText(Path.Combine(launcherSkinsDir, $"{username}.json"), json);
            }
            catch { }
        }

        private void SyncCapeToLauncher(string username, string capeFilePath)
        {
            if (LocalLauncherRoot == null) return;
            try
            {
                var bytes = File.ReadAllBytes(capeFilePath);
                var launcherCapesDir = Path.Combine(LocalLauncherRoot, "capes");
                Directory.CreateDirectory(launcherCapesDir);
                var destPath = Path.Combine(launcherCapesDir, $"{username}.png");
                SafeWriteBytes(destPath, bytes);
            }
            catch { }
        }

        public string? GetUserAvatarHash(string username)
        {
            EnsureDirectories();
            var profileFile = Path.Combine(WebUploadsProfileDir, $"{username}.json");
            if (File.Exists(profileFile))
            {
                try
                {
                    var json = File.ReadAllText(profileFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("avatarHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h)) return h;
                    }
                }
                catch { }
            }
            return null;
        }

        public string? GetUserBannerHash(string username)
        {
            EnsureDirectories();
            var profileFile = Path.Combine(WebUploadsProfileDir, $"{username}.json");
            if (File.Exists(profileFile))
            {
                try
                {
                    var json = File.ReadAllText(profileFile);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("bannerHash", out var prop))
                    {
                        var h = prop.GetString();
                        if (!string.IsNullOrEmpty(h)) return h;
                    }
                }
                catch { }
            }
            return null;
        }

        public void UpdateUserProfileHashes(string username, string? avatarHash = null, string? bannerHash = null, bool clearAvatar = false, bool clearBanner = false)
        {
            try
            {
                EnsureDirectories();
                var profileFile = Path.Combine(WebUploadsProfileDir, $"{username}.json");
                string siteName = username;
                string minecraftUsername = username;
                string createdAt = DateTime.UtcNow.ToString("dd.MM.yyyy");
                string? currentAvatar = null;
                string? currentBanner = null;

                if (File.Exists(profileFile))
                {
                    try
                    {
                        var json = File.ReadAllText(profileFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("siteName", out var s)) siteName = s.GetString() ?? username;
                        if (doc.RootElement.TryGetProperty("minecraftUsername", out var m)) minecraftUsername = m.GetString() ?? username;
                        if (doc.RootElement.TryGetProperty("createdAt", out var c)) createdAt = c.GetString() ?? createdAt;
                        if (doc.RootElement.TryGetProperty("avatarHash", out var a)) currentAvatar = a.GetString();
                        if (doc.RootElement.TryGetProperty("bannerHash", out var b)) currentBanner = b.GetString();
                    }
                    catch { }
                }

                if (clearAvatar) currentAvatar = null;
                else if (avatarHash != null) currentAvatar = avatarHash;

                if (clearBanner) currentBanner = null;
                else if (bannerHash != null) currentBanner = bannerHash;

                var data = new
                {
                    siteName = siteName,
                    minecraftUsername = minecraftUsername,
                    createdAt = createdAt,
                    avatarHash = currentAvatar,
                    bannerHash = currentBanner
                };

                var newJson = JsonSerializer.Serialize(data);
                File.WriteAllText(profileFile, newJson);
                SyncFileToFallback(profileFile);
            }
            catch { }
        }

        public string GetBannerUrl(string username)
        {
            var hashFile = GetUserBannerHash(username);
            if (!string.IsNullOrEmpty(hashFile))
            {
                var hashPath = Path.Combine(WebUploadsBannerDir, hashFile);
                if (File.Exists(hashPath))
                {
                    return $"/uploads/banners/{hashFile}";
                }
            }

            var exts = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            foreach (var ext in exts)
            {
                var filePath = Path.Combine(WebUploadsBannerDir, $"{username}{ext}");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(filePath);
                        var hash = ComputeSha256(bytes);
                        var hashFileName = $"{hash}{ext}";
                        var newDest = Path.Combine(WebUploadsBannerDir, hashFileName);
                        if (!File.Exists(newDest)) SafeWriteBytes(newDest, bytes);
                        UpdateUserProfileHashes(username, bannerHash: hashFileName);
                        return $"/uploads/banners/{hashFileName}";
                    }
                    catch
                    {
                        var fileVer = File.GetLastWriteTimeUtc(filePath).Ticks;
                        return $"/uploads/banners/{username}{ext}?v={fileVer}";
                    }
                }
            }
            return "/img/backgrounds/bg1.webp";
        }

        public bool HasCustomBanner(string username)
        {
            var hashFile = GetUserBannerHash(username);
            if (!string.IsNullOrEmpty(hashFile))
            {
                if (File.Exists(Path.Combine(WebUploadsBannerDir, hashFile))) return true;
            }
            var exts = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            return exts.Any(ext => File.Exists(Path.Combine(WebUploadsBannerDir, $"{username}{ext}")));
        }

        public async Task<bool> SaveBannerAsync(string username, IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp") return false;
            if (file.Length > 10 * 1024 * 1024) return false;

            EnsureDirectories();

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var hash = ComputeSha256(bytes);
            var hashFileName = $"{hash}{ext}";

            var destPath = Path.Combine(WebUploadsBannerDir, hashFileName);
            SafeWriteBytes(destPath, bytes);

            UpdateUserProfileHashes(username, bannerHash: hashFileName);
            return true;
        }

        public bool ResetBanner(string username)
        {
            UpdateUserProfileHashes(username, clearBanner: true);
            var exts = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            var deleted = false;
            foreach (var ext in exts)
            {
                var filePath = Path.Combine(WebUploadsBannerDir, $"{username}{ext}");
                if (File.Exists(filePath))
                {
                    try { File.Delete(filePath); deleted = true; } catch { }
                    SyncDeleteToFallback(filePath);
                }
            }
            return deleted;
        }

        public string GetAvatarUrl(string username)
        {
            var hashFile = GetUserAvatarHash(username);
            if (!string.IsNullOrEmpty(hashFile))
            {
                var hashPath = Path.Combine(WebUploadsAvatarDir, hashFile);
                if (File.Exists(hashPath))
                {
                    return $"/uploads/avatars/{hashFile}";
                }
            }

            var exts = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            foreach (var ext in exts)
            {
                var filePath = Path.Combine(WebUploadsAvatarDir, $"{username}{ext}");
                if (File.Exists(filePath))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(filePath);
                        var hash = ComputeSha256(bytes);
                        var hashFileName = $"{hash}{ext}";
                        var newDest = Path.Combine(WebUploadsAvatarDir, hashFileName);
                        if (!File.Exists(newDest)) SafeWriteBytes(newDest, bytes);
                        UpdateUserProfileHashes(username, avatarHash: hashFileName);
                        return $"/uploads/avatars/{hashFileName}";
                    }
                    catch
                    {
                        var fileVer = File.GetLastWriteTimeUtc(filePath).Ticks;
                        return $"/uploads/avatars/{username}{ext}?v={fileVer}";
                    }
                }
            }
            return $"https://minotar.net/helm/{username}/112.png";
        }

        public async Task<bool> SaveAvatarAsync(string username, IFormFile file)
        {
            if (file == null || file.Length == 0) return false;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".webp") return false;
            if (file.Length > 5 * 1024 * 1024) return false;

            EnsureDirectories();

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var hash = ComputeSha256(bytes);
            var hashFileName = $"{hash}{ext}";

            var destPath = Path.Combine(WebUploadsAvatarDir, hashFileName);
            SafeWriteBytes(destPath, bytes);

            UpdateUserProfileHashes(username, avatarHash: hashFileName);
            return true;
        }

        public (string siteName, string minecraftUsername, string createdAt) GetUserProfileData(string username)
        {
            EnsureDirectories();
            var profileFile = Path.Combine(WebUploadsProfileDir, $"{username}.json");
            if (File.Exists(profileFile))
            {
                try
                {
                    var json = File.ReadAllText(profileFile);
                    using var doc = JsonDocument.Parse(json);
                    var site = doc.RootElement.TryGetProperty("siteName", out var s) ? s.GetString() ?? username : username;
                    var mc = doc.RootElement.TryGetProperty("minecraftUsername", out var m) ? m.GetString() ?? username : username;
                    var created = doc.RootElement.TryGetProperty("createdAt", out var c) ? c.GetString() ?? DateTime.UtcNow.ToString("dd.MM.yyyy") : DateTime.UtcNow.ToString("dd.MM.yyyy");
                    return (site, mc, created);
                }
                catch { }
            }
            var defaultCreated = DateTime.UtcNow.ToString("dd.MM.yyyy");
            SaveUserProfileData(username, username, username, defaultCreated);
            return (username, username, defaultCreated);
        }

        public void SaveUserProfileData(string username, string siteName, string minecraftUsername, string? createdAt = null)
        {
            try
            {
                EnsureDirectories();
                var profileFile = Path.Combine(WebUploadsProfileDir, $"{username}.json");
                string created = createdAt ?? DateTime.UtcNow.ToString("dd.MM.yyyy");
                string? currentAvatar = null;
                string? currentBanner = null;

                if (File.Exists(profileFile))
                {
                    try
                    {
                        var json = File.ReadAllText(profileFile);
                        using var doc = JsonDocument.Parse(json);
                        if (createdAt == null && doc.RootElement.TryGetProperty("createdAt", out var c))
                        {
                            created = c.GetString() ?? created;
                        }
                        if (doc.RootElement.TryGetProperty("avatarHash", out var a)) currentAvatar = a.GetString();
                        if (doc.RootElement.TryGetProperty("bannerHash", out var b)) currentBanner = b.GetString();
                    }
                    catch { }
                }

                var data = new
                {
                    siteName = string.IsNullOrWhiteSpace(siteName) ? username : siteName.Trim(),
                    minecraftUsername = string.IsNullOrWhiteSpace(minecraftUsername) ? username : minecraftUsername.Trim(),
                    createdAt = created,
                    avatarHash = currentAvatar,
                    bannerHash = currentBanner
                };

                var newJson = JsonSerializer.Serialize(data);
                File.WriteAllText(profileFile, newJson);
                SyncFileToFallback(profileFile);
            }
            catch { }
        }
    }
}
