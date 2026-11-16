using System;

namespace VyacheslavsMegaWebApp.Models
{
    public class LauncherSessionInfo
    {
        public string SessionKey { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsActive => ExpiresAt > DateTime.UtcNow;
    }
}
