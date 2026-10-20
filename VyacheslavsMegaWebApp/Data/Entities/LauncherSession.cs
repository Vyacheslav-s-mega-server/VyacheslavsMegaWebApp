using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class LauncherSession : Base.EntityGuid
    {
        //public string SessionKey { get; set; } = string.Empty;
        public required byte[] SessionKeyHash { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string? ClientToken { get; set; }
        public string? ServerId { get; set; }

        [ValidateNever]
        public virtual VmsIdentityUser User { get; set; } = null!;
    }
}
