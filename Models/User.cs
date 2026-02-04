using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column("username")]
        public required string Username { get; set; }

        [Required]
        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [Required]
        [Column("salt")]
        public required string Salt { get; set; }

        [Required]
        [StringLength(50)]
        [Column("role")]
        public string Role { get; set; } = "User";

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационное свойство - ДОБАВЬТЕ ЭТО!
        public virtual ICollection<UserControllerAccess> ControllerAccesses { get; set; } = new List<UserControllerAccess>();
    }
}