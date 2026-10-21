using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record ProfileDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name
    );
}
