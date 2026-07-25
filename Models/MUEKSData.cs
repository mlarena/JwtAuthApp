using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("MUEKSData")]
    public class MUEKSData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        public DateTime DataTimestamp { get; set; }

        [Column(TypeName = "numeric(5,2)")]  public decimal? TemperatureBox { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? UPowerIn12B { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? UOut12B { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? IOut12B { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? IOut48B { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? UAkb { get; set; }
        [Column(TypeName = "numeric(5,2)")]  public decimal? IAkb { get; set; }
        public int? Sens220B { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? WhAkb { get; set; }
        [Column(TypeName = "numeric(10,2)")] public decimal? VisibleRange { get; set; }
        public int? DoorStatus { get; set; }
        [StringLength(50)] public string? TdsH { get; set; }
        [StringLength(50)] public string? TdsTds { get; set; }
        [StringLength(50)] public string? TkosaT1 { get; set; }
        [StringLength(50)] public string? TkosaT3 { get; set; }
        public Guid? PollingSessionId { get; set; }
        public int? MonitoringPostId { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? OwenCh1 { get; set; }
        [Column(TypeName = "numeric(5,2)")] public decimal? OwenCh2 { get; set; }

        [ForeignKey(nameof(SensorId))]
        public Sensor? Sensor { get; set; }
        [ForeignKey(nameof(PollingSessionId))]
        public PollingSession? PollingSession { get; set; }
        [ForeignKey(nameof(MonitoringPostId))]
        public MonitoringPost? MonitoringPost { get; set; }
    }
}
