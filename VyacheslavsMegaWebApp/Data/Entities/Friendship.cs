using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VyacheslavsMegaWebApp.Data.Entities.Base;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class Friendship : EntityBase
    {
        [Required]
        public string RequesterId { get; set; } = string.Empty;

        [Required]
        public string AddresseeId { get; set; } = string.Empty;

        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(RequesterId))]
        [ValidateNever]
        public virtual VmsIdentityUser? Requester { get; set; }

        [ForeignKey(nameof(AddresseeId))]
        [ValidateNever]
        public virtual VmsIdentityUser? Addressee { get; set; }
    }
}
