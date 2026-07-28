using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    public class EmployeePasswordResetViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage =
                "仮パスワードを入力してください。")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage =
                "仮パスワードは8文字以上100文字以内で入力してください。")]
        [DataType(DataType.Password)]
        [Display(Name = "新しい仮パスワード")]
        public string TemporaryPassword { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage =
                "確認用パスワードを入力してください。")]
        [DataType(DataType.Password)]
        [Display(Name = "新しい仮パスワード（確認）")]
        [Compare(
            nameof(TemporaryPassword),
            ErrorMessage =
                "仮パスワードと確認用パスワードが一致しません。")]
        public string ConfirmTemporaryPassword { get; set; }
            = string.Empty;
    }
}