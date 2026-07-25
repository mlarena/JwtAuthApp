using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("Sensor")]
    public class Sensor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int SensorTypeId { get; set; }

        public int? MonitoringPostId { get; set; }

        public double? Longitude { get; set; }

        public double? Latitude { get; set; }

        [Required]
        [StringLength(64)]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string EndPointsName { get; set; } = string.Empty;

        [Required]
        public string Url { get; set; } = string.Empty;

        public DateTime? LastActivityUTC { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(SensorTypeId))]
        public SensorType? SensorType { get; set; }

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
