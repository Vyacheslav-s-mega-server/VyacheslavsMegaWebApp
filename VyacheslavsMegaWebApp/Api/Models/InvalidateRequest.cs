using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record InvalidateRequest(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("clientToken")] string? ClientToken = null
    );
}
