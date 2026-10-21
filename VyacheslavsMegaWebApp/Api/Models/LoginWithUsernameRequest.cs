namespace VyacheslavsMegaWebApp.Api.Models
{
    public class LoginWithUsernameRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ClientToken { get; set; }
        public string? Hwid { get; set; }
    }
}
