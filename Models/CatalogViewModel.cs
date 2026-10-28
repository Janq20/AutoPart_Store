using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyApp.Models
{
    public class CatalogViewModel
    {
        public string? Search { get; set; }
        public int? ParentCategoryId { get; set; }
        public int? SubcategoryId { get; set; }
        public decimal? MinimumPrice { get; set; }
        public decimal? MaximumPrice { get; set; }
        public bool InStockOnly { get; set; }
        public List<SelectListItem> ParentCategories { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}
