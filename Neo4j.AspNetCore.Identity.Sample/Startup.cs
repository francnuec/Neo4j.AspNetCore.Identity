using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neo4j.AspNetCore.Identity.Sample.Models;
using Neo4j.AspNetCore.Identity.Sample.Services;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Neo4jClient;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neo4jClient.DataAnnotations;
using Neo4j.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Neo4j.AspNetCore.Identity.Sample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
            HostingEnvironment = env;
        }

        public IConfigurationRoot Configuration { get; }

        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.Configure<Neo4jDbSettings>(Configuration.GetSection("Neo4jDbSettings"));
            services.AddScoped<IGraphClient, GraphClient>(provider =>
            {
                var options = provider.GetService<IOptions<Neo4jDbSettings>>();
                var client = new GraphClient(new Uri(options.Value.uri),
                    username: options.Value.username, password: options.Value.password);
                client.ConnectAsync().Wait();
                return client;
            });

            services.AddNeo4jAnnotations<ApplicationContext>(); //services.AddNeo4jAnnotations();

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                //var dataProtectionPath = Path.Combine(HostingEnvironment.WebRootPath, "identity-artifacts");
                //options.Cookies.ApplicationCookie.AuthenticationScheme = "ApplicationCookie";
                //options.Cookies.ApplicationCookie.DataProtectionProvider = DataProtectionProvider.Create(dataProtectionPath);

                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            })
            .AddUserStore<UserStore<ApplicationUser>>()
            .AddRoleStore<RoleStore<IdentityRole>>()
            .AddDefaultTokenProviders();


            //// Services used by identity
            ////services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
            //services.AddAuthentication(options =>
            //{
            //    // This is the Default value for ExternalCookieAuthenticationScheme
            //    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme; //new IdentityCookieOptions().ExternalCookieAuthenticationScheme;
            //});

            // Hosting doesn't add IHttpContextAccessor by default
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddOptions();
            services.AddDataProtection();

            services.AddMvc();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.AddApplicationInsightsTelemetry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            // Add external authentication middleware below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
