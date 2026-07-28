namespace AttendanceManagementSystem.ViewModels
{
    public class AdminAttendanceStampLogViewModel
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? StampType { get; set; }

        public string? GpsStatus { get; set; }

        public string? Keyword { get; set; }

        public List<AdminAttendanceStampLogItemViewModel> Logs
        {
            get;
            set;
        } = new();

        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages { get; set; } = 1;
    }

    public class AdminAttendanceStampLogItemViewModel
    {
        public int AttendanceStampLogId { get; set; }

        public int AttendanceId { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public string StampType { get; set; } = string.Empty;

        public DateTime StampedAt { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public double? AccuracyMeters { get; set; }

        public string GpsStatus { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public string UserAgent { get; set; } = string.Empty;
    }
}