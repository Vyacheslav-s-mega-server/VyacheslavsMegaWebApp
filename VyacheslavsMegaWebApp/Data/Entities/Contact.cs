using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using VyacheslavsMegaWebApp.Data.Entities.Base;
using VyacheslavsMegaWebApp.Data.Entities.Identity;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class Contact : EntityBase
    {
        [MaxLength(50)]
        [Display(Name = "Отображаемое имя")]
        public string DisplayName { get; set; }

        [Display(Name = "Описание короткое")]
        public string Description { get; set; }

        [Display(Name = "Описание подробное")]
        public string DescriptionLarge { get; set; }

        [Display(Name = "Показать в списке контактов")]
        public bool ShowContact { get; set; }

        #region Navigations

        [Display(Name = "Пользователь")]
        public string? UserId { get; set; }

        [ValidateNever]
        public virtual VmsIdentityUser? User { get; set; }

        [ValidateNever]
        public virtual ICollection<ContactLink> Links { get; set; } = new List<ContactLink>();

        #endregion
    }
}
