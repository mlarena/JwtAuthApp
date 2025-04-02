using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    public class UserControllerAccess
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ControllerName { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
