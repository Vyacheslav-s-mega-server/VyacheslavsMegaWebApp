using System;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public class FriendRequestDto
    {
        public int FriendshipId { get; set; }
        public string RequesterId { get; set; } = string.Empty;
        public string RequesterUsername { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
