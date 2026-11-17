using MyApp.Models;

namespace MyApp.Services
{
    public interface IOrderNotificationService
    {
        Task SendStatusChangedAsync(
            Order order,
            bool attachInvoice = false,
            CancellationToken cancellationToken = default);
    }
}
