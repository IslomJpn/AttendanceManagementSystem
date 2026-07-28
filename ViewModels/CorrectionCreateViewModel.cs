using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    public class CorrectionCreateViewModel
    {
        [Required(ErrorMessage = "対象日を入力してください。")]
        public DateTime TargetDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "修正項目を選択してください。")]
        public string CorrectionType { get; set; } = "出勤時間";

        public TimeSpan? BeforeTime { get; set; }

        [Required(ErrorMessage = "修正後の時間を入力してください。")]
        public TimeSpan AfterTime { get; set; }

        [Required(ErrorMessage = "理由を入力してください。")]
        public string Reason { get; set; } = string.Empty;

        public string? Message { get; set; }
    }
}