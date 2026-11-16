using System.Collections.Generic;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public class FriendsListResponse
    {
        public List<FriendDto> Friends { get; set; } = new();
        public List<FriendRequestDto> IncomingRequests { get; set; } = new();
        public List<FriendRequestDto> OutgoingRequests { get; set; } = new();
    }
}
