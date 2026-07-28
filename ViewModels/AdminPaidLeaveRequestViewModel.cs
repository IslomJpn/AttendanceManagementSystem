namespace AttendanceManagementSystem.ViewModels
{
    public class AdminPaidLeaveRequestViewModel
    {
        public List<AdminPaidLeaveRequestItemViewModel> Requests { get; set; } = new();
    }

    public class AdminPaidLeaveRequestItemViewModel
    {
        public int PaidLeaveRequestId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public DateTime LeaveDate { get; set; }

        public double Days { get; set; }

        public string? Reason { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}