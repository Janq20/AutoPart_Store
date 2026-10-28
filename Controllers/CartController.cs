using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ShoppingCart";
        private readonly ApplicationDbContext _dbContext;

        public CartController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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
            if (quantityAdjusted)
            {
                TempData["CartError"] = "Koszyk został zaktualizowany, ponieważ zmieniła się dostępność produktów.";
            }

            return model;
        }
    }
}
