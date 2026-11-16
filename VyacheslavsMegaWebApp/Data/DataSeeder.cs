using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using VyacheslavsMegaWebApp.Data.Entities;

namespace VyacheslavsMegaWebApp.Data
{
    public static class DataSeeder
    {
        public static void Seed(AppDbContext db, IWebHostEnvironment env)
        {
            SeedClientBuildFiles(db);
            SeedEcosystemData(db);
            SeedDefaultCapes(db, env);
        }

        private static void SeedClientBuildFiles(AppDbContext db)
        {
            var defaultFiles = new[]
            {
                new {
                    Slug = "fabric-api",
                    Name = "Fabric API",
                    Cat = "Core",
                    Req = true,
                    Desc = "Базовый модуль хуков и событий Fabric 1.21.11",
                    Folder = "mods",
                    File = "fabric-api-0.116.17+1.21.1.jar",
                    Url = "https://cdn.modrinth.com/data/P7dR8mSH/versions/Mys3P7lK/fabric-api-0.116.17%2B1.21.1.jar",
                    Size = 2452735L,
                    Sha = "79ac44b40780acbd884b34c50be1e39af682847e5f5cb3b1fddeeaa768dce800",
                    Icon = "https://cdn.modrinth.com/data/P7dR8mSH/icon.png",
                    Banner = (string?)null
                },
                new {
                    Slug = "simple-voice-chat",
                    Name = "Simple Voice Chat",
                    Cat = "Core",
                    Req = true,
                    Desc = "Позиционный голосовой 3D чат сервера VEC",
                    Folder = "mods",
                    File = "voicechat-fabric-1.21.11-2.6.24.jar",
                    Url = "https://cdn.modrinth.com/data/9eGKb6K1/versions/MLNG868g/voicechat-fabric-1.21.11-2.6.24.jar",
                    Size = 5608482L,
                    Sha = "07762e88744866f9dce5eb9f255d568e72cf8a6ba02bf464532df04c9221a8fd",
                    Icon = "https://cdn.modrinth.com/data/9eGKb6K1/icon.png",
                    Banner = "https://cdn.modrinth.com/data/9eGKb6K1/images/95490c1c7cf1be0d2981476efbc6acfe79c8b58b_350.webp"
                },
                new {
                    Slug = "sodium",
                    Name = "Sodium",
                    Cat = "Optimization",
                    Req = true,
                    Desc = "Движок рендеринга и многократного повышения FPS",
                    Folder = "mods",
                    File = "sodium-fabric-0.8.15-beta.1+mc1.21.11.jar",
                    Url = "https://cdn.modrinth.com/data/AANobbMI/versions/RB7CDjTS/sodium-fabric-0.8.15-beta.1%2Bmc1.21.11.jar",
                    Size = 1914874L,
                    Sha = "f0bd613154a9ed0712d1d8aa62f1de3fb0fc1e76b4edc37f1250679007029775",
                    Icon = "https://cdn.modrinth.com/data/AANobbMI/295862f4724dc3f78df3447ad6072b2dcd3ef0c9_96.webp",
                    Banner = "https://cdn.modrinth.com/data/AANobbMI/images/d84313e6f57dc9e7896961dbd2dfc2689d482758_350.webp"
                },
                new {
                    Slug = "iris",
                    Name = "Iris Shaders",
                    Cat = "Visual",
                    Req = false,
                    Desc = "Современный движок ультра-шейдеров",
                    Folder = "mods",
                    File = "iris-fabric-1.10.8+mc1.21.11.jar",
                    Url = "https://cdn.modrinth.com/data/YL57xq9U/versions/ka9PDlsN/iris-fabric-1.10.8%2Bmc1.21.11.jar",
                    Size = 2807718L,
                    Sha = "5b6b5124c74407ffe4945f8da61491731cbf697f0bba4b14257e0ff977dac087",
                    Icon = "https://cdn.modrinth.com/data/YL57xq9U/18d0e7f076d3d6ed5bedd472b853909aac5da202_96.webp",
                    Banner = "https://cdn.modrinth.com/data/YL57xq9U/images/f47cea26f135d74c2d69df0a1806751818d705d3_350.webp"
                },
                new {
                    Slug = "entity-model-features",
                    Name = "Entity Model Features (EMF)",
                    Cat = "Visual",
                    Req = false,
                    Desc = "Поддержка кастомных 3D моделей сущностей из ресурспаков",
                    Folder = "mods",
                    File = "entity_model_features-3.3.12-1.21.9-fabric.jar",
                    Url = "https://cdn.modrinth.com/data/4I1XuqiY/versions/9Gv6SVe7/entity_model_features-3.3.12-1.21.9-fabric.jar",
                    Size = 668376L,
                    Sha = "b752c15f5339bc7de94a0a1ed7c837e89a3f59b4824766e1fc5ac63c57719711",
                    Icon = "https://cdn.modrinth.com/data/4I1XuqiY/6e5bbd0d06b1741bfdab6c0cfab6de8fdaf0064c_96.webp",
                    Banner = "https://cdn.modrinth.com/data/4I1XuqiY/images/ba682dbb4d4ff5df8900987e6d8e86f4997fc119_350.webp"
                },
                new {
                    Slug = "entitytexturefeatures",
                    Name = "Entity Texture Features (ETF)",
                    Cat = "Visual",
                    Req = false,
                    Desc = "Поддержка пользовательских и рандомных текстур мобов OptiFine",
                    Folder = "mods",
                    File = "entity_texture_features-7.2.6-1.21.9-fabric.jar",
                    Url = "https://cdn.modrinth.com/data/BVzZfTc1/versions/Y65jA7rB/entity_texture_features-7.2.6-1.21.9-fabric.jar",
                    Size = 803120L,
                    Sha = "93de9a53541f182fa12bf71e2da403028f6ae27770de597a146b48764835e6d5",
                    Icon = "https://cdn.modrinth.com/data/BVzZfTc1/af683d206d50b05258d865b0d6e4aa2f2cee12f2_96.webp",
                    Banner = "https://cdn.modrinth.com/data/BVzZfTc1/images/5260909bc44abcade39ce46ec57d0edf6107ffb0_350.webp"
                },
                new {
                    Slug = "modmenu",
                    Name = "Mod Menu",
                    Cat = "Utility",
                    Req = false,
                    Desc = "Удобный список и настройки модов в главном меню",
                    Folder = "mods",
                    File = "modmenu-17.0.1.jar",
                    Url = "https://cdn.modrinth.com/data/mOgUt4GM/versions/cLkYaVuL/modmenu-17.0.1.jar",
                    Size = 960882L,
                    Sha = "2eb2d6db9964fcf2e44e2be1e4d172b8c4e829e385c8f31636de94a1708c2076",
                    Icon = "https://cdn.modrinth.com/data/mOgUt4GM/5a20ed1450a0e1e79a1fe04e61bb4e5878bf1d20.png",
                    Banner = "https://cdn.modrinth.com/data/mOgUt4GM/images/70fd54b0579f2de3ac45fa9650aca2ec8ff0882b_350.webp"
                },
                new {
                    Slug = "appleskin",
                    Name = "AppleSkin",
                    Cat = "Utility",
                    Req = false,
                    Desc = "Показ насыщения еды и очков голода",
                    Folder = "mods",
                    File = "appleskin-fabric-mc1.21.11-3.0.8.jar",
                    Url = "https://cdn.modrinth.com/data/EsAfCjCV/versions/59ti1rvg/appleskin-fabric-mc1.21.11-3.0.8.jar",
                    Size = 180107L,
                    Sha = "04fe837be8f10bb5e6b999238594466c59bdb641a54512f19f1e67241d882ae3",
                    Icon = "https://cdn.modrinth.com/data/EsAfCjCV/icon.png",
                    Banner = (string?)null
                },
                new {
                    Slug = "complementary-reimagined",
                    Name = "Complementary Reimagined",
                    Cat = "Shaders",
                    Req = false,
                    Desc = "Премиальный шейдерпак с кинематографичным светом, туманом и отражениями",
                    Folder = "shaderpacks",
                    File = "ComplementaryReimagined_r5.9.3.zip",
                    Url = "https://cdn.modrinth.com/data/HVnmMxH1/versions/Bqen1mJX/ComplementaryReimagined_r5.9.3.zip",
                    Size = 553397L,
                    Sha = "fed6c879e732cf7fc7bc6570ebb1def43235aa7e96f62859fbdbdbfa0b484b38",
                    Icon = "https://cdn.modrinth.com/data/HVnmMxH1/79cb7c8123bbc54945305b2ebad6b8881efdf5f8_96.webp",
                    Banner = "https://cdn.modrinth.com/data/HVnmMxH1/images/111e76bfa966c68b3581a93f7322588e642ce4db_350.webp"
                },
                new {
                    Slug = "fresh-animations",
                    Name = "Fresh Animations",
                    Cat = "Resourcepacks",
                    Req = false,
                    Desc = "Плавные динамичные анимации существ (требует EMF и ETF)",
                    Folder = "resourcepacks",
                    File = "FreshAnimations_v1.10.4.zip",
                    Url = "https://cdn.modrinth.com/data/50dA9Sha/versions/xN57JJts/FreshAnimations_v1.10.4.zip",
                    Size = 850941L,
                    Sha = "8e44b29560d2e7d952c0a4f77f0451e6dcb048f827189bffc712b446be8de7f2",
                    Icon = "https://cdn.modrinth.com/data/50dA9Sha/3132c10e9e3c73fde9799720fd3da5561071708c_96.webp",
                    Banner = "https://cdn.modrinth.com/data/50dA9Sha/images/c6a5bc8c69dea40a3d6f343390d04f2fc01fb0d6_350.webp"
                }
            };

            var validSlugs = defaultFiles.Select(m => m.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var obsolete = db.ClientBuildFiles
                .Where(f => f.BuildSlug == "vec-fabric-1-21-11" && !string.IsNullOrEmpty(f.ModrinthSlug) && !validSlugs.Contains(f.ModrinthSlug))
                .ToList();

            if (obsolete.Count > 0)
            {
                db.ClientBuildFiles.RemoveRange(obsolete);
                db.SaveChanges();
            }

            foreach (var m in defaultFiles)
            {
                var relPath = $"{m.Folder}/{m.File}";
                var existing = db.ClientBuildFiles.FirstOrDefault(f => f.BuildSlug == "vec-fabric-1-21-11" && (f.ModrinthSlug == m.Slug || f.DisplayName == m.Name));
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
                        DownloadUrl = m.Url,
                        FileSizeBytes = m.Size,
                        Sha256 = m.Sha,
                        IconUrl = m.Icon,
                        BannerUrl = m.Banner,
                        SourceType = "Modrinth",
                        ModrinthSlug = m.Slug
                    };
                    db.ClientBuildFiles.Add(existing);
                }
                else
                {
                    existing.DisplayName = m.Name;
                    existing.Description = m.Desc;
                    existing.Category = m.Cat;
                    existing.IsRequired = m.Req;
                    existing.FileName = m.File;
                    existing.RelativePath = relPath;
                    existing.DownloadUrl = m.Url;
                    existing.FileSizeBytes = m.Size;
                    existing.Sha256 = m.Sha;
                    existing.IconUrl = m.Icon;
                    existing.BannerUrl = m.Banner;
                    existing.SourceType = "Modrinth";
                    existing.ModrinthSlug = m.Slug;
                }
            }

            db.SaveChanges();
        }

        private static void SeedEcosystemData(AppDbContext db)
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

            var launcherBtn = db.DownloadButtons.FirstOrDefault(b => b.Url.Contains("VEC_Setup.exe") || b.Url.Contains("VECLauncher.exe"));
            if (launcherBtn == null)
            {
                db.DownloadButtons.Add(new DownloadButton
                {
                    Title = "Скачать лаунчер",
                    Url = "/downloads/VEC_Setup.exe",
                    ShowButton = true,
                    IsResourceLocal = true
                });
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

        public static void SeedDefaultCapes(AppDbContext db, IWebHostEnvironment env)
        {
            if (!db.CustomCapes.Any(c => c.Slug == "vec"))
            {
                var vecFile = Path.Combine(env.WebRootPath, "img", "capes", "cape_vec.png");
                string hash = "75c0462e8904da6f38b3b597352cf2e2724b504a567a659db877dc328f9aa4a7";
                if (File.Exists(vecFile))
                {
                    try
                    {
                        var bytes = File.ReadAllBytes(vecFile);
                        using var sha = System.Security.Cryptography.SHA256.Create();
                        hash = Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
                        var targetDir = Path.Combine(env.WebRootPath, "uploads", "textures");
                        Directory.CreateDirectory(targetDir);
                        var targetFile = Path.Combine(targetDir, $"{hash}.png");
                        if (!File.Exists(targetFile)) File.WriteAllBytes(targetFile, bytes);
                    }
                    catch { }
                }

                db.CustomCapes.Add(new CustomCape
                {
                    Slug = "vec",
                    Name = "Плащ VEC",
                    Description = "Официальный плащ проекта VEC",
                    TextureHash = hash,
                    IsPublic = true,
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
        }
    }
}
