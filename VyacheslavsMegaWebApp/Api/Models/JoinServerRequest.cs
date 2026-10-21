using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record JoinServerRequest(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("selectedProfile")] string SelectedProfile,
        [property: JsonPropertyName("serverId")] string ServerId
    );
}
