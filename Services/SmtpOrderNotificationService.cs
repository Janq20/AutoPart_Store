using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using MyApp.Models;

namespace MyApp.Services
{
    public class SmtpOptions
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public string From { get; set; } = string.Empty;
    }

    public class SmtpOrderNotificationService : IOrderNotificationService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpOrderNotificationService> _logger;
        private readonly IInvoicePdfService _invoicePdfService;

        public SmtpOrderNotificationService(
            IOptions<SmtpOptions> options,
            ILogger<SmtpOrderNotificationService> logger,
            IInvoicePdfService invoicePdfService)
        {
            _options = options.Value;
            _logger = logger;
            _invoicePdfService = invoicePdfService;
        }

        public async Task SendStatusChangedAsync(
            Order order,
            bool attachInvoice = false,
            CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled || string.IsNullOrWhiteSpace(order.User?.Email))
            {
                return;
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };
            using var message = new MailMessage(_options.From, order.User.Email)
            {
                Subject = $"AutoPart Store - status zamówienia #{order.Id}",
                IsBodyHtml = true,
                Body = BuildBody(order)
            };
            if (attachInvoice)
            {
                message.Attachments.Add(new Attachment(
                    new MemoryStream(_invoicePdfService.Generate(order)),
                    $"faktura-{order.Id}.pdf",
                    "application/pdf"));
            }
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Order status notification sent for order {OrderId}.", order.Id);
        }

        private static string BuildBody(Order order)
        {
            var customerName = HtmlEncoder.Default.Encode($"{order.User?.FirstName} {order.User?.LastName}");
            var status = HtmlEncoder.Default.Encode(order.Status);
            var amount = order.TotalAmount.ToString("F2");
            var invoiceText = order.InvoiceNumber is null
                ? string.Empty
                : $"<p style=\"margin:0 0 16px;color:#64748b\">Faktura PDF znajduje się w załączniku.</p>";

            return $"""
                <div style="font-family:Arial,sans-serif;background:#f1f5f9;padding:32px;color:#172b3a">
                  <div style="max-width:620px;margin:auto;background:#ffffff;border-radius:12px;overflow:hidden">
                    <div style="background:#122532;padding:22px 28px;color:#ffffff">
                      <span style="display:inline-block;background:#ff7043;border-radius:8px;padding:8px 11px;font-weight:bold;font-size:20px;margin-right:10px">A</span>
                      <strong style="font-size:20px">AutoPart Store</strong>
                    </div>
                    <div style="padding:28px">
                      <p style="color:#e27124;font-size:12px;font-weight:bold;letter-spacing:2px">AKTUALIZACJA ZAMÓWIENIA</p>
                      <h1 style="font-size:24px;margin:8px 0 20px">Cześć, {customerName}!</h1>
                      <p>Status zamówienia <strong>#{order.Id}</strong> zmienił się na:</p>
                      <p style="display:inline-block;background:#fff3cd;color:#8a5a00;padding:9px 14px;border-radius:6px;font-weight:bold">{status}</p>
                      <div style="border-top:1px solid #e2e8f0;margin-top:18px;padding-top:18px">
                        <p style="margin:0 0 6px">Wartość zamówienia: <strong>{amount} zł</strong></p>
                        {invoiceText}
                      </div>
                      <p style="color:#64748b;font-size:13px;margin:24px 0 0">Dziękujemy za zakupy w AutoPart Store.</p>
                    </div>
                  </div>
                </div>
                """;
        }
    }
}
