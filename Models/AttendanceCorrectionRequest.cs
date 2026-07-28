using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class AttendanceCorrectionRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required]
        public int AttendanceId { get; set; }

        public Attendance? Attendance { get; set; }

        [Required]
        public DateTime TargetDate { get; set; }

        [Required]
        [StringLength(20)]
        public string CorrectionType { get; set; } = string.Empty;

        public TimeSpan? BeforeTime { get; set; }

        [Required]
        public TimeSpan AfterTime { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "申請中";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedBy { get; set; }

        [StringLength(500)]
        public string? AdminComment { get; set; }
    }
}