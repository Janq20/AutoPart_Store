using System.Globalization;
using Microsoft.AspNetCore.Hosting;

namespace MyApp.Services
{
    public class VirtualPaymentService : IPaymentService
    {
        private readonly IWebHostEnvironment _environment;

        public VirtualPaymentService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public PaymentResult Process(string cardNumber, string expiry, string cvv, decimal amount)
        {
            if (!_environment.IsDevelopment())
            {
                return new PaymentResult(false, null, "Symulator płatności jest niedostępny w środowisku produkcyjnym.");
            }

            cardNumber = cardNumber.Replace(" ", string.Empty);
            if (amount <= 0 ||
                cardNumber.Length != 16 ||
                !cardNumber.All(char.IsDigit) ||
                cvv.Length != 3 ||
                !cvv.All(char.IsDigit) ||
                !DateTime.TryParseExact(
                    $"01/{expiry}",
                    "dd/MM/yy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var expiryDate) ||
                expiryDate < new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1))
            {
                return new PaymentResult(false, null, "Dane płatności są nieprawidłowe.");
            }

            if (cardNumber.EndsWith("0000", StringComparison.Ordinal))
            {
                return new PaymentResult(false, null, "Płatność została odrzucona przez bank.");
            }

            return new PaymentResult(true, $"SIM-{Guid.NewGuid():N}", null);
        }
    }
}
