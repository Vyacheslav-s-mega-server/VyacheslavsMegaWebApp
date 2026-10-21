using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VyacheslavsMegaWebApp.Data.Entities.Base;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class CapeRedeemCode : EntityBase
    {
        public int CapeId { get; set; }

        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        [MaxLength(255)]
        public string? RedeemedByUserId { get; set; }

        public DateTime? RedeemedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CapeId))]
        [ValidateNever]
        public virtual CustomCape? Cape { get; set; }

        [ForeignKey(nameof(RedeemedByUserId))]
        [ValidateNever]
        public virtual VmsIdentityUser? RedeemedByUser { get; set; }
    }
}
