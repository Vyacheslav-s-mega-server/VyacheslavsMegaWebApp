using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class LauncherSession
    {
        [Key]
        [MaxLength(255)]
        public string SessionKey { get; set; } = string.Empty;

        [NotMapped]
        public byte[]? SessionKeyHash { get; set; }

        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? ClientToken { get; set; }
        public string? ServerId { get; set; }

        [ValidateNever]
        public virtual VmsIdentityUser User { get; set; } = null!;
    }
}
