using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Service
{
    public sealed class DeviceCodeInitResult
    {
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = string.Empty;
        [JsonPropertyName("user_code")] public string UserCode { get; set; } = string.Empty;
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; } = 5;
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    public sealed class MinecraftLicenseService
    {
        private const string ClientId = "00000000441cc96b";
        private const string Scope = "service::user.auth.xboxlive.com::MBI_SSL";
        private const string DeviceCodeEndpoint = "https://login.live.com/oauth20_connect.srf";
        private const string TokenEndpoint = "https://login.live.com/oauth20_token.srf";
        private const string XblAuthUrl = "https://user.auth.xboxlive.com/user/authenticate";
        private const string XstsAuthUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
        private const string McLoginUrl = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private const string McProfileUrl = "https://api.minecraftservices.com/minecraft/profile";
        private const string McEntitlementsUrl = "https://api.minecraftservices.com/entitlements/mcstore";

        private readonly HttpClient _http;
        private readonly IWebHostEnvironment _env;

        public MinecraftLicenseService(HttpClient http, IWebHostEnvironment env)
        {
            _http = http;
            _env = env;
        }

        public async Task<DeviceCodeInitResult> RequestDeviceCodeAsync(CancellationToken ct = default)
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"] = Scope,
                ["response_type"] = "device_code"
            });

            using var resp = await _http.PostAsync(DeviceCodeEndpoint, form, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Ошибка запроса кода устройства ({resp.StatusCode}): {body}");
            }

            return JsonSerializer.Deserialize<DeviceCodeInitResult>(body)
                   ?? throw new InvalidOperationException("Пустой ответ от сервера авторизации.");
        }

        public async Task<MinecraftLicenseResult?> CheckDeviceCodeAsync(string deviceCode, CancellationToken ct = default)
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["client_id"] = ClientId,
                ["device_code"] = deviceCode
            });

            using var resp = await _http.PostAsync(TokenEndpoint, form, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[MS-OAUTH] Poll response: status={resp.StatusCode}, body={body}");

            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                var token = doc.RootElement.GetProperty("access_token").GetString()
                            ?? throw new InvalidOperationException("access_token отсутствует в ответе.");
                return await CompleteLicenseVerificationAsync(token, ct);
            }

            using var errDoc = JsonDocument.Parse(body);
            var err = errDoc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : null;
            var errDesc = errDoc.RootElement.TryGetProperty("error_description", out var ed) ? ed.GetString() : null;

            if (err == "authorization_pending" || err == "slow_down")
            {
                return null;
            }

            if (err == "expired_token") throw new InvalidOperationException("Срок действия одноразового кода истёк. Закройте окно и нажмите привязку снова.");
            if (err == "authorization_declined") throw new InvalidOperationException("Авторизация отклонена пользователем.");

            var details = !string.IsNullOrWhiteSpace(errDesc) ? $" — {errDesc}" : "";
            throw new InvalidOperationException($"Ошибка авторизации Microsoft: {err}{details}");
        }

        public async Task<MinecraftLicenseResult> CompleteLicenseVerificationAsync(string msAccessToken, CancellationToken ct = default)
        {
            var prefixes = new[] { "", "d=", "t=" };
            string? xblToken = null;
            string? userHash = null;
            string lastError = "";

            foreach (var prefix in prefixes)
            {
                var ticket = prefix + msAccessToken;
                var xblPayload = new
                {
                    Properties = new
                    {
                        AuthMethod = "RPS",
                        SiteName = "user.auth.xboxlive.com",
                        RpsTicket = ticket
                    },
                    RelyingParty = "http://auth.xboxlive.com",
                    TokenType = "JWT"
                };

                using var xblReq = new HttpRequestMessage(HttpMethod.Post, XblAuthUrl)
                {
                    Content = new StringContent(JsonSerializer.Serialize(xblPayload), Encoding.UTF8, "application/json")
                };
                xblReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                xblReq.Headers.TryAddWithoutValidation("x-xbl-contract-version", "1");

                using var xblResp = await _http.SendAsync(xblReq, ct);
                var xblBody = await xblResp.Content.ReadAsStringAsync(ct);
                if (xblResp.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(xblBody))
                {
                    try
                    {
                        using var xblDoc = JsonDocument.Parse(xblBody);
                        xblToken = xblDoc.RootElement.GetProperty("Token").GetString();
                        userHash = xblDoc.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString();
                        if (!string.IsNullOrEmpty(xblToken) && !string.IsNullOrEmpty(userHash))
                        {
                            break;
                        }
                    }
                    catch { }
                }
                lastError = $"HTTP {(int)xblResp.StatusCode} {xblBody}".Trim();
            }

            if (string.IsNullOrEmpty(xblToken) || string.IsNullOrEmpty(userHash))
            {
                throw new InvalidOperationException($"Вход в Xbox Live отклонён: {lastError}");
            }

            var xstsPayload = new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { xblToken }
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            };

            using var xstsReq = new HttpRequestMessage(HttpMethod.Post, XstsAuthUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(xstsPayload), Encoding.UTF8, "application/json")
            };
            xstsReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            xstsReq.Headers.TryAddWithoutValidation("x-xbl-contract-version", "1");

            using var xstsResp = await _http.SendAsync(xstsReq, ct);
            var xstsBody = await xstsResp.Content.ReadAsStringAsync(ct);
            if (!xstsResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Получение XSTS токена отклонено: {xstsBody}");
            }

            using var xstsDoc = JsonDocument.Parse(xstsBody);
            var xstsToken = xstsDoc.RootElement.GetProperty("Token").GetString()!;

            var mcLoginPayload = new
            {
                identityToken = $"XBL3.0 x={userHash};{xstsToken}"
            };

            using var mcReq = new HttpRequestMessage(HttpMethod.Post, McLoginUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(mcLoginPayload), Encoding.UTF8, "application/json")
            };

            using var mcResp = await _http.SendAsync(mcReq, ct);
            var mcBody = await mcResp.Content.ReadAsStringAsync(ct);
            if (!mcResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Вход в службы Minecraft не удался: {mcBody}");
            }

            using var mcDoc = JsonDocument.Parse(mcBody);
            var mcToken = mcDoc.RootElement.GetProperty("access_token").GetString()!;

            using var entReq = new HttpRequestMessage(HttpMethod.Get, McEntitlementsUrl);
            entReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            using var entResp = await _http.SendAsync(entReq, ct);
            var entBody = await entResp.Content.ReadAsStringAsync(ct);

            bool ownsGame = false;
            if (entResp.IsSuccessStatusCode)
            {
                using var entDoc = JsonDocument.Parse(entBody);
                if (entDoc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    foreach (var itm in items.EnumerateArray())
                    {
                        var name = itm.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (name == "product_minecraft" || name == "game_minecraft")
                        {
                            ownsGame = true;
                            break;
                        }
                    }
                }
            }

            if (!ownsGame)
            {
                throw new InvalidOperationException("На этом аккаунте Microsoft не обнаружена купленная лицензия Minecraft (Java Edition).");
            }

            using var profReq = new HttpRequestMessage(HttpMethod.Get, McProfileUrl);
            profReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mcToken);
            using var profResp = await _http.SendAsync(profReq, ct);
            var profBody = await profResp.Content.ReadAsStringAsync(ct);
            if (!profResp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Профиль игрока Minecraft не найден.");
            }

            using var profDoc = JsonDocument.Parse(profBody);
            var mcId = profDoc.RootElement.GetProperty("id").GetString()!;
            var mcName = profDoc.RootElement.GetProperty("name").GetString()!;

            string? skinUrl = null;
            string? skinModel = "classic";
            if (profDoc.RootElement.TryGetProperty("skins", out var skins) && skins.GetArrayLength() > 0)
            {
                var activeSkin = skins.EnumerateArray().FirstOrDefault(s => s.TryGetProperty("state", out var st) && st.GetString() == "ACTIVE");
                if (activeSkin.ValueKind != JsonValueKind.Undefined)
                {
                    skinUrl = activeSkin.TryGetProperty("url", out var u) ? u.GetString() : null;
                    skinModel = activeSkin.TryGetProperty("variant", out var v) && v.GetString() == "SLIM" ? "slim" : "classic";
                }
            }

            var capesList = new List<MojangCapeInfo>();
            if (profDoc.RootElement.TryGetProperty("capes", out var capes) && capes.GetArrayLength() > 0)
            {
                foreach (var c in capes.EnumerateArray())
                {
                    var capeId = c.TryGetProperty("id", out var cid) ? cid.GetString() : "";
                    var alias = c.TryGetProperty("alias", out var ca) ? ca.GetString() : "";
                    var curl = c.TryGetProperty("url", out var cu) ? cu.GetString() : "";
                    var state = c.TryGetProperty("state", out var cs) ? cs.GetString() : "";
                    if (!string.IsNullOrEmpty(capeId))
                    {
                        var info = new MojangCapeInfo
                        {
                            Id = capeId,
                            Alias = alias ?? "",
                            Url = curl ?? "",
                            DisplayName = FormatCapeDisplayName(alias, capeId),
                            IsActive = state == "ACTIVE"
                        };
                        capesList.Add(info);

                        if (!string.IsNullOrEmpty(curl))
                        {
                            _ = CacheMojangCapeFileAsync(capeId, curl, ct);
                        }
                    }
                }
            }

            return new MinecraftLicenseResult
            {
                HasLicense = true,
                Gamertag = mcName,
                Uuid = mcId,
                Capes = capesList,
                SkinUrl = skinUrl,
                SkinModel = skinModel
            };
        }

        private async Task CacheMojangCapeFileAsync(string capeId, string url, CancellationToken ct)
        {
            try
            {
                var targetDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "img", "capes", "mojang");
                Directory.CreateDirectory(targetDir);
                var targetFile = Path.Combine(targetDir, $"{capeId}.png");

                if (!File.Exists(targetFile))
                {
                    var bytes = await _http.GetByteArrayAsync(url, ct);
                    if (bytes != null && bytes.Length > 0)
                    {
                        await File.WriteAllBytesAsync(targetFile, bytes, ct);
                    }
                }
            }
            catch { }
        }

        private static string FormatCapeDisplayName(string? alias, string id)
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                return alias switch
                {
                    "Migrator" => "Плащ миграции (Migrator)",
                    "Vanilla" => "Ванильный плащ (Vanilla)",
                    "15thAnniversary" => "Плащ 15-летия (15th Anniversary)",
                    "Cherry" => "Цветущая вишня (Cherry Blossom)",
                    "Twitch" => "Twitch плащ",
                    "TikTok" => "TikTok плащ",
                    "Founder" => "Плащ основателя (Founder's Cape)",
                    "Pancake" => "Блинный плащ (Pancakes)",
                    _ => alias
                };
            }
            return "Плащ Mojang";
        }

        public async Task<byte[]?> DownloadSkinBytesAsync(string url, CancellationToken ct = default)
        {
            try
            {
                return await _http.GetByteArrayAsync(url, ct);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(byte[]? skinBytes, string model, string? skinUrl)> FetchSkinByMojangUuidAsync(string uuid, CancellationToken ct = default)
        {
            try
            {
                var cleanUuid = uuid.Replace("-", "").Trim();
                using var req = new HttpRequestMessage(HttpMethod.Get, $"https://sessionserver.mojang.com/session/minecraft/profile/{cleanUuid}");
                using var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode) return (null, "classic", null);

                var body = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("properties", out var props))
                {
                    foreach (var prop in props.EnumerateArray())
                    {
                        var name = prop.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (name == "textures")
                        {
                            var b64 = prop.TryGetProperty("value", out var v) ? v.GetString() : null;
                            if (!string.IsNullOrEmpty(b64))
                            {
                                var jsonStr = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                                using var texDoc = JsonDocument.Parse(jsonStr);
                                if (texDoc.RootElement.TryGetProperty("textures", out var texs) &&
                                    texs.TryGetProperty("SKIN", out var skinObj))
                                {
                                    var skinUrl = skinObj.TryGetProperty("url", out var su) ? su.GetString() : null;
                                    var model = "classic";
                                    if (skinObj.TryGetProperty("metadata", out var meta) &&
                                        meta.TryGetProperty("model", out var m) &&
                                        m.GetString() == "slim")
                                    {
                                        model = "slim";
                                    }

                                    if (!string.IsNullOrEmpty(skinUrl))
                                    {
                                        var bytes = await _http.GetByteArrayAsync(skinUrl, ct);
                                        return (bytes, model, skinUrl);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOJANG-SKIN] Error fetching skin for {uuid}: {ex.Message}");
            }

            return (null, "classic", null);
        }
    }
}
