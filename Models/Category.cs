using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }

        public List<Category> Subcategories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}
