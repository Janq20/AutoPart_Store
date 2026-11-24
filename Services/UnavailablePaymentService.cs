namespace MyApp.Services
{
    public class UnavailablePaymentService : IPaymentService
    {
        public PaymentResult Process(string cardNumber, string expiry, string cvv, decimal amount) =>
            new(false, null, "Płatności online nie są skonfigurowane dla środowiska produkcyjnego.");
    }
}
