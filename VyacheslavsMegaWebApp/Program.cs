using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VyacheslavsMegaWebApp.Data;
using VyacheslavsMegaWebApp.Data.Entities.Identity;
using VyacheslavsMegaWebApp.Data.JSON.Configuration;
using VyacheslavsMegaWebApp.Data.JSON.Repositories;
using VyacheslavsMegaWebApp.Data.Repositories;
using VyacheslavsMegaWebApp.Service;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        new ConfigurationBuilder()
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build()
            .Bind("Project", new ProjectConfig());

        builder.Services.AddAuthorization(x =>
            x.AddPolicy("AdminAreaPolicy", policy => policy.RequireRole("Admin")));

        builder.Services.AddControllersWithViews(x =>
            x.Conventions.Add(new AdminAreaAutorization("Admin", "AdminAreaPolicy")));

        builder.Services.AddSingleton<System.Text.Encodings.Web.HtmlEncoder>(
            System.Text.Encodings.Web.HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));

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
            options.SupportedCultures = new[] { new CultureInfo("ru-RU") };
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
        builder.Services.AddTransient<ClientBuildFilesRepository>();
        builder.Services.AddTransient<FriendshipsRepository>();
        builder.Services.AddTransient<CapesRepository>();
        builder.Services.AddTransient<SkinStorageService>();
        builder.Services.AddSingleton<LaunchTicketService>();
        builder.Services.AddSingleton<YggdrasilService>();
        builder.Services.AddSingleton<AppResourcesManager>();
        builder.Services.AddSingleton<ModrinthService>();
        builder.Services.AddHttpClient<MinecraftLicenseService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try { db.Database.Migrate(); } catch { }
            try { db.Database.ExecuteSqlRaw("ALTER TABLE `ClientBuildFiles` ADD COLUMN `BannerUrl` longtext NULL;"); } catch { }
            try { DataSeeder.Seed(db, app.Environment); } catch { }
        }

        app.UseRequestLocalization();

        if (app.Environment.IsProduction())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
            app.UseHttpsRedirection();
        }
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
}