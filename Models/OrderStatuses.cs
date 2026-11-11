namespace MyApp.Models
{
    public static class OrderStatuses
    {
        public const string New = "Nowe";
        public const string Processing = "W realizacji";
        public const string Shipped = "Wysłane";
        public const string Completed = "Zrealizowane";
        public const string Cancelled = "Anulowane";

        public static readonly string[] All =
        {
            New,
            Processing,
            Shipped,
            Completed,
            Cancelled
        };

        public static bool CanTransition(string currentStatus, string nextStatus)
        {
            if (nextStatus == Cancelled)
            {
                return currentStatus is New or Processing;
            }

            return (currentStatus, nextStatus) switch
            {
                (New, Processing) => true,
                (Processing, Shipped) => true,
                (Shipped, Completed) => true,
                _ => false
            };
        }
    }
}
