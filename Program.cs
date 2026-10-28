using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using MyApp.Models;

var builder = WebApplication.CreateBuilder(args);
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

// Konfiguracja potoku HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Middleware uwierzytelniania i autoryzacji 
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();