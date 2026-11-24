using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Data;
using MyApp.Models;
using MyApp.Services;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace AutoPart.Store.UatTests;

public class SalesReportsUatTests : IClassFixture<UatWebApplicationFactory>
{
    private readonly UatWebApplicationFactory _factory;

    public SalesReportsUatTests(UatWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Reports_require_administrator_role()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Admin/Reports");
        request.Headers.Add("X-Uat-Role", UserRoles.Client);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reports_show_paid_sales_and_exclude_cancelled_and_unpaid_orders()
    {
        using var client = _factory.CreateClient();
        await SeedReportOrdersAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Admin/Reports");
        request.Headers.Add("X-Uat-Role", UserRoles.Administrator);

        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Raporty sprzedaży", html);
        Assert.Contains("123,45", html);
        Assert.Contains("1</strong>", html);
    }

    [Fact]
    public async Task Five_failed_logins_temporarily_lock_the_account()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await SeedLoginAccountAsync();

        using var loginPage = await client.GetAsync("/Account/Login");
        var page = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")
            .Groups[1]
            .Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var response = await PostLoginAsync(client, token, "wrong-password");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users.SingleAsync(candidate => candidate.Email == "uat-login@example.test");
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.True(user.LockoutEndUtc > DateTime.UtcNow);
    }

    [Fact]
    public void Payment_simulator_is_not_available_outside_development()
    {
        using var scope = _factory.Services.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

        var result = paymentService.Process("4532781290431122", "08/28", "216", 100m);

        Assert.False(result.IsSuccessful);
        Assert.Null(result.TransactionId);
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string token, string password) =>
        client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "uat-login@example.test",
            ["Password"] = password,
            ["ReturnUrl"] = string.Empty,
            ["__RequestVerificationToken"] = token
        }));

    private async Task SeedLoginAccountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Users.AnyAsync(user => user.Email == "uat-login@example.test"))
        {
            return;
        }

        var user = new User
        {
            Email = "uat-login@example.test",
            FirstName = "UAT",
            LastName = "Login",
            Role = UserRoles.Client
        };
        user.PasswordHash = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>()
            .HashPassword(user, "correct-password");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedReportOrdersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Users.AnyAsync(user => user.Email == "uat-reports@example.test"))
        {
            return;
        }

        var user = new User
        {
            Email = "uat-reports@example.test",
            PasswordHash = "not-used",
            FirstName = "UAT",
            LastName = "Client",
            Role = UserRoles.Client
        };
        dbContext.Users.Add(user);
        dbContext.Orders.AddRange(
            new Order
            {
                User = user,
                OrderDate = DateTime.UtcNow,
                Status = "Nowe",
                PaymentStatus = "Opłacona",
                TotalAmount = 123.45m
            },
            new Order
            {
                User = user,
                OrderDate = DateTime.UtcNow,
                Status = "Anulowane",
                PaymentStatus = "Opłacona",
                TotalAmount = 900m
            },
            new Order
            {
                User = user,
                OrderDate = DateTime.UtcNow,
                Status = "Nowe",
                PaymentStatus = "Oczekująca",
                TotalAmount = 500m
            });
        await dbContext.SaveChangesAsync();
    }
}
