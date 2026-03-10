using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models;

public enum AuditLogType
{
    Action = 1,
    Change = 2
}

[Table("AuditLogs")]
public sealed class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public AuditLogType Type { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int? UserId { get; set; }

    [StringLength(100)]
    public string? UserName { get; set; }

    // ===== Action log fields =====
    [StringLength(200)]
    public string? Action { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    public int? TargetId { get; set; }

    [StringLength(10)]
    public string? HttpMethod { get; set; }

    [StringLength(500)]
    public string? Url { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public bool? IsSuccess { get; set; }

    public long? ExecutionTimeMs { get; set; }

    // ===== Change log fields =====
    [StringLength(200)]
    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    [StringLength(50)]
    public string? ChangeType { get; set; } // Added/Modified/Deleted

    [Column(TypeName = "jsonb")]
    public string? OriginalValues { get; set; }

    [Column(TypeName = "jsonb")]
    public string? NewValues { get; set; }

    [Column(TypeName = "jsonb")]
    public string? ChangedProperties { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

