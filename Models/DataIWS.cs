using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuthApp.Models
{
    [Table("DataIWS")]
    public class DataIWS
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SensorId { get; set; }

        [Required]
        public double Value { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SensorId))]
        public Sensor? Sensor { get; set; }
    }
}
