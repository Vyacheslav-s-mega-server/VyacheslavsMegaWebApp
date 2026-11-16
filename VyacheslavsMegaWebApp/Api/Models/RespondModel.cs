namespace VyacheslavsMegaWebApp.Api.Models
{
    public class RespondModel
    {
        public int FriendshipId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool Accept { get; set; }
    }
}
