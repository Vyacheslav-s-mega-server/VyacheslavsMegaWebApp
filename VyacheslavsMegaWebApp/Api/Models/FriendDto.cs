using System;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public class FriendDto
    {
        public int FriendshipId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string Status { get; set; } = "Accepted";
        public DateTime CreatedAt { get; set; }
    }
}
