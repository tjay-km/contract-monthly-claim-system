using System.Globalization;
using CMCS.Data;
using CMCS.Models;
using CMCS.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "This field is required.");
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var culture = new CultureInfo("en-US");
    options.DefaultRequestCulture = new RequestCulture(culture);
    options.SupportedCultures = new[] { culture };
    options.SupportedUICultures = new[] { culture };
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection(FileUploadSettings.SectionName));

builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IClaimValidationService, ClaimValidationService>();
builder.Services.AddScoped<IClaimApprovalService, ClaimApprovalService>();
builder.Services.AddScoped<IAutoApprovalConfigService, AutoApprovalConfigService>();
builder.Services.AddScoped<IClaimAuditService, ClaimAuditService>();
builder.Services.AddScoped<IClaimReportService, ClaimReportService>();

var app = builder.Build();

// Apply migrations and seed roles (and demo users in development).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        await SeedData.InitializeRolesAsync(services.GetRequiredService<RoleManager<IdentityRole>>());

        var demoPassword = app.Configuration["DemoUsers:Password"];
        if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(demoPassword))
            await SeedData.SeedDemoUsersAsync(services.GetRequiredService<UserManager<ApplicationUser>>(), demoPassword);
    }
    catch (Exception ex)
    {
        services.GetRequiredService<ILogger<Program>>().LogError(ex, "An error occurred while preparing the database.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposes the entry point to the integration tests (WebApplicationFactory<Program>).
public partial class Program { }
