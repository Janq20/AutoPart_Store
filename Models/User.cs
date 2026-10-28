using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = UserRoles.Client;

        public List<Address> Addresses { get; set; } = new();
    }
}