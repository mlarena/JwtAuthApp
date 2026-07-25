using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("DOVData")]
    public class DOVData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime DataTimestamp { get; set; }

        [Column(TypeName = "numeric(10,2)")]
        public decimal? VisibleRange { get; set; }

        public int? BrightFlag { get; set; }

        public Guid? PollingSessionId { get; set; }

        public int? MonitoringPostId { get; set; }

        [ForeignKey(nameof(SensorId))]
        public Sensor? Sensor { get; set; }

        [ForeignKey(nameof(PollingSessionId))]
        public PollingSession? PollingSession { get; set; }

        [ForeignKey(nameof(MonitoringPostId))]
        public MonitoringPost? MonitoringPost { get; set; }
    }
}
