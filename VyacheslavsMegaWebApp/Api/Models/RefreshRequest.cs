using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record RefreshRequest(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("clientToken")] string? ClientToken = null,
        [property: JsonPropertyName("requestUser")] bool RequestUser = true
    );
}
