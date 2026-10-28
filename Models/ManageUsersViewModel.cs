namespace MyApp.Models
{
    public class ManageUsersViewModel
    {
        public List<UserRoleViewModel> Users { get; set; } = new();
    }

    public class UserRoleViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
