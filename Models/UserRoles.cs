namespace MyApp.Models
{
    public static class UserRoles
    {
        public const string Client = "Klient";
        public const string Employee = "Pracownik";
        public const string Administrator = "Administrator";

        public static readonly string[] All =
        {
            Client,
            Employee,
            Administrator
        };
    }
}
