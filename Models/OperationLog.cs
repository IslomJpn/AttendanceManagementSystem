using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class OperationLog
    {
        [Key]
        public int OperationLogId { get; set; }

        // ログインしているユーザーID
        // 存在しないメールでのログイン失敗時は null
        public int? EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        // 操作したユーザー名
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        // Admin / Employee / Unknown
        [StringLength(20)]
        public string Role { get; set; } = "Unknown";

        // ログイン、出勤、承認など
        [Required]
        [StringLength(100)]
        public string ActionName { get; set; } = string.Empty;

        // Employee、Attendance、PaidLeaveRequest など
        [StringLength(100)]
        public string TargetType { get; set; } = string.Empty;

        // 操作対象のID
        public int? TargetId { get; set; }

        // 操作内容の説明
        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;

        // 成功 / 失敗
        [Required]
        [StringLength(20)]
        public string Result { get; set; } = "成功";

        [StringLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        [StringLength(500)]
        public string UserAgent { get; set; } = string.Empty;

        // PC / Mobile / Tablet / Unknown
        [StringLength(30)]
        public string DeviceType { get; set; } = "Unknown";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}