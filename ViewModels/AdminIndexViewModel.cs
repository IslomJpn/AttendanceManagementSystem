namespace AttendanceManagementSystem.ViewModels
{
    public class AdminIndexViewModel
    {
        public int TodayClockInCount { get; set; }

        public int TodayLateCount { get; set; }

        public int TodayClockOutCount { get; set; }

        public int PendingCorrectionRequestCount { get; set; }

        public int PendingPaidLeaveRequestCount { get; set; }

        public int PaidLeaveAlertCount { get; set; }

        // 出勤打刻を忘れている社員数
        public int MissingClockInCount =>
            MissingClockInEmployees.Count;

        // 退勤打刻を忘れている社員数
        public int MissingClockOutCount =>
            MissingClockOutEmployees.Count;

        // 本日の出勤未打刻社員一覧
        public List<MissingClockInEmployeeViewModel>
            MissingClockInEmployees
        { get; set; }
                = new();

        // 本日の退勤未打刻社員一覧
        public List<MissingClockOutEmployeeViewModel>
            MissingClockOutEmployees
        { get; set; }
                = new();
    }

    // 出勤打刻を忘れている社員
    public class MissingClockInEmployeeViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; }
            = string.Empty;

        public string DepartmentName { get; set; }
            = string.Empty;

        // 社員に設定されている所定出勤時刻
        public TimeSpan ScheduledStartTime { get; set; }

        // 所定出勤時刻から経過した時間
        public int ElapsedMinutes { get; set; }
    }

    // 退勤打刻を忘れている社員
    public class MissingClockOutEmployeeViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; }
            = string.Empty;

        public string DepartmentName { get; set; }
            = string.Empty;

        // 本日の実際の出勤時刻
        public TimeSpan ClockInTime { get; set; }

        // 社員に設定されている所定退勤時刻
        public TimeSpan ScheduledEndTime { get; set; }

        // 所定退勤時刻から経過した時間
        public int ElapsedMinutes { get; set; }
    }
}