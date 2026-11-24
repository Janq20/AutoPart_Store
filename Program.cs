using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using MyApp.Models;
using MyApp.Services;
using QuestPDF.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddDbContext<MyApp.Data.ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});
// Rejestracja kontrolerów i widoków MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("Login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IPaymentService, VirtualPaymentService>();
}
else
{
    builder.Services.AddScoped<IPaymentService, UnavailablePaymentService>();
}
builder.Services.AddScoped<IOrderNotificationService, SmtpOrderNotificationService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
// Konfiguracja uwierzytelniania opartego o Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            if (!int.TryParse(userId, out var parsedUserId) ||
                string.IsNullOrWhiteSpace(role) ||
                !UserRoles.All.Contains(role, StringComparer.Ordinal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<MyApp.Data.ApplicationDbContext>();
            var user = await dbContext.Users
                .AsNoTracking()
                .Where(candidate => candidate.Id == parsedUserId)
                .Select(candidate => new { candidate.Role })
                .SingleOrDefaultAsync();

            if (user is null || user.Role != role)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MyApp.Data.ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
    {
        await MyApp.Data.DemoDataSeeder.SeedAsync(
            dbContext,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
    }
}

// Konfiguracja potoku HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseSession();

// Middleware uwierzytelniania i autoryzacji 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program;