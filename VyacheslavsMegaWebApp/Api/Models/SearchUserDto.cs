namespace VyacheslavsMegaWebApp.Api.Models
{
    public class SearchUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsFriend { get; set; }
        public bool IsPending { get; set; }
    }
}
