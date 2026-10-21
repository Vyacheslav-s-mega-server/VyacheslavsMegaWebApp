namespace VyacheslavsMegaWebApp.Api.Models
{
    public class ManifestFileEntry
    {
        public string Path { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool IsRequired { get; set; } = true;
        public string DownloadUrl { get; set; } = string.Empty;
        public string Category { get; set; } = "Core";
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? ModrinthSlug { get; set; }
        public string? Source { get; set; }
    }
}
