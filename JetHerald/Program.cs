using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using NLog.Web;

using JetHerald;
using JetHerald.Options;
using JetHerald.Services;
using JetHerald.Middlewares;
using JetHerald.Utils;
using JetHerald.Authorization;
using Microsoft.Extensions.Hosting;
using NLog;



#if DEBUG
var nlogConfigPath = "nlog.debug.config";
#else
var nlogConfigPath = "nlog.config";
#endif
LogManager.ThrowConfigExceptions = true;
LogManager.Setup().LoadConfigurationFromFile(nlogConfigPath);
var log = LogManager.GetCurrentClassLogger();

try
{
    log.Info("init main");

    DapperConverters.Register();

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        WebRootPath = "wwwroot",
        Args = args,
    });

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    var cfg = builder.Configuration;
    var services = builder.Services;

    services.Configure<ConnectionStrings>(cfg.GetSection("ConnectionStrings"));
    services.Configure<JetHerald.Options.TelegramConfig>(cfg.GetSection("Telegram"));
    services.Configure<DiscordConfig>(cfg.GetSection("Discord"));
    services.Configure<TimeoutConfig>(cfg.GetSection("Timeout"));
    services.Configure<AuthConfig>(cfg.GetSection("AuthConfig"));

    services.AddSingleton<Db>();
    services.AddSingleton<JetHeraldBot>().AddHostedService(s => s.GetService<JetHeraldBot>());
    services.AddSingleton<LeakyBucket>();
    services.AddSingleton<RequestTimeTrackerMiddleware>();
    services.AddSingleton<AnonymousUserMassagerMiddleware>();
    services.AddHostedService<HeartMonitor>();

    var mvcBuilder = services.AddControllersWithViews();

    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "jetherald.sid";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.HttpOnly = true;
        // options.SessionStore = new JetHeraldTicketStore();
        options.LoginPath = "/ui/login";
        options.LogoutPath = "/ui/logout";
        options.ReturnUrlParameter = "redirect";
        options.AccessDeniedPath = "/ui/403";
        options.ClaimsIssuer = "JetHerald";
    });
    services.AddPermissions();

    var app = builder.Build();

    // preflight checks
    {
        File.WriteAllLines("used-permissions.txt", FlightcheckHelpers.GetUsedPermissions(typeof(Program)));

        var db = app.Services.GetService<Db>();
        using var ctx = await db.GetContext();
        var adminUser = await ctx.GetUser("admin");
        if (adminUser == null)
        {
            var adminRole = (await ctx.GetRoles()).First(r => r.Name == "admin");
            var unlimitedPlan = (await ctx.GetPlans()).First(p => p.Name == "unlimited");

            var authCfg = app.Services.GetService<IOptions<AuthConfig>>().Value;
            var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            adminUser = new JetHerald.Contracts.User()
            {
                Login = "admin",
                Name = "Administrator",
                PasswordSalt = RandomNumberGenerator.GetBytes(64),
                HashType = authCfg.HashType,
                RoleId = adminRole.RoleId,
                PlanId = unlimitedPlan.PlanId
            };
            adminUser.PasswordHash = AuthUtils.GetHashFor(password, adminUser.PasswordSalt, adminUser.HashType);
            var newUser = await ctx.RegisterUser(adminUser);
            ctx.Commit();
            File.WriteAllText("admin.txt", $"{adminUser.Login}\n{password}");
            log.Warn($"Saved administrative account to admin.txt");
        }
    }
    app.UseMiddleware<RequestTimeTrackerMiddleware>();
    app.UsePathBase(cfg.GetValue<string>("PathBase"));
    app.UseForwardedHeaders();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseMiddleware<AnonymousUserMassagerMiddleware>();

    app.UseStaticFiles();
    app.UseStatusCodePagesWithReExecute("/ui/{0}");
    app.UseRouting();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception exception)
{
    log.Error(exception, "Error while starting up");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}
