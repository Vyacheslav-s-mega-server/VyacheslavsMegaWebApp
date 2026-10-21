using System;
using System.Collections.Generic;

namespace VyacheslavsMegaWebApp.Models
{
    public class AccountSettingsViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string RoleBadge { get; set; } = "Игрок VMS";

        public ChangePasswordViewModel ChangePassword { get; set; } = new();
        public List<LauncherSessionInfo> LauncherSessions { get; set; } = new();
    }
}
