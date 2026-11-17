namespace MyApp.Models
{
    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? InvoiceNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderDetailsItemViewModel> Items { get; set; } = new();
    }

    public class OrderDetailsItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CheckoutViewModel
    {
        public int? AddressId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^[\d ]{16,19}$", ErrorMessage = "Numer karty musi mieć 16 cyfr.")]
        public string CardNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Podaj datę w formacie MM/YY.")]
        public string Expiry { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^\d{3}$", ErrorMessage = "Kod CVV musi mieć 3 cyfry.")]
        public string Cvv { get; set; } = string.Empty;
    }

    public static class OrderStatuses
    {
        public static readonly string[] All = ["Nowe", "W trakcie realizacji", "Wysłane", "Zrealizowane", "Anulowane"];
    }
}
