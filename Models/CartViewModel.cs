namespace MyApp.Models
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total => Items.Sum(item => item.LineTotal);
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
