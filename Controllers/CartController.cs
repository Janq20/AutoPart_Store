using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ShoppingCart";
        private readonly ApplicationDbContext _dbContext;
        private readonly IPaymentService _paymentService;
        private readonly IOrderNotificationService _notificationService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ApplicationDbContext dbContext,
            IPaymentService paymentService,
            IOrderNotificationService notificationService,
            ILogger<CartController> logger)
        {
            _dbContext = dbContext;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = await LoadCartAsync();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            if (quantity < 1)
            {
                TempData["CartError"] = "Ilość produktu musi wynosić co najmniej 1.";
                return RedirectToAction("Index", "Catalog");
            }

            var product = await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == productId && item.IsActive);
            if (product is null)
            {
                return NotFound();
            }

            if (product.StockQuantity == 0)
            {
                TempData["CartError"] = "Produkt jest obecnie niedostępny.";
                return RedirectToAction("Index", "Catalog");
            }

            var cart = ReadCart();
            cart.TryGetValue(productId, out var currentQuantity);
            if (quantity > product.StockQuantity - currentQuantity)
            {
                TempData["CartError"] = "Nie można dodać takiej ilości. Sprawdź aktualny stan magazynowy.";
                return RedirectToAction("Index", "Catalog");
            }

            cart[productId] = currentQuantity + quantity;
            SaveCart(cart);
            TempData["CartMessage"] = "Produkt dodano do koszyka.";
            return RedirectToAction("Index", "Catalog");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            var cart = ReadCart();
            if (!cart.ContainsKey(productId))
            {
                return RedirectToAction(nameof(Index));
            }

            if (quantity <= 0)
            {
                cart.Remove(productId);
                SaveCart(cart);
                return RedirectToAction(nameof(Index));
            }

            var product = await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == productId && item.IsActive);
            if (product is null)
            {
                cart.Remove(productId);
                SaveCart(cart);
                TempData["CartError"] = "Produkt nie jest już dostępny i został usunięty z koszyka.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity > product.StockQuantity)
            {
                TempData["CartError"] = $"Dostępna ilość produktu „{product.Name}” wynosi {product.StockQuantity}.";
                return RedirectToAction(nameof(Index));
            }

            cart[productId] = quantity;
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = ReadCart();
            cart.Remove(productId);
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            var addresses = await _dbContext.Addresses
                .AsNoTracking()
                .Where(address => address.UserId == userId)
                .OrderBy(address => address.City)
                .ToListAsync();
            ViewBag.Addresses = addresses;
            return View(new CheckoutViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = ReadCart();
            if (cart.Count == 0)
            {
                TempData["CartError"] = "Koszyk jest pusty.";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetUserId();
            if (userId is null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await PopulateCheckoutAddressesAsync(userId.Value);
                return View(model);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var products = await _dbContext.Products
                .AsNoTracking()
                .Where(product => cart.Keys.Contains(product.Id) && product.IsActive)
                .ToListAsync();

            if (products.Count != cart.Count)
            {
                TempData["CartError"] = "Niektóre produkty nie są już dostępne. Koszyk został zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }

            var unavailableProduct = products.FirstOrDefault(product =>
                !cart.TryGetValue(product.Id, out var quantity) ||
                quantity < 1 ||
                quantity > product.StockQuantity);
            if (unavailableProduct is not null)
            {
                TempData["CartError"] = $"Dostępna ilość produktu „{unavailableProduct.Name}” wynosi {unavailableProduct.StockQuantity}.";
                return RedirectToAction(nameof(Index));
            }

            if (!model.AddressId.HasValue &&
                (string.IsNullOrWhiteSpace(model.Street) ||
                 string.IsNullOrWhiteSpace(model.City) ||
                 string.IsNullOrWhiteSpace(model.PostalCode) ||
                 string.IsNullOrWhiteSpace(model.PhoneNumber)))
            {
                ModelState.AddModelError(string.Empty, "Podaj kompletny adres dostawy.");
                await PopulateCheckoutAddressesAsync(userId.Value);
                await transaction.RollbackAsync();
                return View(model);
            }

            var totalAmount = products.Sum(product => product.Price * cart[product.Id]);
            var payment = _paymentService.Process(model.CardNumber, model.Expiry, model.Cvv, totalAmount);
            if (!payment.IsSuccessful)
            {
                ModelState.AddModelError(string.Empty, payment.ErrorMessage ?? "Płatność nie powiodła się.");
                await PopulateCheckoutAddressesAsync(userId.Value);
                await transaction.RollbackAsync();
                return View(model);
            }

            Address? deliveryAddress = null;
            if (model.AddressId.HasValue)
            {
                deliveryAddress = await _dbContext.Addresses
                    .SingleOrDefaultAsync(address => address.Id == model.AddressId && address.UserId == userId);
                if (deliveryAddress is null)
                {
                    ModelState.AddModelError(nameof(model.AddressId), "Wybrany adres nie należy do tego konta.");
                    await PopulateCheckoutAddressesAsync(userId.Value);
                    await transaction.RollbackAsync();
                    return View(model);
                }
            }
            else
            {
                deliveryAddress = new Address
                {
                    UserId = userId.Value,
                    Street = model.Street.Trim(),
                    City = model.City.Trim(),
                    PostalCode = model.PostalCode.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim()
                };
                _dbContext.Addresses.Add(deliveryAddress);
            }

            foreach (var product in products)
            {
                var quantity = cart[product.Id];
                var updatedRows = await _dbContext.Products
                    .Where(item => item.Id == product.Id &&
                                   item.IsActive &&
                                   item.StockQuantity >= quantity)
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(item => item.StockQuantity, item => item.StockQuantity - quantity));

                if (updatedRows != 1)
                {
                    await transaction.RollbackAsync();
                    TempData["CartError"] = "Stan magazynowy właśnie się zmienił. Sprawdź koszyk i spróbuj ponownie.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var order = new Order
            {
                UserId = userId.Value,
                User = await _dbContext.Users.FindAsync(userId.Value),
                Status = "Nowe",
                PaymentStatus = "Opłacona",
                PaymentTransactionId = payment.TransactionId,
                InvoiceNumber = $"FV/{DateTime.UtcNow:yyyy}/{Guid.NewGuid():N}".Substring(0, 15),
                TotalAmount = totalAmount,
                DeliveryAddress = deliveryAddress,
                Items = products.Select(product => new OrderItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = cart[product.Id],
                    UnitPrice = product.Price
                }).ToList()
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            HttpContext.Session.Remove(CartSessionKey);
            try
            {
                await _notificationService.SendStatusChangedAsync(order, attachInvoice: true);
            }
            catch (System.Net.Mail.SmtpException exception)
            {
                _logger.LogWarning(exception, "Could not send order notification for order {OrderId}.", order.Id);
            }
            TempData["CartMessage"] = "Zakup został zarejestrowany. Koszyk jest już pusty.";
            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = GetUserId();
            var order = await _dbContext.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);
            return order is null ? NotFound() : View(order);
        }

        private int? GetUserId() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

        private async Task PopulateCheckoutAddressesAsync(int userId)
        {
            ViewBag.Addresses = await _dbContext.Addresses
                .AsNoTracking()
                .Where(address => address.UserId == userId)
                .OrderBy(address => address.City)
                .ToListAsync();
        }

        private Dictionary<int, int> ReadCart()
        {
            var serializedCart = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(serializedCart)
                ? new Dictionary<int, int>()
                : JsonSerializer.Deserialize<Dictionary<int, int>>(serializedCart) ?? new Dictionary<int, int>();
        }

        private void SaveCart(Dictionary<int, int> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        private async Task<CartViewModel> LoadCartAsync()
        {
            var cart = ReadCart();
            if (cart.Count == 0)
            {
                return new CartViewModel();
            }

            var products = await _dbContext.Products
                .AsNoTracking()
                .Where(product => cart.Keys.Contains(product.Id) && product.IsActive && product.StockQuantity > 0)
                .ToListAsync();
            var availableIds = products.Select(product => product.Id).ToHashSet();
            var quantityAdjusted = false;
            foreach (var removedId in cart.Keys.Where(id => !availableIds.Contains(id)).ToList())
            {
                cart.Remove(removedId);
                quantityAdjusted = true;
            }

            var adjustedItems = new List<CartItemViewModel>();
            foreach (var product in products)
            {
                var quantity = Math.Min(Math.Max(cart[product.Id], 1), product.StockQuantity);
                quantityAdjusted |= quantity != cart[product.Id];
                adjustedItems.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    AvailableStock = product.StockQuantity
                });
            }

            var model = new CartViewModel
            {
                Items = adjustedItems
                    .OrderBy(item => item.Name)
                    .ToList()
            };

            foreach (var item in model.Items)
            {
                cart[item.ProductId] = item.Quantity;
            }

            SaveCart(cart);
            if (quantityAdjusted && TempData["CartError"] is not string)
            {
                TempData["CartError"] = "Koszyk został zaktualizowany, ponieważ zmieniła się dostępność produktów.";
            }

            return model;
        }
    }
}
