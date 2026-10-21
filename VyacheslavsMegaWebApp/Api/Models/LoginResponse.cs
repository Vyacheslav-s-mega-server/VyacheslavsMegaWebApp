using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public class LoginResponse
    {
        [JsonPropertyName("succeeded")]
        public bool Succeeded { get; set; }

        [JsonPropertyName("success")]
        public bool Success => Succeeded;

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("sessionKey")]
        public string? SessionKey { get; set; }

        [JsonPropertyName("accessToken")]
        public string? AccessToken => SessionKey;

        [JsonPropertyName("clientToken")]
        public string? ClientToken { get; set; }

        [JsonPropertyName("selectedProfile")]
        public ProfileDto? SelectedProfile { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }
    }
}
