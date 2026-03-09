using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("UserActionLogs")]
    public class UserActionLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;

        public int? TargetId { get; set; }

        [StringLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        [StringLength(500)]
        public string Url { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; }

        public bool IsSuccess { get; set; }

        public long ExecutionTimeMs { get; set; }

        // Навигационное свойство
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}