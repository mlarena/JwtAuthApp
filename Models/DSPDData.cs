using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("DSPDData")]
    public class DSPDData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime DataTimestamp { get; set; }

        [Column(TypeName = "numeric(5,2)")] public decimal? Grip { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? Shake { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? UPower { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? TemperatureCase { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? TemperatureRoad { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? HeightH2O { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? HeightIce { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? HeightSnow { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? PercentICE { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? PercentPGM { get; set; }
        public int? RoadStatus { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? AngleToRoad { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? TemperatureFreezePGM { get; set; }
        public int? NeedCalibration { get; set; }
        [Column(TypeName = "numeric(10,6)")] public decimal? GPSLatitude { get; set; }
        [Column(TypeName = "numeric(10,6)")] public decimal? GPSLongitude { get; set; }
        [Column(TypeName = "numeric(12,2)")] public decimal? DistanceToSurface { get; set; }
        public bool? IsGpsValid { get; set; } = true;
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
