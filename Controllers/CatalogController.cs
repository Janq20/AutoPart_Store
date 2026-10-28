using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public CatalogController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? parentCategoryId,
            int? categoryId,
            decimal? minimumPrice,
            decimal? maximumPrice,
            bool inStockOnly = false)
        {
            if (minimumPrice < 0 || maximumPrice < 0 ||
                (minimumPrice.HasValue && maximumPrice.HasValue && minimumPrice > maximumPrice))
            {
                ModelState.AddModelError(string.Empty, "Zakres cen jest nieprawidłowy.");
            }

            var parentCategories = await _dbContext.Categories
                .AsNoTracking()
                .Where(category => category.ParentCategoryId == null)
                .OrderBy(category => category.Name)
                .ToListAsync();

            var validParentIds = parentCategories.Select(category => category.Id).ToHashSet();
            if (parentCategoryId.HasValue && !validParentIds.Contains(parentCategoryId.Value))
            {
                parentCategoryId = null;
                categoryId = null;
            }

            var categories = parentCategoryId.HasValue
                ? await _dbContext.Categories
                    .AsNoTracking()
                    .Where(category => category.ParentCategoryId == parentCategoryId.Value)
                    .OrderBy(category => category.Name)
                    .ToListAsync()
                : new List<Category>();
            var validCategoryIds = categories.Select(category => category.Id).ToHashSet();
            if (categoryId.HasValue && !validCategoryIds.Contains(categoryId.Value))
            {
                categoryId = null;
            }

            var productsQuery = _dbContext.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Where(product => product.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                productsQuery = productsQuery.Where(product =>
                    product.Name.Contains(term) ||
                    (product.Description != null && product.Description.Contains(term)));
            }

            if (parentCategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(product =>
                    product.Category != null &&
                    (product.Category.ParentCategoryId == parentCategoryId.Value ||
                     product.CategoryId == parentCategoryId.Value));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(product => product.CategoryId == categoryId.Value);
            }

            if (minimumPrice.HasValue && minimumPrice >= 0)
            {
                productsQuery = productsQuery.Where(product => product.Price >= minimumPrice.Value);
            }

            if (maximumPrice.HasValue && maximumPrice >= 0)
            {
                productsQuery = productsQuery.Where(product => product.Price <= maximumPrice.Value);
            }

            if (inStockOnly)
            {
                productsQuery = productsQuery.Where(product => product.StockQuantity > 0);
            }

            var model = new CatalogViewModel
            {
                Search = search?.Trim(),
                ParentCategoryId = parentCategoryId,
                SubcategoryId = categoryId,
                MinimumPrice = minimumPrice,
                MaximumPrice = maximumPrice,
                InStockOnly = inStockOnly,
                ParentCategories = parentCategories
                    .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                    .ToList(),
                Categories = categories
                    .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                    .ToList(),
                Products = await productsQuery.OrderBy(product => product.Name).ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Subcategories(int parentCategoryId)
        {
            if (!await _dbContext.Categories.AnyAsync(category =>
                    category.Id == parentCategoryId && category.ParentCategoryId == null))
            {
                return BadRequest("Nieprawidłowa kategoria nadrzędna.");
            }

            var categories = await _dbContext.Categories
                .AsNoTracking()
                .Where(category => category.ParentCategoryId == parentCategoryId)
                .OrderBy(category => category.Name)
                .Select(category => new { category.Id, category.Name })
                .ToListAsync();

            return Json(categories);
        }
    }
}
