using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;
using JetHerald;
using JetHerald.Options;
using JetHerald.Services;

var log =
#if DEBUG
NLogBuilder.ConfigureNLog("nlog.debug.config").GetCurrentClassLogger();
#else
NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
#endif

try
{
    log.Info("init main");

    DapperConverters.Register();

    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory());
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
    services.AddSingleton<Db>();
    services.AddSingleton<JetHeraldBot>().AddHostedService(s => s.GetService<JetHeraldBot>());
    services.AddSingleton<LeakyBucket>();
    services.AddHostedService<HeartMonitor>();
    services.AddMvc();


    var app = builder.Build();
    app.UsePathBase(cfg.GetValue<string>("PathBase"));
    app.UseDeveloperExceptionPage();
    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
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
