using Microsoft.AspNetCore.Identity;
using System.Configuration;

namespace VyacheslavsMegaWebApp.Data.Entities.Identity
{
    public class VmsIdentityUser : IdentityUser
    {
        public VmsIdentityUser() : base() { }

        public DateTime? EmailConfirmedCodeCreationTime { get; set; }

        public bool HasMinecraftLicense { get; set; }
        public string? MicrosoftGamertag { get; set; }
        public string? MicrosoftUuid { get; set; }
        public string? OwnedMojangCapesJson { get; set; }
        public DateTime? MicrosoftLicenseLinkedAt { get; set; }
    }
}
