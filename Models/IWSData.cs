using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("IWSData")]
    public class IWSData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime DataTimestamp { get; set; }

        [Column(TypeName = "numeric(5,2)")]  public decimal? EnvTemperature { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? Humidity { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? DewPoint { get; set; }
        [Column(TypeName = "numeric(7,2)")]  public decimal? PressureHPa { get; set; }
        [Column(TypeName = "numeric(7,2)")]  public decimal? PressureQNHHPa { get; set; }
        [Column(TypeName = "numeric(7,2)")]  public decimal? PressureMmHg { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? WindSpeed { get; set; }
        [Column(TypeName = "numeric(6,2)")]  public decimal? WindDirection { get; set; }
        [Column(TypeName = "numeric(6,2)")]  public decimal? WindVSound { get; set; }
        public int? PrecipitationType { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? PrecipitationIntensity { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? PrecipitationQuantity { get; set; }
        public int? PrecipitationElapsed { get; set; }
        public int? PrecipitationPeriod { get; set; }
        [Column(TypeName = "numeric(6,2)")]  public decimal? CO2Level { get; set; }
        [Column(TypeName = "numeric(5,1)")]  public decimal? SupplyVoltage { get; set; }
        [Column(TypeName = "numeric(10,6)")] public decimal? Latitude { get; set; }
        [Column(TypeName = "numeric(10,6)")] public decimal? Longitude { get; set; }
        [Column(TypeName = "numeric(7,2)")]  public decimal? Altitude { get; set; }
        public int? KSP { get; set; }
        [Column(TypeName = "numeric(5,1)")]  public decimal? GPSSpeed { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? AccelerationStDev { get; set; }
        [Column(TypeName = "numeric(5,1)")]  public decimal? Roll { get; set; }
        [Column(TypeName = "numeric(5,1)")]  public decimal? Pitch { get; set; }
        public int? WeAreFine { get; set; }
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
