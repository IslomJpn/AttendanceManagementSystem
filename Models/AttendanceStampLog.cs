using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceManagementSystem.Models
{
    public class AttendanceStampLog
    {
        [Key]
        public int AttendanceStampLogId { get; set; }

        public int AttendanceId { get; set; }

        public Attendance? Attendance { get; set; }

        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required]
        [StringLength(20)]
        public string StampType { get; set; } = string.Empty;

        public DateTime StampedAt { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        public double? AccuracyMeters { get; set; }

        [Required]
        [StringLength(30)]
        public string GpsStatus { get; set; } = "未取得";

        [Required]
        [StringLength(20)]
        public string DeviceType { get; set; } = "Unknown";

        [StringLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Result { get; set; } = "成功";

        [StringLength(500)]
        public string Details { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}