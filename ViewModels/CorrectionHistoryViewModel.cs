namespace AttendanceManagementSystem.ViewModels
{
    public class CorrectionHistoryViewModel
    {
        public List<CorrectionHistoryItemViewModel> Items
        {
            get;
            set;
        } = new();
    }

    public class CorrectionHistoryItemViewModel
    {
        public int RequestId { get; set; }

        public DateTime TargetDate { get; set; }

        public string CorrectionType { get; set; }
            = string.Empty;

        public TimeSpan? BeforeTime { get; set; }

        public TimeSpan AfterTime { get; set; }

        public string Reason { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedBy { get; set; }

        public string ApprovedByName { get; set; }
            = string.Empty;

        public string AdminComment { get; set; }
            = string.Empty;
    }
}