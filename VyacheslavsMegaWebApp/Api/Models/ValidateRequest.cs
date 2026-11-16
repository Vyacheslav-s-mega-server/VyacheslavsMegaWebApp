using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record ValidateRequest(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("clientToken")] string? ClientToken = null
    );
}
