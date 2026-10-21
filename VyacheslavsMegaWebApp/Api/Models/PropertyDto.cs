using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record PropertyDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("signature")] string? Signature = null
    );
}
