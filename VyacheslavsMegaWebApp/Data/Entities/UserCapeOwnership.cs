using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VyacheslavsMegaWebApp.Data.Entities.Base;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class UserCapeOwnership : EntityBase
    {
        [Required]
        [MaxLength(255)]
        public string UserId { get; set; } = string.Empty;

        public int CapeId { get; set; }

        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string AcquiredVia { get; set; } = "Code";

        [ForeignKey(nameof(UserId))]
        [ValidateNever]
        public virtual VmsIdentityUser? User { get; set; }

        [ForeignKey(nameof(CapeId))]
        [ValidateNever]
        public virtual CustomCape? Cape { get; set; }
    }
}
