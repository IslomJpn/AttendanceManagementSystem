using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class PaidLeaveRequest
    {
        [Key]
        public int PaidLeaveRequestId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        [Required]
        public DateTime LeaveDate { get; set; }

        [Required]
        public double Days { get; set; } = 1.0;

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "申請中";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedBy { get; set; }
        [StringLength(500)]
        public string? AdminComment { get; set; }
    }
}