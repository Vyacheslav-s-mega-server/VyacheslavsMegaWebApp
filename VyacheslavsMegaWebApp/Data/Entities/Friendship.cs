using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class Friendship : Base.EntityBase
    {
        public enum FriendshipStatus
        {
            Pending = 0,
            Accepted = 1,
            Rejected = 2
        }

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
