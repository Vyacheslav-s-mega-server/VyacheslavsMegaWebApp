using System;

namespace VyacheslavsMegaWebApp.Areas.Admin.Models
{
    public class UserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsAdmin { get; set; }
        public bool LockoutEnabled { get; set; }
        public bool IsLockedOut { get; set; }
        public string? LastHwid { get; set; }
        public DateTime? LastSessionDate { get; set; }
        public bool HasSkin { get; set; }
        public bool HasCape { get; set; }
        public string? SkinUrl { get; set; }
        public string? CapeUrl { get; set; }
        public string? SkinModel { get; set; }
    }
}
