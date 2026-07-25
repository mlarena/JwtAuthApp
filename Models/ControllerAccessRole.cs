using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("ControllerAccessRoles")]
    public class ControllerAccessRole
    {
        [Column("ControllerAccessId")]
        public int ControllerAccessId { get; set; }

        [Column("RoleId")]
        public int RoleId { get; set; }

        [ForeignKey(nameof(ControllerAccessId))]
        public ControllerAccess ControllerAccess { get; set; } = null!;

        [ForeignKey(nameof(RoleId))]
        public Role Role { get; set; } = null!;
    }
}
