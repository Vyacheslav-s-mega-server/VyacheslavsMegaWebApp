namespace VyacheslavsMegaWebApp.Models
{
    public class PublicProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string MinecraftUsername { get; set; } = string.Empty;
        public string BannerUrl { get; set; } = "/img/backgrounds/bg1.webp";
        public string AvatarUrl { get; set; } = string.Empty;
        public string SkinUrl { get; set; } = string.Empty;
        public string SkinModel { get; set; } = "classic";
        public string? CapeUrl { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public int FriendsCount { get; set; } = 0;
        public bool IsSelf { get; set; }
        public string FriendshipStatus { get; set; } = "None"; // "None", "PendingIncoming", "PendingOutgoing", "Friends"
        public int? FriendshipId { get; set; }
    }
}
