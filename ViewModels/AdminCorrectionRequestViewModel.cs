namespace AttendanceManagementSystem.ViewModels
{
    public class AdminCorrectionRequestViewModel
    {
        public List<AdminCorrectionRequestItemViewModel> Requests { get; set; } = new();
    }

    public class AdminCorrectionRequestItemViewModel
    {
        public int RequestId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public DateTime TargetDate { get; set; }

        public string CorrectionType { get; set; } = string.Empty;

        public TimeSpan? BeforeTime { get; set; }

        public TimeSpan AfterTime { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}