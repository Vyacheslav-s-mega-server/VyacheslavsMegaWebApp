using System.Collections.Generic;

namespace VyacheslavsMegaWebApp.Models
{
    public class MinecraftLicenseResult
    {
        public bool Success { get; set; } = true;
        public bool HasLicense { get; set; }
        public bool HasMinecraftLicense => HasLicense;
        public string? ErrorMessage { get; set; }
        public string Gamertag { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
        public List<MojangCapeInfo> Capes { get; set; } = new();
        public string? SkinUrl { get; set; }
        public string? SkinModel { get; set; }
    }
}
