using System.Text.Json.Serialization;

namespace VyacheslavsMegaWebApp.Api.Models
{
    public sealed record YggdrasilMetaResponse(
        [property: JsonPropertyName("meta")] object Meta,
        [property: JsonPropertyName("skinDomains")] string[] SkinDomains,
        [property: JsonPropertyName("signaturePublickey")] string SignaturePublickey
    );
}
