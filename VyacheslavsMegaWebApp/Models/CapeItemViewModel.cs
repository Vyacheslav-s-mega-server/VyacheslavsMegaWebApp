namespace VyacheslavsMegaWebApp.Models
{
    public class CapeItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public bool IsEquipped { get; set; }
        public bool IsOfficial { get; set; }
    }
}
