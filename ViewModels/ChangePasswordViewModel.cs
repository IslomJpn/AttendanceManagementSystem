using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(
            ErrorMessage = "現在のパスワードを入力してください。")]
        [DataType(DataType.Password)]
        [Display(Name = "現在のパスワード")]
        public string CurrentPassword { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage = "新しいパスワードを入力してください。")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage =
                "新しいパスワードは8文字以上100文字以内で入力してください。")]
        [DataType(DataType.Password)]
        [Display(Name = "新しいパスワード")]
        public string NewPassword { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage = "確認用パスワードを入力してください。")]
        [DataType(DataType.Password)]
        [Display(Name = "新しいパスワード（確認）")]
        [Compare(
            nameof(NewPassword),
            ErrorMessage =
                "新しいパスワードと確認用パスワードが一致しません。")]
        public string ConfirmPassword { get; set; }
            = string.Empty;
    }
}