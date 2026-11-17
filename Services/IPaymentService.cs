namespace MyApp.Services
{
    public interface IPaymentService
    {
        PaymentResult Process(string cardNumber, string expiry, string cvv, decimal amount);
    }

    public record PaymentResult(bool IsSuccessful, string? TransactionId, string? ErrorMessage);
}
