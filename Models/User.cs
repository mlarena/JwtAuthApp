using System.ComponentModel.DataAnnotations;

namespace JwtAuthApp.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters.")]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        public required string Salt { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Role cannot be longer than 50 characters.")]
        public string Role { get; set; } = "User"; // Добавляем роль с дефолтным значением "User"
    }
}
