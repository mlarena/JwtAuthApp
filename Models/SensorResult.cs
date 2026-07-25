using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("SensorResults")]
    public class SensorResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorId { get; set; }

        public DateTime CheckedAt { get; set; }

        public int StatusCode { get; set; }

        public string? ResponseBody { get; set; }

        public long ResponseTimeMs { get; set; }

        public bool IsSuccess { get; set; }

        public Guid? PollingSessionId { get; set; }

        [ForeignKey(nameof(SensorId))]
        public Sensor? Sensor { get; set; }

        [ForeignKey(nameof(PollingSessionId))]
        public PollingSession? PollingSession { get; set; }
    }
}
