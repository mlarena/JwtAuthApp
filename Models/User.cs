using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("Users")]
    public class User
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column("UserName")]
        public required string UserName { get; set; }

        [Required]
        [Column("PasswordHash")]
        public required string PasswordHash { get; set; }

        [Required]
        [Column("Salt")]
        public required string Salt { get; set; }

        [Required]
        [StringLength(50)]
        [Column("Role")]
        public string Role { get; set; } = "User";

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}