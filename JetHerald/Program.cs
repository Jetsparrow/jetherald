using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;
using JetHerald;
using JetHerald.Options;
using JetHerald.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using JetHerald.Middlewares;
using System.Security.Cryptography;
using JetHerald.Utils;
using JetHerald.Authorization;

#if DEBUG
var debug = true;
#else
var debug = false;
#endif
var log = NLogBuilder.ConfigureNLog(debug ? "nlog.debug.config" : "nlog.config").GetCurrentClassLogger();

try
{
    log.Info("init main");

    DapperConverters.Register();
    log.Info($"Permissions digest:\n{string.Join('\n', FlightcheckHelpers.GetUsedPermissions(typeof(Program)))}");

    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
        ContentRootPath = Directory.GetCurrentDirectory(),
        Args = args, 
    });

    builder.WebHost.ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddIniFile("secrets.ini",
            optional: true, reloadOnChange: true);
        config.AddIniFile($"secrets.{hostingContext.HostingEnvironment.EnvironmentName}.ini",
            optional: true, reloadOnChange: true);
        config.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
        config.AddJsonFile($"secrets.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
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
            log.Warn($"Created administrative account {adminUser.Login}:{password}. Be sure to save these credentials somewhere!");
        }
    }
    app.UseMiddleware<RequestTimeTrackerMiddleware>();
    app.UsePathBase(cfg.GetValue<string>("PathBase"));
    app.UseAuthentication();
    app.UseMiddleware<AnonymousUserMassagerMiddleware>();
    app.UseDeveloperExceptionPage();
    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseStatusCodePagesWithReExecute("/{0}");
    app.UseRouting();
    app.UseAuthorization();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
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
