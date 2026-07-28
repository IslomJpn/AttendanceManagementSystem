using System;
using System.Collections.Generic;

namespace AttendanceManagementSystem.ViewModels
{
    public class AdminPaidLeaveAlertViewModel
    {
        public List<AdminPaidLeaveAlertItemViewModel> Alerts { get; set; }
            = new List<AdminPaidLeaveAlertItemViewModel>();
    }

    public class AdminPaidLeaveAlertItemViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; }

        public DateTime? CurrentGrantDate { get; set; }

        public DateTime? FiveDayDeadline { get; set; }

        public DateTime? LegalExpiryDate { get; set; }

        public int GrantedDays { get; set; }

        public double UsedDays { get; set; }

        public double RemainingDays { get; set; }

        public double RemainingFiveDayRequirement { get; set; }

        public double AttendanceRate { get; set; }

        public bool IsAttendanceRateEnough { get; set; }

        public bool IsFiveDayAlertTarget { get; set; }

        public string StatusText { get; set; } = string.Empty;
    }
}