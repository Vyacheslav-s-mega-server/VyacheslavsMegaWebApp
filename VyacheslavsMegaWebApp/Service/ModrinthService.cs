using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VyacheslavsMegaWebApp.Service
{
    public class ModrinthProjectInfo
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string ProjectType { get; set; } = "mod";
        public string Category { get; set; } = "Optimization";
        public string FileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
    }

    public class ModrinthService
    {
        private readonly HttpClient _http;

        public ModrinthService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("VEC_Launcher/3.0 (contact@vec.kpvk.edu.kz)");
        }

        public static string? ExtractSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();

            if (input.Contains("modrinth.com", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(
                    input,
                    @"modrinth\.com/(?:mod|plugin|datapack|resourcepack|shader|modpack)/([A-Za-z0-9._-]+)",
                    RegexOptions.IgnoreCase);

                if (match.Success) return match.Groups[1].Value.ToLowerInvariant();
            }

            var clean = Regex.Replace(input, @"[^A-Za-z0-9._-]", "");
            return clean.Length >= 2 ? clean.ToLowerInvariant() : null;
        }

        public async Task<ModrinthProjectInfo?> FetchModrinthInfoAsync(string query, string defaultCategory = "Core")
        {
            var slug = ExtractSlug(query);
            if (string.IsNullOrEmpty(slug)) return null;

            try
            {
                var projectResp = await _http.GetAsync($"https://api.modrinth.com/v2/project/{slug}");
                if (!projectResp.IsSuccessStatusCode) return null;

                var projectJson = await projectResp.Content.ReadAsStringAsync();
                using var pDoc = JsonDocument.Parse(projectJson);
                var pRoot = pDoc.RootElement;

                var title = pRoot.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? slug : slug;
                var desc = pRoot.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? "" : "";
                var iconUrl = pRoot.TryGetProperty("icon_url", out var iProp) ? iProp.GetString() : null;
                var projType = pRoot.TryGetProperty("project_type", out var ptProp) ? ptProp.GetString() ?? "mod" : "mod";

                var category = defaultCategory;
                var subFolder = "mods";
                if (string.Equals(projType, "shader", StringComparison.OrdinalIgnoreCase))
                {
                    category = "Shaders";
                    subFolder = "shaderpacks";
                }
                else if (string.Equals(projType, "resourcepack", StringComparison.OrdinalIgnoreCase))
                {
                    category = "Resourcepacks";
                    subFolder = "resourcepacks";
                }

                var versionsResp = await _http.GetAsync($"https://api.modrinth.com/v2/project/{slug}/version");
                if (!versionsResp.IsSuccessStatusCode) return null;

                var versionsJson = await versionsResp.Content.ReadAsStringAsync();
                using var vDoc = JsonDocument.Parse(versionsJson);
                var vRoot = vDoc.RootElement;
                if (vRoot.ValueKind != JsonValueKind.Array || vRoot.GetArrayLength() == 0) return null;

                JsonElement? chosenVersion = null;
                foreach (var v in vRoot.EnumerateArray())
                {
                    bool matchGame = false;
                    bool matchLoader = false;

                    if (v.TryGetProperty("game_versions", out var gvProp) && gvProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var gv in gvProp.EnumerateArray())
                        {
                            var verStr = gv.GetString();
                            if (verStr == "1.21.11" || verStr == "1.21.1" || verStr == "1.21")
                            {
                                matchGame = true;
                                break;
                            }
                        }
                    }

                    if (v.TryGetProperty("loaders", out var lProp) && lProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var l in lProp.EnumerateArray())
                        {
                            var lStr = l.GetString();
                            if (string.Equals(lStr, "fabric", StringComparison.OrdinalIgnoreCase))
                            {
                                matchLoader = true;
                                break;
                            }
                        }
                    }

                    if (matchGame && (matchLoader || projType != "mod"))
                    {
                        chosenVersion = v;
                        break;
                    }
                }

                if (!chosenVersion.HasValue)
                {
                    chosenVersion = vRoot[0];
                }

                var ver = chosenVersion.Value;
                if (!ver.TryGetProperty("files", out var filesProp) || filesProp.ValueKind != JsonValueKind.Array || filesProp.GetArrayLength() == 0)
                {
                    return null;
                }

                JsonElement chosenFile = filesProp[0];
                foreach (var f in filesProp.EnumerateArray())
                {
                    if (f.TryGetProperty("primary", out var prim) && prim.GetBoolean())
                    {
                        chosenFile = f;
                        break;
                    }
                }

                var downloadUrl = chosenFile.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                var fileName = chosenFile.TryGetProperty("filename", out var fn) ? fn.GetString() ?? $"{slug}.jar" : $"{slug}.jar";
                var size = chosenFile.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0L;

                string sha256 = "";
                if (chosenFile.TryGetProperty("hashes", out var hashesProp))
                {
                    if (hashesProp.TryGetProperty("sha256", out var h256))
                    {
                        sha256 = h256.GetString() ?? "";
                    }
                }

                if (string.IsNullOrEmpty(sha256) && !string.IsNullOrEmpty(downloadUrl))
                {
                    try
                    {
                        using var fileResp = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                        if (fileResp.IsSuccessStatusCode)
                        {
                            using var stream = await fileResp.Content.ReadAsStreamAsync();
                            using var sha = SHA256.Create();
                            var hashBytes = await sha.ComputeHashAsync(stream);
                            sha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();
                            if (size <= 0 && fileResp.Content.Headers.ContentLength.HasValue)
                            {
                                size = fileResp.Content.Headers.ContentLength.Value;
                            }
                        }
                    }
                    catch
                    {
                        if (chosenFile.TryGetProperty("hashes", out var fallbackHashes) && fallbackHashes.TryGetProperty("sha1", out var hSha1))
                        {
                            sha256 = hSha1.GetString() ?? "";
                        }
                    }
                }

                return new ModrinthProjectInfo
                {
                    Slug = slug,
                    Title = title,
                    Description = desc,
                    IconUrl = iconUrl,
                    ProjectType = projType,
                    Category = category,
                    FileName = fileName,
                    DownloadUrl = downloadUrl,
                    FileSizeBytes = size,
                    Sha256 = sha256,
                    RelativePath = $"{subFolder}/{fileName}"
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
