using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;
using System.Security.Claims;

namespace MyApp.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext dbContext, ILogger<AdminController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Administrator)]
        public async Task<IActionResult> Users()
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .OrderBy(user => user.Email)
                .Select(user => new UserRoleViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role
                })
                .ToListAsync();

            return View(new ManageUsersViewModel { Users = users });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Administrator)]
        public async Task<IActionResult> ChangeRole(int id, string role)
        {
            if (!UserRoles.All.Contains(role, StringComparer.Ordinal))
            {
                return BadRequest("Nieprawidłowa rola użytkownika.");
            }

            var user = await _dbContext.Users.FindAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            var roleChanged = user.Role != role;
            user.Role = role;
            await _dbContext.SaveChangesAsync();

            if (roleChanged &&
                User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString())
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            TempData["SuccessMessage"] = $"Rola użytkownika {user.Email} została zmieniona.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> Products()
        {
            var products = await _dbContext.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .ThenInclude(category => category!.ParentCategory)
                .OrderBy(product => product.Name)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> CreateProduct()
        {
            var model = new ProductFormViewModel();
            await PopulateProductCategoriesAsync(model);
            return View("ProductForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> CreateProduct(ProductFormViewModel model)
        {
            var mainCategoryExists = await _dbContext.Categories.AnyAsync(category =>
                category.Id == model.MainCategoryId &&
                category.ParentCategoryId == null);
            if (!mainCategoryExists)
            {
                ModelState.AddModelError(nameof(model.MainCategoryId), "Wybrana kategoria nie istnieje.");
            }

            var categoryId = model.MainCategoryId;
            if (model.SubcategoryId.HasValue)
            {
                var subcategoryExists = await _dbContext.Categories.AnyAsync(category =>
                    category.Id == model.SubcategoryId.Value &&
                    category.ParentCategoryId == model.MainCategoryId);
                if (!subcategoryExists)
                {
                    ModelState.AddModelError(nameof(model.SubcategoryId), "Wybierz podkategorię należącą do wybranej kategorii.");
                }
                else
                {
                    categoryId = model.SubcategoryId.Value;
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateProductCategoriesAsync(model);
                return View("ProductForm", model);
            }

            _dbContext.Products.Add(new Product
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                CategoryId = categoryId,
                IsActive = model.IsActive
            });
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Produkt został dodany.";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _dbContext.Products
                .Include(item => item.Category)
                .SingleOrDefaultAsync(item => item.Id == id);
            if (product is null)
            {
                return NotFound();
            }

            var model = new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                MainCategoryId = product.Category?.ParentCategoryId ?? product.CategoryId,
                SubcategoryId = product.Category?.ParentCategoryId.HasValue == true
                    ? product.CategoryId
                    : null,
                IsActive = product.IsActive
            };
            await PopulateProductCategoriesAsync(model);
            return View("ProductForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> EditProduct(int id, ProductFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var mainCategoryExists = await _dbContext.Categories.AnyAsync(category =>
                category.Id == model.MainCategoryId &&
                category.ParentCategoryId == null);
            if (!mainCategoryExists)
            {
                ModelState.AddModelError(nameof(model.MainCategoryId), "Wybrana kategoria nie istnieje.");
            }

            var categoryId = model.MainCategoryId;
            if (model.SubcategoryId.HasValue)
            {
                var subcategoryExists = await _dbContext.Categories.AnyAsync(category =>
                    category.Id == model.SubcategoryId.Value &&
                    category.ParentCategoryId == model.MainCategoryId);
                if (!subcategoryExists)
                {
                    ModelState.AddModelError(nameof(model.SubcategoryId), "Wybierz podkategorię należącą do wybranej kategorii.");
                }
                else
                {
                    categoryId = model.SubcategoryId.Value;
                }
            }

            var product = await _dbContext.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateProductCategoriesAsync(model);
                return View("ProductForm", model);
            }

            product.Name = model.Name.Trim();
            product.Description = model.Description?.Trim();
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.CategoryId = categoryId;
            product.IsActive = model.IsActive;

            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Produkt został zaktualizowany.";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> ToggleProduct(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product is null)
            {
                return NotFound();
            }

            product.IsActive = !product.IsActive;
            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = product.IsActive
                ? "Produkt ponownie pojawia się w katalogu."
                : "Produkt został ukryty w katalogu.";
            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> Categories()
        {
            var categories = await _dbContext.Categories
                .AsNoTracking()
                .Include(category => category.ParentCategory)
                .OrderBy(category => category.ParentCategoryId)
                .ThenBy(category => category.Name)
                .ToListAsync();
            return View(categories);
        }

        [HttpGet]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> CreateCategory()
        {
            var model = new CategoryFormViewModel();
            await PopulateParentCategoriesAsync(model);
            return View("CategoryForm", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> CreateCategory(CategoryFormViewModel model)
        {
            if (model.ParentCategoryId.HasValue &&
                !await _dbContext.Categories.AnyAsync(category =>
                    category.Id == model.ParentCategoryId &&
                    category.ParentCategoryId == null))
            {
                ModelState.AddModelError(nameof(model.ParentCategoryId), "Wybierz istniejącą kategorię nadrzędną.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateParentCategoriesAsync(model);
                return View("CategoryForm", model);
            }

            _dbContext.Categories.Add(new Category
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                ParentCategoryId = model.ParentCategoryId
            });

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Failed to create catalog category {CategoryName}.", model.Name);
                ModelState.AddModelError(nameof(model.Name), "Kategoria o takiej nazwie już istnieje lub nie można jej zapisać.");
                await PopulateParentCategoriesAsync(model);
                return View("CategoryForm", model);
            }

            TempData["SuccessMessage"] = "Kategoria została dodana.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Administrator + "," + UserRoles.Employee)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _dbContext.Categories.FindAsync(id);
            if (category is null)
            {
                return NotFound();
            }

            if (await _dbContext.Products.AnyAsync(product => product.CategoryId == id) ||
                await _dbContext.Categories.AnyAsync(child => child.ParentCategoryId == id))
            {
                TempData["CategoryError"] = "Nie można usunąć kategorii, która zawiera produkty lub podkategorie.";
                return RedirectToAction(nameof(Categories));
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Kategoria została usunięta.";
            return RedirectToAction(nameof(Categories));
        }

        private async Task PopulateProductCategoriesAsync(ProductFormViewModel model)
        {
            var categories = await _dbContext.Categories
                .AsNoTracking()
                .Where(category => category.ParentCategoryId == null)
                .OrderBy(category => category.Name)
                .ToListAsync();

            model.Categories = categories
                .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                .ToList();

            model.Subcategories = model.MainCategoryId > 0
                ? await _dbContext.Categories
                    .AsNoTracking()
                    .Where(category => category.ParentCategoryId == model.MainCategoryId)
                    .OrderBy(category => category.Name)
                    .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                    .ToListAsync()
                : new List<SelectListItem>();
        }

        private async Task PopulateParentCategoriesAsync(CategoryFormViewModel model)
        {
            model.ParentCategories = await _dbContext.Categories
                .AsNoTracking()
                .Where(category => category.ParentCategoryId == null)
                .OrderBy(category => category.Name)
                .Select(category => new SelectListItem(category.Name, category.Id.ToString()))
                .ToListAsync();
        }
    }
}
