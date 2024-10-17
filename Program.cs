using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CMCS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create a WebApplicationBuilder instance
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Add session services
            builder.Services.AddDistributedMemoryCache(); // Required for session storage
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
                options.Cookie.HttpOnly = true; // Ensure cookie can't be accessed via JavaScript
                options.Cookie.IsEssential = true; // Session cookie is essential for authentication
            });

            // Build the app
            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Middleware: HTTPS redirection, static files, routing, and session
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable session handling
            app.UseSession();

            app.UseAuthorization();

            // Add endpoints configuration here
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            // Start the app
            app.Run();
        }
    }
}
