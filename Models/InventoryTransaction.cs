using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int? OrderId { get; set; }
        public Order? Order { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required, MaxLength(30)]
        public string Type { get; set; } = InventoryTransactionTypes.Adjustment;

        public int QuantityChange { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }

        [Required, MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class InventoryTransactionTypes
    {
        public const string InitialStock = "Stan początkowy";
        public const string Adjustment = "Korekta";
        public const string Sale = "Sprzedaż";
        public const string OrderCancellation = "Anulowanie zamówienia";
    }
}
