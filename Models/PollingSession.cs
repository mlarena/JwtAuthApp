using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("PollingSessions")]
    public class PollingSession
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public int MonitoringPostId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "IN_PROGRESS";

        public int TotalSensorsCount { get; set; } = 0;

        public int SuccessfulSensorsCount { get; set; } = 0;

        public string? FailedSensorsDetails { get; set; }

        [ForeignKey(nameof(MonitoringPostId))]
        public MonitoringPost? MonitoringPost { get; set; }

        public ICollection<DOVData> DOVDatas { get; set; } = new List<DOVData>();
        public ICollection<DSPDData> DSPDDatas { get; set; } = new List<DSPDData>();
        public ICollection<DustData> DustDatas { get; set; } = new List<DustData>();
        public ICollection<IWSData> IWSDatas { get; set; } = new List<IWSData>();
        public ICollection<MUEKSData> MUEKSDatas { get; set; } = new List<MUEKSData>();
        public ICollection<SensorResult> SensorResults { get; set; } = new List<SensorResult>();
    }
}
