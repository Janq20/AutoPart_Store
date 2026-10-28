using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(typeof(decimal), "0.01", "999999999")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
