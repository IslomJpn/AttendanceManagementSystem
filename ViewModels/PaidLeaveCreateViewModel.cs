using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    public class PaidLeaveCreateViewModel
    {
        public double RemainingDays { get; set; }

        [Required(
            ErrorMessage =
                "有給取得日を入力してください。"
        )]
        [DataType(DataType.Date)]
        [Display(Name = "有給取得日")]
        public DateTime LeaveDate { get; set; }
            = DateTime.Today;

        [Required(
            ErrorMessage =
                "日数を入力してください。"
        )]
        [Range(
            0.5,
            100,
            ErrorMessage =
                "日数を正しく入力してください。"
        )]
        [Display(Name = "申請日数")]
        public double Days { get; set; }
            = 1.0;

        [Display(Name = "申請理由")]
        public string? Reason { get; set; }

        public string? Message { get; set; }

        public List<PaidLeaveNonWorkingDayViewModel>
            NonWorkingDays
        { get; set; }
                = new();
    }

    public class PaidLeaveNonWorkingDayViewModel
    {
        public DateTime CalendarDate { get; set; }

        public string DayType { get; set; }
            = string.Empty;

        public string HolidayName { get; set; }
            = string.Empty;

        public string DisplayText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                    HolidayName))
                {
                    return DayType;
                }

                return
                    $"{DayType}（{HolidayName}）";
            }
        }
    }
}