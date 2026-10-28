using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountController> logger)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            if (await _dbContext.Users.AnyAsync(user => user.Email == email))
            {
                ModelState.AddModelError(nameof(model.Email), "Konto z tym adresem e-mail już istnieje.");
                return View(model);
            }

            var user = new User
            {
                Email = email,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Role = UserRoles.Client
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _dbContext.Users.Add(user);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Failed to create an account for {Email}.", email);
                ModelState.AddModelError(nameof(model.Email), "Nie udało się utworzyć konta. Sprawdź, czy adres e-mail nie jest już używany.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Konto zostało pomyślnie utworzone. Możesz się zalogować.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null) =>
            View(new LoginViewModel { ReturnUrl = returnUrl });

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Email == email);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Nieprawidłowy adres e-mail lub hasło.");
                return View(model);
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Nieprawidłowy adres e-mail lub hasło.");
                return View(model);
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                await _dbContext.SaveChangesAsync();
            }

            if (!UserRoles.All.Contains(user.Role, StringComparer.Ordinal))
            {
                ModelState.AddModelError(string.Empty, "Konto ma nieprawidłową rolę. Skontaktuj się z administratorem.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}