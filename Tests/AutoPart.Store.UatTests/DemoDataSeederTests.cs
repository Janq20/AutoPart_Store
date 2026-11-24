using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace AutoPart.Store.UatTests;

public class DemoDataSeederTests
{
    [Fact]
    public async Task Seed_is_idempotent_and_creates_demo_purchases()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        var passwordHasher = new PasswordHasher<User>();

        await DemoDataSeeder.SeedAsync(dbContext, passwordHasher);
        await DemoDataSeeder.SeedAsync(dbContext, passwordHasher);

        Assert.Equal(438, await dbContext.Orders.CountAsync(order =>
            order.PaymentTransactionId != null &&
            order.PaymentTransactionId.StartsWith("SEED-BULK-V2-")));
        Assert.Equal(50, await dbContext.Users.CountAsync(user =>
            user.Email.EndsWith("@autopart.test")));
        var seededProducts = await dbContext.Products.ToListAsync();
        Assert.Equal(120, seededProducts.Count);
        Assert.Equal(120, seededProducts.Select(product => product.Name).Distinct().Count());
        Assert.DoesNotContain(seededProducts, product =>
        {
            var suffix = product.Name.Split(' ').Last();
            return suffix.Length == 2 && suffix.All(char.IsDigit);
        });
        Assert.Contains(seededProducts, product =>
            product.Name == "Żarówka Bosch Pure Light H7" && product.Price == 9.99m);
        Assert.Contains(seededProducts, product =>
            product.Name == "Akumulator Bosch S4" && product.Price == 333.53m);
        Assert.DoesNotContain(seededProducts, product => product.Name.Contains("DEMO"));
        Assert.Empty(await dbContext.Categories
            .Where(category => category.Name.Contains("DEMO"))
            .ToListAsync());
        Assert.All(
            await dbContext.Orders
                .Where(order => order.PaymentTransactionId!.StartsWith("SEED-BULK-V2-"))
                .ToListAsync(),
            order => Assert.True(order.PaymentStatus is "Opłacona" or "Oczekująca"));
        Assert.Equal(2, await dbContext.Users.CountAsync(user =>
            user.Email.EndsWith("@autopart.test") && user.Role == UserRoles.Administrator));
        Assert.Equal(8, await dbContext.Users.CountAsync(user =>
            user.Email.EndsWith("@autopart.test") && user.Role == UserRoles.Employee));
        Assert.Equal(40, await dbContext.Users.CountAsync(user =>
            user.Email.EndsWith("@autopart.test") && user.Role == UserRoles.Client));
    }
}
