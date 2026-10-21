using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VyacheslavsMegaWebApp.Data.Entities.Base;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class CustomCape : EntityBase
    {
        [Required]
        [MaxLength(64)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(128)]
        public string TextureHash { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<CapeRedeemCode> Codes { get; set; } = new List<CapeRedeemCode>();
        public virtual ICollection<UserCapeOwnership> Ownerships { get; set; } = new List<UserCapeOwnership>();
    }
}
