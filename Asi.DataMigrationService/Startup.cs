using System;
using Asi.DataMigrationService.ComponentLib;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Lib;
using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Publisher.Hub;
using Asi.DataMigrationService.MessageQueue;
using Blazored.Modal;
using MatBlazor;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Asi.DataMigrationService
{
    public class Startup
    {
        private readonly bool _requireAuthentication = true;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _requireAuthentication = Configuration.GetSection("AzureAd").GetChildren().Any();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(opt =>
                    {
                        opt.AddConsole();
                        opt.AddDebug();
                    });

            if (_requireAuthentication)
            {
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                 .AddMicrosoftIdentityWebApp(Configuration);

                services
                    .AddControllersWithViews(options =>
                    {
                        var policy = new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build();
                        options.Filters.Add(new AuthorizeFilter(policy));
                    })
                    .AddMicrosoftIdentityUI();
            }
            else
            {
                services.AddControllersWithViews();
            }

            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpClient("SecureHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                });
            services.AddDataProtection();
            services.AddSignalR();
            services.AddHostedService<DataMigrationServiceBackgroundService>();
            services.AddClient();
            services.AddMessageQueue();
            services.AddDataMigrationServiceLib(Configuration);
            services.AddDataMigrationServiceComponentLib();
            services.AddScoped<HttpClient>();
            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddMatToaster(config =>
            {
                config.Position = MatToastPosition.TopFullWidth;
                config.PreventDuplicates = true;
                config.NewestOnTop = true;
                config.ShowCloseButton = true;
                config.MaximumOpacity = 100;
                config.VisibleStateDuration = 5000;
            });
            services.AddBlazoredModal();

            services.AddCascadingValue("RequireAuthentication", r => _requireAuthentication);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(System.IO.Path.Combine(env.ContentRootPath, "Docs")),
                RequestPath = "/docs"
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapHub<DataMigrationServiceHub>("/hubs/DataMigrationService");
            });

            InitializeDatabase(app);
        }

        private static void InitializeDatabase(IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }
    }
}
