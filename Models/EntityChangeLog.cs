using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("EntityChangeLogs")]
    public class EntityChangeLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [Required]
        [StringLength(50)]
        public string ChangeType { get; set; } = string.Empty; // Added, Modified, Deleted

        [Column(TypeName = "jsonb")]
        public string? OriginalValues { get; set; } // JSON с оригинальными значениями

        [Column(TypeName = "jsonb")]
        public string? NewValues { get; set; } // JSON с новыми значениями

        [Column(TypeName = "jsonb")]
        public string? ChangedProperties { get; set; } // JSON с изменениями

        public int? UserId { get; set; }

        [StringLength(100)]
        public string? UserName { get; set; }

        public DateTime Timestamp { get; set; }

        // Навигационные свойства
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}