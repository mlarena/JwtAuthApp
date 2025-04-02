using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace JwtAuthApp.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters.")]
        public string Username { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Role cannot be longer than 50 characters.")]
        public string Role { get; set; }

        public List<string> SelectedControllers { get; set; }
    }
}
