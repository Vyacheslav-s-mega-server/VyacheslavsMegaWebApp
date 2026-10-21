using VyacheslavsMegaWebApp.Data.Entities.Base;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class ClientBuildFile : EntityBase
    {
        public string BuildSlug { get; set; } = "vec-fabric-1-21-11";
        public string RelativePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool IsRequired { get; set; } = true;
        public string? DownloadUrl { get; set; }
        public string? Category { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? SourceType { get; set; } = "Local";
        public string? ModrinthSlug { get; set; }
    }
}
