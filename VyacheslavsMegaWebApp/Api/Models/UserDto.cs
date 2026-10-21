using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record UserDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("minecraftUsername")] string? MinecraftUsername = null,
        [property: JsonPropertyName("skinModel")] string SkinModel = "classic",
        [property: JsonPropertyName("skinUrl")] string? SkinUrl = null,
        [property: JsonPropertyName("capeUrl")] string? CapeUrl = null
    );
}
