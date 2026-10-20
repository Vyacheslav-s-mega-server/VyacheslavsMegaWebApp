using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using VyacheslavsMegaWebApp.Data;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration;
using VyacheslavsMegaWebApp.Data.JSON.Repositories;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Data.Entities;
using VyacheslavsMegaWebApp.Service;

internal class Program
{
    private static void Main(string[] args)
    {
        var probe = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(probe) && !File.Exists(Path.Combine(probe, "VyacheslavsMegaWebApp.csproj")))
        {
            var parent = Directory.GetParent(probe)?.FullName;
            if (parent == null || parent == probe)
            {
                break;
            }
            probe = parent;
        }

        if (string.IsNullOrEmpty(probe) || !File.Exists(Path.Combine(probe, "VyacheslavsMegaWebApp.csproj")))
        {
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "VyacheslavsMegaWebApp-dev", "VyacheslavsMegaWebApp-dev", "VyacheslavsMegaWebApp");
            if (Directory.Exists(fallback))
            {
                probe = fallback;
            }
            else
            {
                probe = Directory.GetCurrentDirectory();
            }
        }

        var webRoot = Path.Combine(probe, "wwwroot");
        if (!Directory.Exists(webRoot))
        {
            Directory.CreateDirectory(webRoot);
        }

        var options = new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = probe,
            WebRootPath = webRoot
        };

        var builder = WebApplication.CreateBuilder(options);

        ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.SetBasePath(probe)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build()
            .Bind("Project", new ProjectConfig());
       
        builder.Services.AddAuthorization(x =>
            x.AddPolicy("AdminAreaPolicy", policy => policy.RequireRole("Admin")));

        builder.Services.AddControllersWithViews(x =>
            x.Conventions.Add(new AdminAreaAutorization("Admin", "AdminAreaPolicy")));

        builder.Services.AddSingleton<System.Text.Encodings.Web.HtmlEncoder>(System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));

        builder.Services.AddDbContext<AppDbContext>();

        builder.Services.AddIdentity<VmsIdentityUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
            options.Password.RequireDigit = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<VmsIdentityErrorDescriber>()
            .AddUserValidator<VmsUserValidator>();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "auth";
            options.Cookie.HttpOnly = true;
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/denied";
        });

        builder.Services.AddLocalization(options => options.ResourcesPath = "AppResources/Resx");

        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SupportedCultures = new[]
            {
                new CultureInfo("ru-RU"),
            };
            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("ru-RU");
        });

        builder.Services.AddTransient<MainPageRepository>();
        builder.Services.AddTransient<UserReportsRepository>();
        builder.Services.AddTransient<ContactsInfoRepository>();
        builder.Services.AddTransient<PartnersInfoRepository>();
        builder.Services.AddTransient<DonationCardsRepository>();
        builder.Services.AddTransient<ServerMediaLinksRepository>();
        builder.Services.AddTransient<ImageManager>();
        builder.Services.AddTransient<MCServerStatRepository>();
        builder.Services.AddTransient<FundRepository>();
        builder.Services.AddTransient<GoalRepository>();
        builder.Services.AddTransient<DownloadButtonsRepository>();
        builder.Services.AddTransient<EmailService>();
        builder.Services.AddTransient<LauncherSessionRepository>();
        builder.Services.AddTransient<SkinStorageService>();
        builder.Services.AddSingleton<LauncherTicketService>();
        builder.Services.AddSingleton<YggdrasilService>();
        builder.Services.AddSingleton<AppResourcesManager>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try { db.Database.Migrate(); } catch { }
            try
            {
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS `Friendships` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `RequesterId` varchar(255) NOT NULL,
                        `AddresseeId` varchar(255) NOT NULL,
                        `Status` int NOT NULL DEFAULT 0,
                        `CreatedAt` datetime(6) NOT NULL,
                        `UpdatedAt` datetime(6) NULL,
                        PRIMARY KEY (`Id`),
                        KEY `IX_Friendships_RequesterId` (`RequesterId`),
                        KEY `IX_Friendships_AddresseeId` (`AddresseeId`)
                    );
                ");
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS `friendships` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `RequesterId` varchar(255) NOT NULL,
                        `AddresseeId` varchar(255) NOT NULL,
                        `Status` int NOT NULL DEFAULT 0,
                        `CreatedAt` datetime(6) NOT NULL,
                        `UpdatedAt` datetime(6) NULL,
                        PRIMARY KEY (`Id`),
                        KEY `IX_friendships_RequesterId` (`RequesterId`),
                        KEY `IX_friendships_AddresseeId` (`AddresseeId`)
                    );
                ");
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS `ClientBuildFiles` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `BuildSlug` varchar(128) NOT NULL,
                        `RelativePath` varchar(512) NOT NULL,
                        `FileName` varchar(255) NOT NULL,
                        `Sha256` varchar(64) NOT NULL,
                        `FileSizeBytes` bigint NOT NULL DEFAULT 0,
                        `IsRequired` tinyint(1) NOT NULL DEFAULT 1,
                        `DownloadUrl` varchar(1024) NULL,
                        `Category` varchar(64) NULL,
                        `DisplayName` varchar(255) NULL,
                        `Description` varchar(1024) NULL,
                        PRIMARY KEY (`Id`),
                        KEY `IX_ClientBuildFiles_BuildSlug` (`BuildSlug`)
                    );
                ");
                db.Database.ExecuteSqlRaw(@"
                    CREATE TABLE IF NOT EXISTS `clientbuildfiles` (
                        `Id` int NOT NULL AUTO_INCREMENT,
                        `BuildSlug` varchar(128) NOT NULL,
                        `RelativePath` varchar(512) NOT NULL,
                        `FileName` varchar(255) NOT NULL,
                        `Sha256` varchar(64) NOT NULL,
                        `FileSizeBytes` bigint NOT NULL DEFAULT 0,
                        `IsRequired` tinyint(1) NOT NULL DEFAULT 1,
                        `DownloadUrl` varchar(1024) NULL,
                        `Category` varchar(64) NULL,
                        `DisplayName` varchar(255) NULL,
                        `Description` varchar(1024) NULL,
                        PRIMARY KEY (`Id`),
                        KEY `IX_clientbuildfiles_BuildSlug` (`BuildSlug`)
                    );
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine("TABLE CREATE ERROR: " + ex.Message);
            }

            try
            {
                var conn = db.Database.GetDbConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'LauncherSessions' AND COLUMN_NAME IN ('ClientToken', 'ServerId');";
                var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) existingCols.Add(reader.GetString(0));
                }
                if (!existingCols.Contains("ClientToken"))
                {
                    cmd.CommandText = "ALTER TABLE `LauncherSessions` ADD COLUMN `ClientToken` varchar(128) NULL;";
                    cmd.ExecuteNonQuery();
                }
                if (!existingCols.Contains("ServerId"))
                {
                    cmd.CommandText = "ALTER TABLE `LauncherSessions` ADD COLUMN `ServerId` varchar(128) NULL;";
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }

            SeedClientBuildFiles(db, app.Environment);
            SeedAdminAndEcosystemData(db, app.Environment);
        }

        app.UseRequestLocalization();

        if (app.Environment.IsProduction())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseCookiePolicy();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute("admin", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        app.MapControllers();

        app.Run();
    }

    private static void SeedClientBuildFiles(AppDbContext db, IWebHostEnvironment env)
    {
        try
        {
            var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var buildBase = Path.Combine(root, "builds", "vec-fabric-1-21-11");
            Directory.CreateDirectory(Path.Combine(buildBase, "mods"));
            Directory.CreateDirectory(Path.Combine(buildBase, "shaderpacks"));
            Directory.CreateDirectory(Path.Combine(buildBase, "resourcepacks"));

            var defaultFiles = new[]
            {
                new { Folder = "mods", File = "fabric-api-0.141.6+1.21.11.jar", Name = "Fabric API", Req = true, Cat = "Core", Desc = "Базовый модуль хуков и событий Fabric 1.21.11" },
                new { Folder = "mods", File = "voicechat-fabric-1.21.11-2.6.24.jar", Name = "Simple Voice Chat", Req = true, Cat = "Core", Desc = "Позиционный голосовой 3D чат сервера VEC" },
                new { Folder = "mods", File = "sodium-fabric-0.8.15-beta.1+mc1.21.11.jar", Name = "Sodium", Req = true, Cat = "Optimization", Desc = "Движок рендеринга и многократного повышения FPS" },
                new { Folder = "mods", File = "iris-fabric-1.10.8+mc1.21.11.jar", Name = "Iris Shaders", Req = false, Cat = "Visual", Desc = "Современный движок ультра-шейдеров" },
                new { Folder = "mods", File = "entity_model_features-3.3.11-1.21.11-fabric.jar", Name = "Entity Model Features (EMF)", Req = false, Cat = "Visual", Desc = "Поддержка кастомных 3D моделей сущностей из ресурспаков" },
                new { Folder = "mods", File = "entity_texture_features-7.2.5-1.21.11-fabric.jar", Name = "Entity Texture Features (ETF)", Req = false, Cat = "Visual", Desc = "Поддержка пользовательских и рандомных текстур мобов OptiFine" },
                new { Folder = "mods", File = "modmenu-17.0.1.jar", Name = "Mod Menu", Req = false, Cat = "Utility", Desc = "Удобный список и настройки модов в главном меню" },
                new { Folder = "mods", File = "appleskin-fabric-mc1.21.11-3.0.8.jar", Name = "AppleSkin", Req = false, Cat = "Utility", Desc = "Показ насыщения еды и очков голода" },
                new { Folder = "shaderpacks", File = "ComplementaryReimagined_r5.9.3.zip", Name = "Complementary Reimagined", Req = false, Cat = "Shaders", Desc = "Премиальный шейдерпак с кинематографичным светом, туманом и отражениями" },
                new { Folder = "resourcepacks", File = "FreshAnimations_v1.10.4.zip", Name = "Fresh Animations", Req = false, Cat = "Resourcepacks", Desc = "Плавные динамичные анимации существ (требует EMF и ETF)" }
            };

            var validRelPaths = defaultFiles.Select(m => $"{m.Folder}/{m.File}").ToHashSet(StringComparer.OrdinalIgnoreCase);
            var obsolete = db.ClientBuildFiles.Where(f => f.BuildSlug == "vec-fabric-1-21-11" && !validRelPaths.Contains(f.RelativePath)).ToList();
            if (obsolete.Count > 0)
            {
                db.ClientBuildFiles.RemoveRange(obsolete);
                db.SaveChanges();
            }

            using var sha = System.Security.Cryptography.SHA256.Create();

            foreach (var m in defaultFiles)
            {
                var filePath = Path.Combine(buildBase, m.Folder, m.File);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var bytes = File.ReadAllBytes(filePath);
                var hash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
                var relPath = $"{m.Folder}/{m.File}";

                var existing = db.ClientBuildFiles.FirstOrDefault(f => f.BuildSlug == "vec-fabric-1-21-11" && f.RelativePath == relPath);
                if (existing == null)
                {
                    existing = new ClientBuildFile
                    {
                        BuildSlug = "vec-fabric-1-21-11",
                        RelativePath = relPath,
                        FileName = m.File,
                        Category = m.Cat,
                        DisplayName = m.Name,
                        Description = m.Desc,
                        IsRequired = m.Req,
                        DownloadUrl = $"/api/builds/vec-fabric-1-21-11/file/{relPath}"
                    };
                    db.ClientBuildFiles.Add(existing);
                }

                existing.Sha256 = hash;
                existing.FileSizeBytes = bytes.LongLength;
                existing.DownloadUrl = $"/api/builds/vec-fabric-1-21-11/file/{relPath}";
                existing.DisplayName = m.Name;
                existing.Description = m.Desc;
                existing.Category = m.Cat;
                existing.IsRequired = m.Req;
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("SEED MODS ERROR: " + ex.Message);
        }
    }

    private static void SeedAdminAndEcosystemData(AppDbContext db, IWebHostEnvironment env)
    {
        try
        {
            var mp = db.MainPageData.FirstOrDefault(m => m.Id == 1);
            if (mp != null)
            {
                mp.ServerVersion = "1.21.11 Fabric";
                mp.YellowHint = "Fabric 1.21.11 • Закрытый сервер с защитой";
                if (string.IsNullOrWhiteSpace(mp.Title) || mp.Title.Contains("1.20.1") || mp.Title.Contains("1.21.1\r"))
                {
                    mp.Title = "Vyacheslav's Mega Server — Открой свой идеальный мир";
                }
                if (string.IsNullOrWhiteSpace(mp.Description) || mp.Description.Contains("полностью ванильный"))
                {
                    mp.Description = "<p><strong>Vyacheslav's Mega Server</strong> — официальный Minecraft сервер организации «Vyacheslav's Engineering Company» (VEC) на версии <strong>Fabric 1.21.11</strong>.</p><p>Для входа требуется официальный лаунчер VEC со встроенным античитом и голосовым 3D-чатом. Честное выживание без платных донат-привилегий и админок.</p>";
                }
            }

            var launcherBtn = db.DownloadButtons.FirstOrDefault(b => b.Url.Contains("VECLauncher.exe"));
            if (launcherBtn == null)
            {
                db.DownloadButtons.Add(new DownloadButton
                {
                    Title = "Скачать лаунчер",
                    Url = "/downloads/VECLauncher.exe",
                    ShowButton = true,
                    IsResourceLocal = true
                });
            }
            else
            {
                launcherBtn.Title = "Скачать лаунчер";
                launcherBtn.Url = "/downloads/VECLauncher.exe";
                launcherBtn.ShowButton = true;
                launcherBtn.IsResourceLocal = true;
            }

            if (!db.ServerMediaLinks.Any())
            {
                db.ServerMediaLinks.Add(new ServerMediaLink
                {
                    Url = "https://t.me/vyacheslav_minecraft",
                    Content = "Telegram",
                    LogoFileName = "d0b8c984-1c46-4d5e-b905-051e3b3a887e.webp",
                    ShowLink = true
                });
                db.ServerMediaLinks.Add(new ServerMediaLink
                {
                    Url = "https://discord.gg/ATES48pgJj",
                    Content = "Discord",
                    LogoFileName = "7c32631b-dcc3-4862-b3e1-94286da932dc.png",
                    ShowLink = true
                });
                db.ServerMediaLinks.Add(new ServerMediaLink
                {
                    Url = "https://vk.com/sansei_1",
                    Content = "ВКонтакте",
                    LogoFileName = "0cbb1649-101e-4ee1-8200-f18fafbd4384.png",
                    ShowLink = true
                });
            }

            var contact1 = db.Contacts.FirstOrDefault(c => c.Id == 1);
            if (contact1 != null)
            {
                contact1.DisplayName = "Вячеслав";
                contact1.Description = "Основатель VEC";
                contact1.DescriptionLarge = "Создатель и архитектор сервера, Важный Еврей проекта. Лада Гранта — лучшая машина.";
                contact1.ShowContact = true;
            }

            var contact2 = db.Contacts.FirstOrDefault(c => c.Id == 2);
            if (contact2 == null)
            {
                contact2 = new Contact
                {
                    Id = 2,
                    DisplayName = "ALWLEX",
                    Description = "Разработчик VEC",
                    DescriptionLarge = "Разработка официального веб-портала, игрового лаунчера и технической инфраструктуры VEC. По техническим вопросам, багам и предложениям — пишите напрямую.",
                    ShowContact = true,
                    UserId = "d4bc75a9-ca48-45aa-a3f2-2ea5886ce303"
                };
                db.Contacts.Add(contact2);
                db.SaveChanges();

                db.ContactLinks.Add(new ContactLink
                {
                    ContactId = 2,
                    Description = "Telegram: ",
                    Content = "@ALWLEX",
                    Url = "https://t.me/ALWLEX"
                });
                db.ContactLinks.Add(new ContactLink
                {
                    ContactId = 2,
                    Description = "Discord: ",
                    Content = "alwlex_",
                    Url = "alwlex_"
                });
            }

            if (!db.Goals.Any(g => g.IsCurrent))
            {
                db.Goals.Add(new Goal
                {
                    Name = "Оплата хостинга сервера VEC 1.21.11 на 6 месяцев",
                    Price = 15000,
                    Date = DateTime.Now,
                    IsCurrent = true,
                    IsCompleted = false
                });
            }

            if (!db.DonationCards.Any())
            {
                db.DonationCards.Add(new DonationCard
                {
                    CardName = "Kaspi Gold",
                    BankName = "Kaspi",
                    CardNumber = "4400 4301 2345 6789",
                    CardHolder = "Вячеслав К.",
                    ColorDownLeft = "#c0392b",
                    ColorUpRight = "#e74c3c"
                });
                db.DonationCards.Add(new DonationCard
                {
                    CardName = "Сбербанк",
                    BankName = "Сбер",
                    CardNumber = "2202 2032 1234 5678",
                    CardHolder = "Вячеслав К.",
                    ColorDownLeft = "#27ae60",
                    ColorUpRight = "#2ecc71"
                });
            }

            if (!db.TransactionPurposes.Any())
            {
                db.TransactionPurposes.Add(new TransactionPurpose
                {
                    Name = "Добровольные пожертвования игроков",
                    Type = TransactionPurpose.TransactionType.Income
                });
                db.TransactionPurposes.Add(new TransactionPurpose
                {
                    Name = "Оплата хостинга сервера VEC 1.21.11",
                    Type = TransactionPurpose.TransactionType.Expense
                });
                db.TransactionPurposes.Add(new TransactionPurpose
                {
                    Name = "Разработка и безопасность экосистемы VEC",
                    Type = TransactionPurpose.TransactionType.Expense
                });
                db.TransactionPurposes.Add(new TransactionPurpose
                {
                    Name = "Домен и веб-сервер",
                    Type = TransactionPurpose.TransactionType.Expense
                });
            }

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("SEED ADMIN DATA ERROR: " + ex.Message);
        }
    }
}