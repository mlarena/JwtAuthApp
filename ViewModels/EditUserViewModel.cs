using System.ComponentModel.DataAnnotations;

namespace JwtAuthApp.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters.")]
        public string UserName { get; set; } = string.Empty;
    }
}
