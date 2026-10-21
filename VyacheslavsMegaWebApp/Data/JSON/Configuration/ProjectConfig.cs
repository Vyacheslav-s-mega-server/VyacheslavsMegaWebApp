using System.Collections.Generic;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.EmailService;
using VyacheslavsMegaWebApp.Data.JSON.Configuration.MinecraftServer;

namespace VyacheslavsMegaWebApp.Data.JSON.Configuration
{
    public class ProjectConfig
    {
        public static string ConnectionString { get; set; }
        public static MinecraftServerData MinecraftServerData { get; set; }
        public static EmailServiceConfig EmailServiceConfig { get; set; }
        public static List<string> AcceptedEmailDomains { get; set; }
        public static string? PublicUrl { get; set; } = "https://vec.kpvk.edu.kz";
        public static List<string>? SkinDomains { get; set; } = new() { "vec.kpvk.edu.kz", "95.59.233.227", "109.248.157.54", "localhost", "127.0.0.1" };
        public static bool EnforceLaunchTicket { get; set; } = true;
    }
}
