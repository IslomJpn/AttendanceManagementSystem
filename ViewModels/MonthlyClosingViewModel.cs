namespace AttendanceManagementSystem.ViewModels
{
    public class MonthlyClosingViewModel
    {
        public string YearMonth { get; set; }
            = DateTime.Today.ToString("yyyy-MM");

        public int TargetYear { get; set; }

        public int TargetMonth { get; set; }

        public bool IsClosed { get; set; }

        public int AttendanceCount { get; set; }

        public int MissingClockOutCount { get; set; }

        public int PendingCorrectionRequestCount { get; set; }
        public int PendingPaidLeaveRequestCount { get; set; }

        public DateTime? ClosedAt { get; set; }

        public int? ClosedByEmployeeId { get; set; }

        public string ClosedByEmployeeName { get; set; }
            = string.Empty;

        public string ClosingComment { get; set; }
            = string.Empty;

        public DateTime? ReopenedAt { get; set; }

        public int? ReopenedByEmployeeId { get; set; }

        public string ReopenedByEmployeeName { get; set; }
            = string.Empty;

        public string ReopenComment { get; set; }
            = string.Empty;

        public List<MonthlyClosingHistoryItemViewModel> History
        {
            get;
            set;
        } = new();
    }

    public class MonthlyClosingHistoryItemViewModel
    {
        public int MonthlyClosingId { get; set; }

        public int TargetYear { get; set; }

        public int TargetMonth { get; set; }

        public bool IsClosed { get; set; }

        public DateTime? ClosedAt { get; set; }

        public string ClosedByEmployeeName { get; set; }
            = string.Empty;

        public string ClosingComment { get; set; }
            = string.Empty;

        public DateTime? ReopenedAt { get; set; }

        public string ReopenedByEmployeeName { get; set; }
            = string.Empty;

        public string ReopenComment { get; set; }
            = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }
}