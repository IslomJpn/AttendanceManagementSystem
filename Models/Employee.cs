using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
            = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }
            = string.Empty;

        [Required]
        public string PasswordHash { get; set; }
            = string.Empty;

        [Required]
        public int DepartmentId { get; set; }

        public Department? Department { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; }
            = "Employee";

        public DateTime JoinDate { get; set; }

        public bool IsActive { get; set; }
            = true;

        // 所定出勤時刻
        public TimeSpan ScheduledStartTime { get; set; }
            = new TimeSpan(9, 0, 0);

        // 所定退勤時刻
        public TimeSpan ScheduledEndTime { get; set; }
            = new TimeSpan(18, 0, 0);

        // 1日の所定労働時間（分）
        [Range(
            1,
            1440,
            ErrorMessage =
                "所定労働時間は1分以上1440分以内で設定してください。"
        )]
        public int ScheduledWorkMinutes { get; set; }
            = 480;

        // 昼休憩開始時刻
        public TimeSpan LunchBreakStartTime { get; set; }
            = new TimeSpan(12, 0, 0);

        // 昼休憩終了時刻
        public TimeSpan LunchBreakEndTime { get; set; }
            = new TimeSpan(13, 0, 0);

        // 小休憩1開始時刻
        public TimeSpan? SmallBreak1StartTime { get; set; }

        // 小休憩1終了時刻
        public TimeSpan? SmallBreak1EndTime { get; set; }

        // 小休憩2開始時刻
        public TimeSpan? SmallBreak2StartTime { get; set; }

        // 小休憩2終了時刻
        public TimeSpan? SmallBreak2EndTime { get; set; }

        // 初回ログイン時のパスワード変更
        public bool MustChangePassword { get; set; }
            = false;

        // ログイン失敗回数
        public int FailedLoginCount { get; set; }
            = 0;

        // 最後にログインに失敗した日時
        public DateTime? LastFailedLoginAt { get; set; }

        // アカウントロック終了日時
        public DateTime? LockoutEndAt { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public DateTime UpdatedAt { get; set; }
            = DateTime.Now;
    }
}