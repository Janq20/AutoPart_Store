using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Data
{
    public static class DemoDataSeeder
    {
        private const string DemoPassword = "AutoPartDemo!2026";
        private const string OrderMarker = "SEED-BULK-V2-";
        private const int DemoOrderCount = 438;

        private static readonly (string Name, (string Name, decimal Price)[] Products)[] Catalog =
        [
            ("Hamulce",
            [
                ("Klocki hamulcowe Bosch", 159.90m), ("Tarcza hamulcowa Bosch", 149.90m),
                ("Szczęki hamulcowe Bosch", 89.90m), ("Zacisk hamulcowy TRW", 449.90m),
                ("Pompa hamulcowa ATE", 319.90m), ("Płyn hamulcowy Bosch DOT 4", 29.90m),
                ("Czujnik ABS Bosch", 137.98m), ("Przewód hamulcowy ATE", 59.90m),
                ("Bęben hamulcowy Bosch", 119.90m), ("Zestaw naprawczy hamulca TRW", 79.90m)
            ]),
            ("Filtry",
            [
                ("Filtr oleju Bosch", 34.44m), ("Filtr kabinowy Bosch", 49.90m),
                ("Filtr powietrza Bosch", 35.00m), ("Filtr paliwa Bosch", 59.90m),
                ("Filtr sportowy K&N", 249.90m), ("Wkład filtra oleju MANN-FILTER", 39.90m),
                ("Obudowa filtra powietrza Mahle", 189.90m), ("Filtr LPG BRC", 39.90m),
                ("Separator oleju MANN-FILTER", 133.79m), ("Zestaw filtrów Bosch", 220.87m)
            ]),
            ("Układ napędowy",
            [
                ("Pasek wielorowkowy Gates", 49.90m), ("Zestaw paska rozrządu INA", 269.90m),
                ("Łańcuch rozrządu Febi Bilstein", 799.90m), ("Rolka napinacza INA", 79.90m),
                ("Pompa sprzęgła Sachs", 189.90m), ("Przegub napędowy SKF", 189.90m),
                ("Półoś napędowa SKF", 499.90m), ("Poduszka skrzyni biegów Lemförder", 119.90m),
                ("Koło pasowe Corteco", 299.90m), ("Zestaw sprzęgła Sachs", 599.90m)
            ]),
            ("Oświetlenie",
            [
                ("Żarówka Bosch Pure Light H7", 9.99m), ("Żarówka Bosch Pure Light H4", 14.99m),
                ("Lampa przednia Valeo", 789.90m), ("Lampa tylna Valeo", 249.90m),
                ("Kierunkowskaz Hella", 49.90m), ("Lampa przeciwmgielna Hella", 119.90m),
                ("Moduł LED Hella", 299.90m), ("Oprawa żarówki Bosch", 29.90m),
                ("Lampa oświetlenia tablicy Hella", 39.90m), ("Zestaw żarówek Osram", 39.90m)
            ]),
            ("Zawieszenie",
            [
                ("Amortyzator Bilstein", 275.00m), ("Sprężyna zawieszenia Sachs", 179.90m),
                ("Wahacz Lemförder", 289.90m), ("Łącznik stabilizatora Febi Bilstein", 59.90m),
                ("Tuleja wahacza Lemförder", 39.90m), ("Sworzeń wahacza TRW", 99.90m),
                ("Łożysko koła SKF", 179.90m), ("Piasta koła SKF", 299.90m),
                ("Górne mocowanie amortyzatora Sachs", 99.90m), ("Zestaw zawieszenia Meyle", 499.90m)
            ]),
            ("Silnik",
            [
                ("Świece zapłonowe Bosch Super Plus", 45.00m), ("Cewka zapłonowa Bosch", 129.90m),
                ("Pompa oleju Febi Bilstein", 349.90m), ("Uszczelka pokrywy zaworów Elring", 49.90m),
                ("Termostat Mahle", 89.90m), ("Czujnik temperatury Bosch", 39.90m),
                ("Zawór EGR Pierburg", 499.90m), ("Przepływomierz Bosch", 299.90m),
                ("Poduszka silnika Lemförder", 189.90m), ("Zestaw uszczelek silnika Elring", 119.90m)
            ]),
            ("Elektryka",
            [
                ("Akumulator Bosch S4", 333.53m), ("Alternator Bosch regenerowany", 450.00m),
                ("Rozrusznik Bosch regenerowany", 440.00m), ("Bezpiecznik samochodowy Bosch", 4.99m),
                ("Przekaźnik samochodowy Hella", 29.90m), ("Czujnik parkowania Bosch", 119.90m),
                ("Czujnik wału korbowego Bosch", 89.90m), ("Klema akumulatora Bosch", 19.90m),
                ("Przewody zapłonowe Bosch", 99.90m), ("Moduł sterujący Bosch", 799.90m)
            ]),
            ("Chłodzenie",
            [
                ("Chłodnica silnika Mahle", 429.90m), ("Wentylator chłodnicy NRF", 299.90m),
                ("Pompa wody SKF", 149.90m), ("Przewód chłodnicy Gates", 49.90m),
                ("Zbiornik wyrównawczy Febi Bilstein", 69.90m), ("Chłodnica oleju Mahle", 249.90m),
                ("Korek chłodnicy Febi Bilstein", 19.90m), ("Nagrzewnica Valeo", 249.90m),
                ("Płyn chłodniczy Glysantin", 59.90m), ("Zestaw napędu pompy wody Gates", 249.90m)
            ]),
            ("Układ wydechowy",
            [
                ("Tłumik końcowy Bosal", 489.90m), ("Tłumik środkowy Walker", 399.90m),
                ("Sonda lambda Bosch", 214.49m), ("Katalizator Walker", 1799.90m),
                ("Filtr cząstek stałych BM Catalysts", 2199.90m), ("Uszczelka wydechu Elring", 19.90m),
                ("Obejma wydechu Bosal", 29.90m), ("Łącznik elastyczny wydechu Bosal", 39.90m),
                ("Czujnik NOx Bosch", 799.90m), ("Zestaw montażowy wydechu Bosal", 79.90m)
            ]),
            ("Oleje i płyny",
            [
                ("Olej silnikowy Motul 5W-40", 199.00m), ("Olej silnikowy Motul 5W-30", 189.00m),
                ("Olej przekładniowy Motul", 39.90m), ("Płyn hamulcowy ATE DOT 4", 29.90m),
                ("Koncentrat chłodniczy Glysantin", 74.90m), ("Płyn do spryskiwaczy Sonax", 19.90m),
                ("Dodatek do paliwa Liqui Moly", 49.90m), ("Smar uniwersalny Liqui Moly", 29.90m),
                ("Olej do wspomagania Febi Bilstein", 44.90m), ("Zestaw serwisowy filtrów Bosch", 220.87m)
            ]),
            ("Nadwozie",
            [
                ("Lusterko boczne Febi Bilstein", 159.90m), ("Wycieraczki Bosch Aerotwin", 50.69m),
                ("Pióro wycieraczki Bosch Aerotwin", 28.90m), ("Zderzak Van Wezel", 499.90m),
                ("Błotnik Van Wezel", 399.90m), ("Klamka drzwi Febi Bilstein", 59.90m),
                ("Siłownik klapy Febi Bilstein", 119.90m), ("Mata bagażnika Frogum", 99.90m),
                ("Chlapacze Frogum", 49.90m), ("Zestaw mocowań nadwozia Romix", 19.90m)
            ]),
            ("Akcesoria",
            [
                ("Dywaniki samochodowe Frogum", 149.90m), ("Uchwyt telefonu Baseus", 49.90m),
                ("Ładowarka samochodowa Baseus", 39.90m), ("Apteczka samochodowa Vera", 24.90m),
                ("Trójkąt ostrzegawczy Michelin", 29.90m), ("Kompresor samochodowy Michelin", 129.90m),
                ("Organizer bagażnika Bottari", 39.90m), ("Pokrowiec ochronny Kegel-Błażusiak", 129.90m),
                ("Zestaw narzędzi Yato", 99.90m), ("Kamera samochodowa 70mai", 249.90m)
            ])
        ];

        private static readonly string[] LegacyBrands = ["Bosch", "Mann", "Febi", "Valeo", "Motul"];
        private static readonly string[] FirstNames = ["Anna", "Marek", "Ola", "Piotr", "Kasia", "Tomasz", "Maja", "Jakub", "Natalia", "Paweł", "Zuzanna", "Michał", "Alicja", "Kamil", "Julia", "Filip", "Ewa", "Adam", "Lena", "Bartosz"];
        private static readonly string[] LastNames = ["Nowak", "Kowalski", "Wiśniewska", "Wójcik", "Kaczmarek", "Mazur", "Krawczyk", "Zielińska", "Szymański", "Woźniak", "Dąbrowska", "Kozłowski", "Jankowska", "Wojciechowski", "Kwiatkowska", "Kaczmarek", "Piotrowska", "Grabowski", "Pawłowska", "Michalski"];
        private static readonly (string City, string PostalCode)[] Cities =
        [
            ("Nowy Sącz", "33-300"), ("Kraków", "30-001"), ("Tarnów", "33-100"),
            ("Rzeszów", "35-001"), ("Katowice", "40-001"), ("Łódź", "90-001"),
            ("Warszawa", "00-001"), ("Wrocław", "50-001"), ("Lublin", "20-001"),
            ("Bielsko-Biała", "43-300")
        ];

        public static async Task SeedAsync(
            ApplicationDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await NormalizeExistingSeedDataAsync(dbContext, cancellationToken);
            if (await dbContext.Orders.AnyAsync(
                    order => order.PaymentTransactionId != null &&
                             order.PaymentTransactionId.StartsWith(OrderMarker),
                    cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var adminUsers = new List<User>
            {
                await FindOrCreateUserAsync(dbContext, passwordHasher, "admin@autopart.test", "Jan", "Administrator", UserRoles.Administrator, cancellationToken),
                await FindOrCreateUserAsync(dbContext, passwordHasher, "admin2@autopart.test", "Aleksandra", "Admin", UserRoles.Administrator, cancellationToken)
            };
            var employees = new List<User>();
            for (var index = 1; index <= 8; index++)
            {
                employees.Add(await FindOrCreateUserAsync(
                    dbContext,
                    passwordHasher,
                    $"pracownik{index:00}@autopart.test",
                    FirstNames[index],
                    LastNames[index],
                    UserRoles.Employee,
                    cancellationToken));
            }

            var customers = new List<User>
            {
                await FindOrCreateUserAsync(dbContext, passwordHasher, "klient1@autopart.test", "Anna", "Nowak", UserRoles.Client, cancellationToken),
                await FindOrCreateUserAsync(dbContext, passwordHasher, "klient2@autopart.test", "Marek", "Kowalski", UserRoles.Client, cancellationToken),
                await FindOrCreateUserAsync(dbContext, passwordHasher, "klient3@autopart.test", "Ola", "Wiśniewska", UserRoles.Client, cancellationToken)
            };
            for (var index = 4; index <= 40; index++)
            {
                customers.Add(await FindOrCreateUserAsync(
                    dbContext,
                    passwordHasher,
                    $"klient{index:00}@autopart.test",
                    FirstNames[index % FirstNames.Length],
                    LastNames[index % LastNames.Length],
                    UserRoles.Client,
                    cancellationToken));
            }

            var allDemoUsers = adminUsers.Concat(employees).Concat(customers).ToList();
            foreach (var user in allDemoUsers)
            {
                var existingAddress = await dbContext.Addresses.AnyAsync(
                    address => address.UserId == user.Id, cancellationToken);
                if (!existingAddress)
                {
                    var addressDetails = Cities[user.Id % Cities.Length];
                    dbContext.Addresses.Add(new Address
                    {
                        User = user,
                        Street = $"ul. Przykładowa {user.Id % 90 + 1}",
                        City = addressDetails.City,
                        PostalCode = addressDetails.PostalCode,
                        PhoneNumber = $"500{user.Id % 1000000:000000}"
                    });
                }
            }

            var categoryNames = Catalog.Select(item => item.Name).ToArray();
            var categories = await dbContext.Categories
                .Where(category => categoryNames.Contains(category.Name))
                .ToDictionaryAsync(category => category.Name, cancellationToken);
            foreach (var definition in Catalog)
            {
                if (!categories.ContainsKey(definition.Name))
                {
                    var category = new Category
                    {
                        Name = definition.Name,
                        Description = $"Części samochodowe: {definition.Name}."
                    };
                    dbContext.Categories.Add(category);
                    categories.Add(definition.Name, category);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var productNames = Catalog
                .SelectMany(category => category.Products.Select((_, index) =>
                    ProductName(category.Name, index)))
                .ToArray();
            var existingProducts = await dbContext.Products
                .Where(product => productNames.Contains(product.Name))
                .ToDictionaryAsync(product => product.Name, cancellationToken);
            var products = new List<Product>();
            foreach (var category in Catalog)
            {
                for (var index = 0; index < category.Products.Length; index++)
                {
                    var name = ProductName(category.Name, index);
                    if (!existingProducts.TryGetValue(name, out var product))
                    {
                        product = new Product
                        {
                            Name = name,
                            Description = $"Produkt z kategorii {category.Name}. Przed zakupem sprawdź kompatybilność z pojazdem.",
                            Price = category.Products[index].Price,
                            StockQuantity = 100,
                            Category = categories[category.Name],
                            IsActive = true
                        };
                        dbContext.Products.Add(product);
                    }
                    else
                    {
                        product.Description = $"Produkt z kategorii {category.Name}. Przed zakupem sprawdź kompatybilność z pojazdem.";
                        product.Price = category.Products[index].Price;
                    }
                    products.Add(product);
                }
            }

            var legacySeededProducts = await dbContext.Products
                .Where(product =>
                    product.Name == "Klocki hamulcowe" ||
                    product.Name == "Filtr oleju" ||
                    product.Name == "Pasek napędowy" ||
                    product.Name == "Żarówka H7" ||
                    product.Name == "Filtr kabinowy")
                .ToListAsync(cancellationToken);
            products.AddRange(legacySeededProducts);
            await dbContext.SaveChangesAsync(cancellationToken);

            var addressByCustomer = new Dictionary<int, Address>();
            foreach (var customer in customers)
            {
                addressByCustomer[customer.Id] = await dbContext.Addresses
                    .Where(address => address.UserId == customer.Id)
                    .OrderBy(address => address.Id)
                    .FirstAsync(cancellationToken);
            }

            var random = new Random(20261124);
            var now = DateTime.UtcNow;
            for (var orderIndex = 1; orderIndex <= DemoOrderCount; orderIndex++)
            {
                var customer = customers[random.Next(customers.Count)];
                var daysAgo = (int)(Math.Pow(random.NextDouble(), 1.7) * 181);
                var isCancelled = random.NextDouble() < 0.045;
                var isPaid = isCancelled || random.NextDouble() < 0.94;
                var items = new List<OrderItem>();
                var selectedProducts = new HashSet<int>();
                var itemCount = random.Next(1, 5);
                for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    var availableProducts = products
                        .Where(product => !selectedProducts.Contains(product.Id) &&
                                          (!isPaid || isCancelled || product.StockQuantity > 0))
                        .ToList();
                    if (availableProducts.Count == 0)
                    {
                        break;
                    }

                    var product = availableProducts[random.Next(availableProducts.Count)];
                    var quantity = random.Next(1, 4);
                    if (isPaid && !isCancelled)
                    {
                        quantity = Math.Min(quantity, product.StockQuantity);
                        product.StockQuantity -= quantity;
                    }

                    selectedProducts.Add(product.Id);
                    items.Add(new OrderItem
                    {
                        Product = product,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    });
                }

                if (items.Count == 0)
                {
                    continue;
                }

                var orderNumber = orderIndex.ToString("0000");
                dbContext.Orders.Add(new Order
                {
                    User = customer,
                    DeliveryAddress = addressByCustomer[customer.Id],
                    OrderDate = now.AddDays(-daysAgo).AddMinutes(random.Next(1440)),
                    Status = isCancelled
                        ? "Anulowane"
                        : isPaid
                            ? new[] { "Nowe", "W trakcie realizacji", "Wysłane", "Zrealizowane" }[random.Next(4)]
                            : "Nowe",
                    PaymentStatus = isPaid ? "Opłacona" : "Oczekująca",
                    PaymentTransactionId = $"{OrderMarker}{orderNumber}",
                    InvoiceNumber = isPaid ? $"FV/{now:yyyy}/{orderNumber}" : null,
                    TotalAmount = items.Sum(item => item.UnitPrice * item.Quantity),
                    Items = items
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        private static string ProductName(string category, int index) =>
            Catalog.Single(item => item.Name == category).Products[index].Name;

        private static async Task NormalizeExistingSeedDataAsync(
            ApplicationDbContext dbContext,
            CancellationToken cancellationToken)
        {
            var oldProducts = await dbContext.Products
                .Where(product => product.Name.StartsWith("DEMO - "))
                .ToListAsync(cancellationToken);
            foreach (var product in oldProducts)
            {
                product.Name = product.Name["DEMO - ".Length..];
            }

            var oldCategories = await dbContext.Categories
                .Where(category => category.Name.StartsWith("DEMO - "))
                .ToListAsync(cancellationToken);
            foreach (var oldCategory in oldCategories)
            {
                var name = oldCategory.Name["DEMO - ".Length..];
                var category = await dbContext.Categories
                    .SingleOrDefaultAsync(item => item.Name == name, cancellationToken);
                if (category is null)
                {
                    oldCategory.Name = name;
                    oldCategory.Description = $"Części samochodowe: {name}.";
                    continue;
                }

                var productsInMergedCategory = await dbContext.Products
                    .Where(product => product.CategoryId == oldCategory.Id)
                    .ToListAsync(cancellationToken);
                foreach (var product in productsInMergedCategory)
                {
                    product.CategoryId = category.Id;
                }

                var childCategories = await dbContext.Categories
                    .Where(child => child.ParentCategoryId == oldCategory.Id)
                    .ToListAsync(cancellationToken);
                foreach (var childCategory in childCategories)
                {
                    childCategory.ParentCategoryId = category.Id;
                }

                dbContext.Categories.Remove(oldCategory);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            var categoryNames = Catalog.Select(item => item.Name).ToArray();
            var categories = await dbContext.Categories
                .Where(category => categoryNames.Contains(category.Name))
                .ToDictionaryAsync(category => category.Name, cancellationToken);
            var categoryIds = categories.Values.Select(category => category.Id).ToArray();
            var products = await dbContext.Products
                .Where(product => categoryIds.Contains(product.CategoryId))
                .ToListAsync(cancellationToken);
            foreach (var categoryDefinition in Catalog)
            {
                if (!categories.TryGetValue(categoryDefinition.Name, out var category))
                {
                    continue;
                }

                var categoryId = category.Id;
                for (var index = 0; index < categoryDefinition.Products.Length; index++)
                {
                    var legacySuffix = $" {LegacyBrands[index % LegacyBrands.Length]} {index + 1:00}";
                    var legacyProduct = products.FirstOrDefault(product =>
                        product.CategoryId == categoryId &&
                        product.Name.EndsWith(legacySuffix, StringComparison.Ordinal));
                    if (legacyProduct is null)
                    {
                        continue;
                    }

                    legacyProduct.Name = categoryDefinition.Products[index].Name;
                    legacyProduct.Price = categoryDefinition.Products[index].Price;
                    legacyProduct.Description =
                        $"Produkt z kategorii {categoryDefinition.Name}. Przed zakupem sprawdź kompatybilność z pojazdem.";
                }
            }

            var generatedCategories = await dbContext.Categories
                .Where(category => category.Description != null)
                .ToListAsync(cancellationToken);
            foreach (var category in generatedCategories.Where(item =>
                         item.Description!.Contains("demonstr", StringComparison.OrdinalIgnoreCase)))
            {
                category.Description = $"Części samochodowe: {category.Name}.";
            }

            var administrator = await dbContext.Users
                .SingleOrDefaultAsync(user => user.Email == "admin@autopart.test", cancellationToken);
            if (administrator?.FirstName == "Demo")
            {
                administrator.FirstName = "Jan";
            }

            var oldSeedOrders = await dbContext.Orders
                .Where(order => order.PaymentTransactionId != null &&
                                order.PaymentTransactionId.StartsWith("SIM-DEMO-"))
                .ToListAsync(cancellationToken);
            foreach (var order in oldSeedOrders)
            {
                if (order.PaymentTransactionId!.StartsWith("SIM-DEMO-BULK-V2-", StringComparison.Ordinal))
                {
                    var sequence = order.PaymentTransactionId["SIM-DEMO-BULK-V2-".Length..];
                    order.PaymentTransactionId = $"{OrderMarker}{sequence}";
                }
                else if (order.PaymentTransactionId.StartsWith("SIM-DEMO-", StringComparison.Ordinal))
                {
                    order.PaymentTransactionId = $"SIM-{order.PaymentTransactionId["SIM-DEMO-".Length..]}";
                }

                if (order.InvoiceNumber?.StartsWith("DEMO/FV/", StringComparison.Ordinal) == true)
                {
                    order.InvoiceNumber = $"FV/{order.InvoiceNumber["DEMO/FV/".Length..]}";
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        private static async Task<User> FindOrCreateUserAsync(
            ApplicationDbContext dbContext,
            IPasswordHasher<User> passwordHasher,
            string email,
            string firstName,
            string lastName,
            string role,
            CancellationToken cancellationToken)
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(
                item => item.Email == email, cancellationToken);
            if (user is not null)
            {
                return user;
            }

            user = new User
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role
            };
            user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
            dbContext.Users.Add(user);
            return user;
        }
    }
}
