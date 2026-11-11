using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Data;
using MyApp.Models;
using Xunit;

namespace AutoPart.Store.IntegrationTests;

public class OrderFlowTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public OrderFlowTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CheckoutReducesStockCreatesLedgerAndEmptiesCart()
    {
        await _factory.ResetDatabaseAsync(stock: 3);
        using var client = _factory.CreateAuthenticatedClient(UserRoles.Client);

        await AddToCartAsync(client, quantity: 2);
        var checkoutToken = await GetAntiforgeryTokenAsync(client, "/Cart");
        var response = await client.PostAsync(
            "/Cart/Checkout",
            Form(("__RequestVerificationToken", checkoutToken)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmation = await response.Content.ReadAsStringAsync();
        Assert.Contains("Koszyk jest pusty", confirmation);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = await db.Products.SingleAsync();
        var order = await db.Orders.Include(item => item.Items).SingleAsync();
        var movement = await db.InventoryTransactions.SingleAsync();

        Assert.Equal(1, product.StockQuantity);
        Assert.Equal(100m, order.TotalAmount);
        Assert.Equal(OrderStatuses.New, order.Status);
        Assert.Equal(2, order.Items.Single().Quantity);
        Assert.Equal(InventoryTransactionTypes.Sale, movement.Type);
        Assert.Equal(-2, movement.QuantityChange);
        Assert.Equal(3, movement.StockBefore);
        Assert.Equal(1, movement.StockAfter);
        Assert.Equal(order.Id, movement.OrderId);
    }

    [Fact]
    public async Task CheckoutRejectsQuantityNoLongerInStock()
    {
        await _factory.ResetDatabaseAsync(stock: 2);
        using var client = _factory.CreateAuthenticatedClient(UserRoles.Client);
        await AddToCartAsync(client, quantity: 2);
        var checkoutToken = await GetAntiforgeryTokenAsync(client, "/Cart");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.SingleAsync();
            product.StockQuantity = 1;
            await db.SaveChangesAsync();
        }

        await client.PostAsync(
            "/Cart/Checkout",
            Form(("__RequestVerificationToken", checkoutToken)));

        await using var assertScope = _factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, (await assertDb.Products.SingleAsync()).StockQuantity);
        Assert.Empty(await assertDb.Orders.ToListAsync());
        Assert.Empty(await assertDb.InventoryTransactions.ToListAsync());
    }

    [Fact]
    public async Task AddRejectsProductWithNoStock()
    {
        await _factory.ResetDatabaseAsync(stock: 0);
        using var client = _factory.CreateAuthenticatedClient(UserRoles.Client);
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync(
            "/Cart/Add",
            Form(("__RequestVerificationToken", token), ("productId", "1"), ("quantity", "1")));
        var catalog = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Niedostępny", catalog);
        var cart = await client.GetStringAsync("/Cart");
        Assert.Contains("Koszyk jest pusty", cart);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, (await db.Products.SingleAsync()).StockQuantity);
        Assert.Empty(await db.Orders.ToListAsync());
    }

    [Fact]
    public async Task StaffCanCancelOrderAndInventoryIsReturnedOnlyOnce()
    {
        await _factory.ResetDatabaseAsync(stock: 4);
        using var client = _factory.CreateAuthenticatedClient(UserRoles.Client);
        await AddToCartAsync(client, quantity: 2);
        var checkoutToken = await GetAntiforgeryTokenAsync(client, "/Cart");
        await client.PostAsync(
            "/Cart/Checkout",
            Form(("__RequestVerificationToken", checkoutToken)));

        using var staffClient = _factory.CreateAuthenticatedClient(UserRoles.Employee);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/Admin/Orders")).StatusCode);
        var detailsToken = await GetAntiforgeryTokenAsync(staffClient, "/Admin/OrderDetails/1");
        var cancelForm = Form(
            ("__RequestVerificationToken", detailsToken),
            ("id", "1"),
            ("status", OrderStatuses.Cancelled));

        Assert.Equal(HttpStatusCode.OK, (await staffClient.PostAsync("/Admin/UpdateOrderStatus", cancelForm)).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(4, (await db.Products.SingleAsync()).StockQuantity);
        Assert.Equal(OrderStatuses.Cancelled, (await db.Orders.SingleAsync()).Status);
        var movements = await db.InventoryTransactions.OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(2, movements.Count);
        Assert.Equal(-2, movements[0].QuantityChange);
        Assert.Equal(2, movements[1].QuantityChange);

        var secondToken = await GetAntiforgeryTokenAsync(staffClient, "/Admin/OrderDetails/1");
        await staffClient.PostAsync(
            "/Admin/UpdateOrderStatus",
            Form(("__RequestVerificationToken", secondToken), ("id", "1"), ("status", OrderStatuses.Cancelled)));
        Assert.Equal(2, await db.InventoryTransactions.CountAsync());
        Assert.Equal(4, (await db.Products.SingleAsync()).StockQuantity);
    }

    [Fact]
    public async Task ClientCannotOpenOrderManagementPanel()
    {
        await _factory.ResetDatabaseAsync(stock: 1);
        using var client = _factory.CreateAuthenticatedClient(UserRoles.Client);

        var response = await client.GetAsync("/Admin/Orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task AddToCartAsync(HttpClient client, int quantity)
    {
        var token = await GetAntiforgeryTokenAsync(client, "/Catalog");
        var response = await client.PostAsync(
            "/Cart/Add",
            Form(("__RequestVerificationToken", token), ("productId", "1"), ("quantity", quantity.ToString())));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.IgnoreCase);
        Assert.True(match.Success, $"Antiforgery token was not found on {path}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Key, field.Value)));
}

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public IntegrationTestFactory()
    {
        _connection.Open();
    }

    public HttpClient CreateAuthenticatedClient(string role, int userId = 1)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        return client;
    }

    public async Task ResetDatabaseAsync(int stock)
    {
        using var client = CreateClient();
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = "customer@example.test",
            FirstName = "Jan",
            LastName = "Testowy",
            PasswordHash = "not-used",
            Role = UserRoles.Client
        };
        var category = new Category { Name = "Testowe części" };
        db.Users.Add(user);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        db.Products.Add(new Product
        {
            Name = "Opona testowa",
            Price = 50m,
            StockQuantity = stock,
            CategoryId = category.Id
        });
        await db.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddSingleton(_connection);
            services.AddDbContext<ApplicationDbContext>((provider, options) =>
                options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthenticationHandler.TestScheme;
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.TestScheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.TestScheme;
                options.DefaultForbidScheme = TestAuthenticationHandler.TestScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.TestScheme,
                _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestScheme = "IntegrationTest";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User-Id", out var userId) ||
            !Request.Headers.TryGetValue("X-Test-Role", out var role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Użytkownik testowy"),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, TestScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), TestScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
