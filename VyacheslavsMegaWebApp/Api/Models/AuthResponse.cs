using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record AuthResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("clientToken")] string ClientToken,
        [property: JsonPropertyName("selectedProfile")] ProfileDto SelectedProfile,
        [property: JsonPropertyName("availableProfiles")] List<ProfileDto> AvailableProfiles,
        [property: JsonPropertyName("user")] UserDto User,
        [property: JsonPropertyName("skinUrl")] string? SkinUrl = null
    );
}
