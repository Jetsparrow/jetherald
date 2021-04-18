using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JetHerald
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Options.ConnectionStrings>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<Options.Telegram>(Configuration.GetSection("Telegram"));
            services.Configure<Options.Discord>(Configuration.GetSection("Discord"));
            services.AddSingleton<Db>();
            services.AddSingleton<JetHeraldBot>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var bot = app.ApplicationServices.GetService<JetHeraldBot>();
            bot.Init().GetAwaiter().GetResult();
            app.UsePathBase(Configuration.GetValue<string>("PathBase"));
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
