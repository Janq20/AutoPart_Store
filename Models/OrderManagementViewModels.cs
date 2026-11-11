namespace MyApp.Models
{
    public class ManageOrdersViewModel
    {
        public List<OrderListItemViewModel> Orders { get; set; } = new();
    }

    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailsViewModel
    {
        public Order Order { get; set; } = new();
        public List<InventoryTransaction> InventoryTransactions { get; set; } = new();
    }
}
