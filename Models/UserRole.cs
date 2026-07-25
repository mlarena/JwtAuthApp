using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("UserRoles")]
    public class UserRole
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("RoleId")]
        public int RoleId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;
    }
}
