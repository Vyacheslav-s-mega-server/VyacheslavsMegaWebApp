using System.ComponentModel.DataAnnotations;
using VyacheslavsMegaWebApp.Models;

namespace VyacheslavsMegaWebApp.Data.Entities
{
    public class MainPageData : Base.PageData
    {
        [MaxLength(120)]
        [Display(Name = "Заголовок (строка 1)")]
        public string? Title { get; set; } = "ОТКРОЙ СВОЙ";

        [MaxLength(120)]
        [Display(Name = "Акцентный заголовок (строка 2, жёлтый)")]
        public string? TitleAccent { get; set; } = "ИДЕАЛЬНЫЙ МИР";

        [Display(Name = "Подзаголовок первого экрана")]
        public string? Subtitle { get; set; }

        [Display(Name = "Адрес сервера (IP:Port)")]
        public string? ServerAddress { get; set; }

        [Display(Name = "Версия сервера")]
        public string? ServerVersion { get; set; }

        [MaxLength(100)]
        public string? YellowHint { get; set; }

        [MaxLength(4000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? ErrorMessage { get; set; }

        public bool ShowErrorMessage { get; set; }

        [MaxLength(100)]
        [Display(Name = "Бейдж блока «О проекте»")]
        public string? AboutTag { get; set; } = "О ПРОЕКТЕ VEC";

        [MaxLength(200)]
        [Display(Name = "Заголовок блока «О проекте»")]
        public string? AboutTitle { get; set; } = "Сервер, сделанный для людей";

        [Display(Name = "Текст о сервере и колледже")]
        public string? AboutText { get; set; }

        [MaxLength(150)]
        [Display(Name = "Карточка 1: Заголовок")]
        public string? Feature1Title { get; set; } = "Собственный лаунчер VEC";

        [Display(Name = "Карточка 1: Описание")]
        public string? Feature1Desc { get; set; }

        [MaxLength(150)]
        [Display(Name = "Карточка 2: Заголовок")]
        public string? Feature2Title { get; set; } = "Честная игра без донат-мусора";

        [Display(Name = "Карточка 2: Описание")]
        public string? Feature2Desc { get; set; }

        [MaxLength(150)]
        [Display(Name = "Карточка 3: Заголовок")]
        public string? Feature3Title { get; set; } = "Стабильность и защита";

        [Display(Name = "Карточка 3: Описание")]
        public string? Feature3Desc { get; set; }
    }
}
