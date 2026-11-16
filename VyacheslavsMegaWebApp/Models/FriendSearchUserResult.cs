namespace VyacheslavsMegaWebApp.Models
{
    public class FriendSearchUserResult
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string MinecraftUsername { get; set; } = string.Empty;
        public string SkinUrl { get; set; } = string.Empty;
        public string FriendshipStatus { get; set; } = "None"; // "None", "PendingIncoming", "PendingOutgoing", "Friends"
        public int? FriendshipId { get; set; }
    }
}
