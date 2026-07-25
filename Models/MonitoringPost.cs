using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("MonitoringPost")]
    public class MonitoringPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public double? Longitude { get; set; }

        public double? Latitude { get; set; }

        public bool IsMobile { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? Address { get; set; }

        public int PollingIntervalSeconds { get; set; } = 60;

        public DateTime? LastPolledAt { get; set; }

        public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
        public ICollection<PollingSession> PollingSessions { get; set; } = new List<PollingSession>();
    }
}
