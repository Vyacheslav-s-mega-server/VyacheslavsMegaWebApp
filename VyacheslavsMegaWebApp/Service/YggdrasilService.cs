using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using VyacheslavsMegaWebApp.Api.Models;
using VyacheslavsMegaWebApp.Data.JSON.Configuration;

namespace VyacheslavsMegaWebApp.Service
{
    public sealed class YggdrasilService
    {
        private readonly RSA _rsa;
        private readonly string _serverPublicDomain;
        private readonly string _keyFilePath;
        private readonly SkinStorageService _skinStorage;

        public YggdrasilService(IWebHostEnvironment env, SkinStorageService skinStorage)
        {
            _skinStorage = skinStorage;
            _serverPublicDomain = ProjectConfig.PublicUrl ?? "http://localhost:5044";
            _keyFilePath = Path.Combine(env.ContentRootPath, "App_Data", "yggdrasil_key.pem");
            _rsa = LoadOrGenerateRsaKey();
        }

        private RSA LoadOrGenerateRsaKey()
        {
            RSA rsa = RSA.Create(2048);
            try
            {
                string? dir = Path.GetDirectoryName(_keyFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(_keyFilePath))
                {
                    string pem = File.ReadAllText(_keyFilePath);
                    rsa.ImportFromPem(pem);
                }
                else
                {
                    string pem = rsa.ExportPkcs8PrivateKeyPem();
                    File.WriteAllText(_keyFilePath, pem);
                }
            }
            catch
            {
            }

            return rsa;
        }

        public string GetPublicKeyPem()
        {
            return _rsa.ExportSubjectPublicKeyInfoPem();
        }

        public PropertyDto BuildTexturesProperty(string username, string userId, string? baseUrl = null)
        {
            string domain = (baseUrl ?? _serverPublicDomain).TrimEnd('/');
            Dictionary<string, object> textures = new Dictionary<string, object>();
            string model = _skinStorage.GetSkinModel(username);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string? skinHash = _skinStorage.GetSkinHash(username);
            string skinUrl = !string.IsNullOrEmpty(skinHash)
                ? $"{domain}/textures/{skinHash}"
                : $"{domain}/api/skin/{Uri.EscapeDataString(username)}.png?v={timestamp}";

            Dictionary<string, object> skinMeta = new Dictionary<string, object>
            {
                ["url"] = skinUrl
            };

            if (string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase))
            {
                skinMeta["metadata"] = new { model = "slim" };
            }

            textures["SKIN"] = skinMeta;

            if (_skinStorage.HasCape(username))
            {
                string? capeHash = _skinStorage.GetCapeHash(username);
                string capeUrl = !string.IsNullOrEmpty(capeHash)
                    ? $"{domain}/textures/{capeHash}"
                    : $"{domain}/api/cape/{Uri.EscapeDataString(username)}.png?v={timestamp}";

                textures["CAPE"] = new
                {
                    url = capeUrl
                };
            }

            string cleanId = userId.Replace("-", "");
            object payloadObj = new
            {
                timestamp = timestamp,
                profileId = cleanId,
                profileName = username,
                signatureRequired = true,
                textures = textures
            };

            string payloadJson = JsonSerializer.Serialize(payloadObj);
            string payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

            byte[] signatureBytes = _rsa.SignData(Encoding.UTF8.GetBytes(payloadBase64), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
            string signatureBase64 = Convert.ToBase64String(signatureBytes);

            return new PropertyDto("textures", payloadBase64, signatureBase64);
        }

        public static string GenerateOfflineUuid(string username)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + username));
            hash[6] = (byte)((hash[6] & 0x0f) | 0x30);
            hash[8] = (byte)((hash[8] & 0x3f) | 0x80);

            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
