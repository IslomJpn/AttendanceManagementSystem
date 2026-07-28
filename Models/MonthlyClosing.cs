using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AttendanceManagementSystem.Models
{
    public class MonthlyClosing
    {
        [Key]
        public int MonthlyClosingId { get; set; }

        [Range(2000, 2100)]
        public int TargetYear { get; set; }

        [Range(1, 12)]
        public int TargetMonth { get; set; }

        public bool IsClosed { get; set; }

        public DateTime? ClosedAt { get; set; }

        public int? ClosedByEmployeeId { get; set; }

        [ForeignKey(nameof(ClosedByEmployeeId))]
        public Employee? ClosedByEmployee { get; set; }

        [StringLength(500)]
        public string? ClosingComment { get; set; }

        public DateTime? ReopenedAt { get; set; }

        public int? ReopenedByEmployeeId { get; set; }

        [ForeignKey(nameof(ReopenedByEmployeeId))]
        public Employee? ReopenedByEmployee { get; set; }

        [StringLength(500)]
        public string? ReopenComment { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public DateTime UpdatedAt { get; set; }
            = DateTime.Now;
    }
}