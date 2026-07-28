namespace AttendanceManagementSystem.ViewModels
{
    public class EmployeeListViewModel
    {
        public int? DepartmentId { get; set; }

        public string? Keyword { get; set; }

        public List<EmployeeListItemViewModel>
            Employees
        { get; set; }
                = new();

        public List<DepartmentSelectViewModel>
            Departments
        { get; set; }
                = new();
    }

    public class EmployeeListItemViewModel
    {
        public int EmployeeId { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        public string DepartmentName { get; set; }
            = string.Empty;

        public string Role { get; set; }
            = string.Empty;

        public bool IsActive { get; set; }

        // 所定出勤時刻
        public TimeSpan ScheduledStartTime { get; set; }
            = new TimeSpan(9, 0, 0);

        // 所定退勤時刻
        public TimeSpan ScheduledEndTime { get; set; }
            = new TimeSpan(18, 0, 0);

        // 1日の所定労働時間（分）
        public int ScheduledWorkMinutes { get; set; }
            = 480;

        // 初回パスワード変更が必要か
        public bool MustChangePassword { get; set; }

        // ログイン失敗回数
        public int FailedLoginCount { get; set; }

        // アカウントロック終了日時
        public DateTime? LockoutEndAt { get; set; }

        // 現在ロック中か
        public bool IsLocked { get; set; }

        // ロック解除までの残り時間
        public int RemainingLockMinutes { get; set; }
    }

    public class DepartmentSelectViewModel
    {
        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; }
            = string.Empty;
    }
}