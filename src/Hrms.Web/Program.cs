using Hrms.Employees;
using Hrms.Employees.Application.Abstractions;
using Hrms.Identity;
using Hrms.Identity.Domain.Entities;
using Hrms.Settings;
using Hrms.Settings.Application.Abstractions;
using Hrms.Persistence;
using Hrms.Persistence.Interceptors;
using Hrms.Persistence.Seed;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Infrastructure;
using Hrms.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database & Persistence
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddScoped<AuditableEntityInterceptor>();

builder.Services.AddDbContext<HrmsDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
    options.UseSqlServer(connectionString)
           .AddInterceptors(interceptor);
});

// Identity Core & Cookie Auth
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;

    // Password policy
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Lockout settings (5 failed attempts)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<HrmsDbContext>()
.AddDefaultTokenProviders();

// Custom claims factory to load permission claims and employeeId into the cookie
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Dynamic Permission-based Authorization
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Shared infrastructure services
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IFileStore, LocalFileStore>();

// Data Protection (persist keys to disk)
var keysFolder = builder.Configuration["DataProtection:KeysFolder"] ?? "keys";
var keysPath = Path.IsPathRooted(keysFolder) ? keysFolder : Path.Combine(builder.Environment.ContentRootPath, keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("HRMS");

// Identity module services
builder.Services.AddIdentityModule();

// Employees module services
builder.Services.AddScoped<IEmployeesDbContext>(sp => sp.GetRequiredService<HrmsDbContext>());
builder.Services.AddEmployeesModule();

// Settings module services
builder.Services.AddScoped<ISettingsDbContext>(sp => sp.GetRequiredService<HrmsDbContext>());
builder.Services.AddSettingsModule();

// Seeder
builder.Services.AddScoped<IdentitySeeder>();

// MVC Controllers with Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seeding on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var seeder = services.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database seeding was skipped or database is not reachable at startup.");
    }
}

if (args.Contains("--seed-only"))
{
    Console.WriteLine("Database seeding finished.");
    return;
}

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
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// For integration testing support
public partial class Program { }
