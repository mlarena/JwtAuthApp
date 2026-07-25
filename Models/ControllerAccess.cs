using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("ControllerAccess")]
    public class ControllerAccess
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ControllerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool AllowAllAuthenticated { get; set; }

        public ICollection<ControllerAccessRole> ControllerAccessRoles { get; set; } = new List<ControllerAccessRole>();
    }
}
