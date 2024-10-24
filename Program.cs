using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CMCS.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using CMCS.Services;

namespace CMCS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder.Services);

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("AllowAll");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Remove the in-memory database context registration
            // services.AddDbContext<ApplicationDbContext>(options =>
            //     options.UseInMemoryDatabase("InMemoryDb"));

            // Register in-memory storage for users and claims
            services.AddSingleton<List<User>>(new List<User>());
            services.AddSingleton<List<Claim>>(new List<Claim>());

            // Register the claims service if you have any logic to handle claims
            //services.AddSingleton<IClaimsService, ClaimsService>();
            services.AddScoped<IClaimsService, ClaimsService>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Lecturer", policy => policy.RequireRole("Lecturer"));
                options.AddPolicy("ProgrammeCoordinator", policy => policy.RequireRole("ProgrammeCoordinator"));
                options.AddPolicy("AcademicManager", policy => policy.RequireRole("AcademicManager"));
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("AllowAll");

            
            // Remove the Browser Link configuration as it is not supported
            //app.UseBrowserLink(options => options.Disabled = true);

        }
    }
}