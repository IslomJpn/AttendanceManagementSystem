using System;

namespace AttendanceManagementSystem.ViewModels
{
    public class AdminPaidLeaveNoticeViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public DateTime? CurrentGrantDate { get; set; }

        public DateTime? FiveDayDeadline { get; set; }

        public double UsedDays { get; set; }

        public double RemainingFiveDayRequirement { get; set; }

        public string NoticeText { get; set; } = string.Empty;
    }
}