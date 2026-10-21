using System;

namespace VyacheslavsMegaWebApp.Models
{
    public class FriendItemViewModel
    {
        public int FriendshipId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string MinecraftUsername { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string SkinUrl { get; set; } = string.Empty;
        public DateTime Since { get; set; }
        public bool IsIncoming { get; set; }
    }
}
