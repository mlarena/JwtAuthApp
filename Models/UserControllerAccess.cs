using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("user_controller_accesses")]
    public class UserControllerAccess
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("user_id")]
        public int UserId { get; set; }
        
        [Required]
        [Column("controller_name")]
        public string ControllerName { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}