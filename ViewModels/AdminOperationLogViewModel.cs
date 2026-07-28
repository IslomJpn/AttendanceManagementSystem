namespace AttendanceManagementSystem.ViewModels
{
    public class AdminOperationLogViewModel
    {
        // 検索条件
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? ActionName { get; set; }

        public string? Result { get; set; }

        public string? Keyword { get; set; }

        // 操作名の選択肢
        public List<string> ActionNames { get; set; } = new();

        // ログ一覧
        public List<AdminOperationLogItemViewModel> Logs { get; set; } = new();

        // ページング
        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }
    }

    public class AdminOperationLogItemViewModel
    {
        public int OperationLogId { get; set; }

        public int? EmployeeId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string ActionName { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public int? TargetId { get; set; }

        public string Details { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public string DeviceType { get; set; } = string.Empty;

        public string UserAgent { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}