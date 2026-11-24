using System.Globalization;
using MyApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyApp.Services
{
    public interface IInvoicePdfService
    {
        byte[] Generate(Order order);
    }

    public class InvoicePdfService : IInvoicePdfService
    {
        private const decimal VatRate = 0.23m;

        public byte[] Generate(Order order)
        {
            var invoiceNumber = order.InvoiceNumber ?? $"FV/{DateTime.UtcNow:yyyy}/{order.Id}";
            var totalNet = order.TotalAmount / (1 + VatRate);
            var totalVat = order.TotalAmount - totalNet;
            var address = order.DeliveryAddress;

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.DefaultTextStyle(style => style.FontFamily("Arial").FontSize(9));
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("AutoPart Store").FontSize(19).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Sprzedawca części samochodowych");
                            });
                            row.ConstantItem(190).Border(1).BorderColor(Colors.Blue.Darken2).Padding(10).Column(column =>
                            {
                                column.Item().Text("FAKTURA VAT").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"Nr {invoiceNumber}").FontSize(11).Bold();
                            });
                        });
                        header.Item().PaddingTop(16).Row(row =>
                        {
                            row.RelativeItem().Border(1).Padding(8).Column(column =>
                            {
                                column.Item().Text("SPRZEDAWCA").Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text("AutoPart Store");
                                column.Item().Text("Sklep internetowy");
                                column.Item().Text("NIP: do uzupełnienia");
                            });
                            row.ConstantItem(12);
                            row.RelativeItem().Border(1).Padding(8).Column(column =>
                            {
                                column.Item().Text("NABYWCA").Bold().FontColor(Colors.Blue.Darken2);
                                column.Item().Text($"{order.User?.FirstName} {order.User?.LastName}");
                                column.Item().Text(order.User?.Email ?? string.Empty);
                                if (address is not null)
                                {
                                    column.Item().Text($"{address.Street}, {address.PostalCode} {address.City}");
                                    column.Item().Text($"Tel.: {address.PhoneNumber}");
                                }
                            });
                        });
                        header.Item().PaddingTop(12).Row(row =>
                        {
                            row.RelativeItem().Text($"Miejscowość: Nowy Sącz");
                            row.RelativeItem().AlignRight().Text($"Data wystawienia: {order.OrderDate:dd.MM.yyyy}");
                        });
                    });

                    page.Content().PaddingTop(18).Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(28);
                                columns.RelativeColumn(4);
                                columns.ConstantColumn(52);
                                columns.ConstantColumn(62);
                                columns.ConstantColumn(62);
                                columns.ConstantColumn(58);
                                columns.ConstantColumn(62);
                            });

                            table.Header(header =>
                            {
                                HeaderCell(header.Cell(), "Lp.");
                                HeaderCell(header.Cell(), "Nazwa towaru lub usługi");
                                HeaderCell(header.Cell(), "Ilość");
                                HeaderCell(header.Cell(), "Cena netto");
                                HeaderCell(header.Cell(), "Wartość netto");
                                HeaderCell(header.Cell(), "VAT");
                                HeaderCell(header.Cell(), "Wartość brutto");
                            });

                            var index = 1;
                            foreach (var item in order.Items)
                            {
                                var gross = item.UnitPrice * item.Quantity;
                                var net = gross / (1 + VatRate);
                                Cell(table.Cell(), index++.ToString(CultureInfo.InvariantCulture));
                                Cell(table.Cell(), item.Product?.Name ?? "Produkt");
                                Cell(table.Cell(), item.Quantity.ToString(CultureInfo.InvariantCulture));
                                Cell(table.Cell(), $"{item.UnitPrice / (1 + VatRate):F2} zł");
                                Cell(table.Cell(), $"{net:F2} zł");
                                Cell(table.Cell(), "23%");
                                Cell(table.Cell(), $"{gross:F2} zł");
                            }
                        });

                        content.Item().PaddingTop(16).Row(row =>
                        {
                            row.RelativeItem().Border(1).Padding(8).Column(column =>
                            {
                                column.Item().Text("Sposób i termin zapłaty").Bold();
                                column.Item().Text("Płatność online, zapłacono");
                                column.Item().Text($"Status płatności: {order.PaymentStatus}");
                            });
                            row.ConstantItem(12);
                            row.ConstantItem(205).Border(1).Padding(8).Column(column =>
                            {
                                column.Item().Text($"Netto: {totalNet:F2} zł");
                                column.Item().Text($"VAT 23%: {totalVat:F2} zł");
                                column.Item().PaddingTop(4).Text($"RAZEM: {order.TotalAmount:F2} zł").Bold().FontSize(12);
                            });
                        });

                        content.Item().PaddingTop(18).Text("Uwagi: Dokument wygenerowany elektronicznie.").FontColor(Colors.Grey.Darken1);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("AutoPart Store | Faktura ");
                        text.Span(invoiceNumber).Bold();
                    });
                });
            }).GeneratePdf();
        }

        private static void HeaderCell(IContainer container, string text) =>
            container.Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text(text).FontColor(Colors.White).Bold();

        private static void Cell(IContainer container, string text) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(text);
    }
}
