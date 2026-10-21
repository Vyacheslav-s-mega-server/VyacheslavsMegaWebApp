using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration;

namespace VyacheslavsMegaWebApp.Data
{
    public class AppDbContext : IdentityDbContext<VmsIdentityUser>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public AppDbContext() : base()
        {
        }

        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactLink> ContactLinks { get; set; }
        public DbSet<MainPageData> MainPageData { get; set; }
        public DbSet<UserReport> UserReports { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<PartnerLink> PartnerLinks { get; set; }
        public DbSet<DonationCard> DonationCards { get; set; }
        public DbSet<ServerMediaLink> ServerMediaLinks { get; set; }
        public DbSet<FundTransaction> FundTransactions { get; set; }
        public DbSet<FundSnapshot> FundSnapshots { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<DownloadButton> DownloadButtons { get; set; }
        public DbSet<TransactionPurpose> TransactionPurposes { get; set; }
        public DbSet<LauncherSession> LauncherSessions { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<ClientBuildFile> ClientBuildFiles { get; set; }
        public DbSet<CustomCape> CustomCapes { get; set; }
        public DbSet<CapeRedeemCode> CapeRedeemCodes { get; set; }
        public DbSet<UserCapeOwnership> UserCapeOwnerships { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(ProjectConfig.ConnectionString);
            optionsBuilder.UseLazyLoadingProxies(true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region Identity config

            modelBuilder.Entity<VmsIdentityUser>().HasData(new VmsIdentityUser()
            {
                Id = "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                UserName = "Evray",
                NormalizedUserName = "EVRAY",
                PhoneNumber = "+7 (705) 806 3101",
                Email = "vya4slav4617@gmail.com",
                NormalizedEmail = "VYA4SLAV4617@GMAIL.COM",
                EmailConfirmed = true,
                PasswordHash = new PasswordHasher<VmsIdentityUser>().HashPassword(null, "admin")
            });

            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole()
            {
                Id = "db158bac-48d4-47c3-a6b0-c6d8b5c9ee55",
                Name = "Admin",
                NormalizedName = "ADMIN"
            });

            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole()
            {
                Id = "03d4ba1e-afef-4e77-849d-295bd86148db",
                Name = "Moderator",
                NormalizedName = "MODERATOR"
            });

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>()
            {
                RoleId = "db158bac-48d4-47c3-a6b0-c6d8b5c9ee55",
                UserId = "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303"
            });

            #endregion

            modelBuilder.Entity<ContactLink>().HasData(new ContactLink()
            {
                Id = 1,
                Url = "https://vk.com/sansei_1",
                Content = "Вячеслав Костырев",
                Description = "VK: ",
                ContactId = 1,
            });

            modelBuilder.Entity<ContactLink>().HasData(new ContactLink()
            {
                Id = 2,
                Url = "https://t.me/vyacheslav_minecraft",
                Content = "@vyacheslav_minecraft",
                Description = "Telegram: ",
                ContactId = 1,
            });

            modelBuilder.Entity<Contact>().HasData(new Contact()
            {
                Id = 1,
                DisplayName = "Вячеслав",
                Description = "Царь и бог этого сервера",
                DescriptionLarge = "",
                UserId = "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303",
                ShowContact = true,
            });

            modelBuilder.Entity<MainPageData>().HasData(new MainPageData()
            {
                Id = 1,
                PageTitle = "V-Server",
                ErrorMessage = "",
                ShowErrorMessage = false,
                Title = "Открой для себя мир Minecraft с\r\nVyacheslav's mega server",
                YellowHint = "Теперь с модами",
                ServerAddress = "109.248.157.54:25565",
                ServerVersion = "1.21.11",
                Description = "На сервере установлена сборка интересных модов. Этот модпак превращает Minecraft в сложное и увлекательное приключение с множеством новых возможностей.\r\nПогружайтесь в новые приключения с:\r\n\r\nУлучшенным ИИ мобов и механикой боя\r\nОбширными новыми биомами для исследования\r\nРазнообразными и опасными новыми мобами\r\nПродвинутой магией и зачарованиями\r\nИ многим другим",
                MetatagDescription = "Мега сервер Вячеслава",
                MetatagKeywords = "Minecraft, server, Вячеслав, мега, сервер",
            });

            modelBuilder.Entity<TransactionPurpose>().HasData(new TransactionPurpose()
            {
                Id = 1,
                Name = "Пожертвование",
                Type = TransactionPurpose.TransactionType.Income
            });

            modelBuilder.Entity<TransactionPurpose>().HasData(new TransactionPurpose()
            {
                Id = 2,
                Name = "Покупка комплектующих сервера",
                Type = TransactionPurpose.TransactionType.Expense
            });

            modelBuilder.Entity<Friendship>().ToTable("Friendships");
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Requester)
                .WithMany()
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Addressee)
                .WithMany()
                .HasForeignKey(f => f.AddresseeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LauncherSession>(b =>
            {
                b.HasKey(s => s.SessionKey);
                b.Ignore(s => s.SessionKeyHash);
            });

            modelBuilder.Entity<CustomCape>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            modelBuilder.Entity<CapeRedeemCode>()
                .HasIndex(r => r.Code)
                .IsUnique();

            modelBuilder.Entity<UserCapeOwnership>()
                .HasIndex(o => new { o.UserId, o.CapeId })
                .IsUnique();
        }
    }
}
