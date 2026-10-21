using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record AuthenticateRequest(
        [property: JsonPropertyName("username")] string? Username,
        [property: JsonPropertyName("password")] string? Password,
        [property: JsonPropertyName("clientToken")] string? ClientToken = null,
        [property: JsonPropertyName("requestUser")] bool RequestUser = true
    );
}
