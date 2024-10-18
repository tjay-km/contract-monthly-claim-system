using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using CMCS.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // Remove session service
            // services.AddDistributedMemoryCache();
            // services.AddSession(options =>
            // {
            //     options.IdleTimeout = TimeSpan.FromMinutes(30);
            //     options.Cookie.HttpOnly = true;
            //     options.Cookie.IsEssential = true;
            // });

            // In-memory user storage
            services.AddSingleton<List<User>>(new List<User>());
            services.AddControllersWithViews();

            // Register the claims list as a singleton (or scoped, depending on your needs)
            services.AddSingleton<List<Claim>>(new List<Claim>());

            // Register the claims service
            services.AddSingleton<IClaimsService, ClaimsService>();

            // Add cookie authentication
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                });

            // Add authorization policies for roles
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Lecturer", policy => policy.RequireRole("Lecturer"));
                options.AddPolicy("ProgrammeCoordinator", policy => policy.RequireRole("ProgrammeCoordinator"));
                options.AddPolicy("AcademicManager", policy => policy.RequireRole("AcademicManager"));
            });
        }
    }
}
