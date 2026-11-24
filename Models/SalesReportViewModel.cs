namespace MyApp.Models
{
    public class SalesReportViewModel
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal MaximumDailyRevenue { get; set; }
        public List<SalesDayViewModel> DailySales { get; set; } = new();
    }

    public class SalesDayViewModel
    {
        public DateOnly Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal BarHeightPercent { get; set; }
    }
}
