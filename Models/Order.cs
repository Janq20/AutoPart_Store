using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Nowe";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int? DeliveryAddressId { get; set; }
        public Address? DeliveryAddress { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
