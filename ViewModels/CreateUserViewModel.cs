using System.ComponentModel.DataAnnotations;

namespace JwtAuthApp.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters.")]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Role cannot be longer than 50 characters.")]
        [Display(Name = "Role")]
        public string Role { get; set; }
    }
}