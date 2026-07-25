using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("DustData")]
    public class DustData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime DataTimestamp { get; set; }

        [Column(TypeName = "numeric(10,2)")] public decimal? PM10Act { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? PM25Act { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? PM1Act { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? PM10AWG { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? PM25AWG { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? PM1AWG { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? FlowProbe { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? TemperatureProbe { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? HumidityProbe { get; set; }
        public int? LaserStatus { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? SupplyVoltage { get; set; }
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
