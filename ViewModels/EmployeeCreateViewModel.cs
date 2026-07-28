using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    public class EmployeeCreateViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "氏名を入力してください。")]
        [StringLength(
            100,
            ErrorMessage = "氏名は100文字以内で入力してください。")]
        [Display(Name = "氏名")]
        public string Name { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage = "メールアドレスを入力してください。")]
        [EmailAddress(
            ErrorMessage =
                "メールアドレスの形式が正しくありません。")]
        [StringLength(
            100,
            ErrorMessage =
                "メールアドレスは100文字以内で入力してください。")]
        [Display(Name = "メールアドレス")]
        public string Email { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage = "初期パスワードを入力してください。")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage =
                "初期パスワードは8文字以上100文字以内で入力してください。")]
        [DataType(DataType.Password)]
        [Display(Name = "初期パスワード")]
        public string Password { get; set; }
            = string.Empty;

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "部署を選択してください。")]
        [Display(Name = "部署")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "権限を選択してください。")]
        [RegularExpression(
            "^(Employee|Admin)$",
            ErrorMessage = "権限の値が正しくありません。")]
        [Display(Name = "権限")]
        public string Role { get; set; }
            = "Employee";

        [Required(ErrorMessage = "入社日を入力してください。")]
        [DataType(DataType.Date)]
        [Display(Name = "入社日")]
        public DateTime JoinDate { get; set; }
            = DateTime.Today;

        // 所定出勤時刻
        [DataType(DataType.Time)]
        [Display(Name = "所定出勤時刻")]
        public TimeSpan ScheduledStartTime { get; set; }
            = new TimeSpan(9, 0, 0);

        // 所定退勤時刻
        [DataType(DataType.Time)]
        [Display(Name = "所定退勤時刻")]
        public TimeSpan ScheduledEndTime { get; set; }
            = new TimeSpan(18, 0, 0);

        // 所定労働時間（時間部分）
        [Range(
            0,
            23,
            ErrorMessage =
                "所定労働時間の時間は0～23で入力してください。")]
        [Display(Name = "所定労働時間（時間）")]
        public int ScheduledWorkHours { get; set; }
            = 8;

        // 所定労働時間（分部分）
        [Range(
            0,
            59,
            ErrorMessage =
                "所定労働時間の分は0～59で入力してください。")]
        [Display(Name = "所定労働時間（分）")]
        public int ScheduledWorkMinutePart { get; set; }
            = 0;

        // Controller で使用する合計所定労働時間
        public int ScheduledWorkMinutes =>
            ScheduledWorkHours * 60 +
            ScheduledWorkMinutePart;

        // 昼休憩開始時刻
        [DataType(DataType.Time)]
        [Display(Name = "昼休憩開始時刻")]
        public TimeSpan LunchBreakStartTime { get; set; }
            = new TimeSpan(12, 0, 0);

        // 昼休憩終了時刻
        [DataType(DataType.Time)]
        [Display(Name = "昼休憩終了時刻")]
        public TimeSpan LunchBreakEndTime { get; set; }
            = new TimeSpan(13, 0, 0);

        // 小休憩1
        [DataType(DataType.Time)]
        [Display(Name = "小休憩1開始時刻")]
        public TimeSpan? SmallBreak1StartTime { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "小休憩1終了時刻")]
        public TimeSpan? SmallBreak1EndTime { get; set; }

        // 小休憩2
        [DataType(DataType.Time)]
        [Display(Name = "小休憩2開始時刻")]
        public TimeSpan? SmallBreak2StartTime { get; set; }

        [DataType(DataType.Time)]
        [Display(Name = "小休憩2終了時刻")]
        public TimeSpan? SmallBreak2EndTime { get; set; }

        public List<DepartmentSelectViewModel>
            Departments
        { get; set; }
                = new();

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (ScheduledEndTime <= ScheduledStartTime)
            {
                yield return new ValidationResult(
                    "所定退勤時刻は所定出勤時刻より後に設定してください。",
                    new[]
                    {
                        nameof(ScheduledStartTime),
                        nameof(ScheduledEndTime)
                    }
                );
            }

            if (ScheduledWorkMinutes <= 0)
            {
                yield return new ValidationResult(
                    "所定労働時間は1分以上で設定してください。",
                    new[]
                    {
                        nameof(ScheduledWorkHours),
                        nameof(ScheduledWorkMinutePart)
                    }
                );
            }

            if (ScheduledEndTime > ScheduledStartTime)
            {
                var scheduledDuration =
                    (int)(
                        ScheduledEndTime -
                        ScheduledStartTime
                    ).TotalMinutes;

                if (ScheduledWorkMinutes >
                    scheduledDuration)
                {
                    yield return new ValidationResult(
                        "所定労働時間は勤務時間の範囲内で設定してください。",
                        new[]
                        {
                            nameof(ScheduledWorkHours),
                            nameof(ScheduledWorkMinutePart)
                        }
                    );
                }
            }

            if (LunchBreakEndTime <=
                LunchBreakStartTime)
            {
                yield return new ValidationResult(
                    "昼休憩終了時刻は開始時刻より後に設定してください。",
                    new[]
                    {
                        nameof(LunchBreakStartTime),
                        nameof(LunchBreakEndTime)
                    }
                );
            }
            else if (!IsWithinScheduledTime(
                         LunchBreakStartTime,
                         LunchBreakEndTime))
            {
                yield return new ValidationResult(
                    "昼休憩は勤務時間の範囲内で設定してください。",
                    new[]
                    {
                        nameof(LunchBreakStartTime),
                        nameof(LunchBreakEndTime)
                    }
                );
            }

            foreach (var result in ValidateOptionalBreak(
                         SmallBreak1StartTime,
                         SmallBreak1EndTime,
                         "小休憩1",
                         nameof(SmallBreak1StartTime),
                         nameof(SmallBreak1EndTime)))
            {
                yield return result;
            }

            foreach (var result in ValidateOptionalBreak(
                         SmallBreak2StartTime,
                         SmallBreak2EndTime,
                         "小休憩2",
                         nameof(SmallBreak2StartTime),
                         nameof(SmallBreak2EndTime)))
            {
                yield return result;
            }

            if (IsValidBreak(
                    SmallBreak1StartTime,
                    SmallBreak1EndTime) &&
                IsOverlapping(
                    SmallBreak1StartTime!.Value,
                    SmallBreak1EndTime!.Value,
                    LunchBreakStartTime,
                    LunchBreakEndTime))
            {
                yield return new ValidationResult(
                    "小休憩1は昼休憩と重ならないように設定してください。",
                    new[]
                    {
                        nameof(SmallBreak1StartTime),
                        nameof(SmallBreak1EndTime)
                    }
                );
            }

            if (IsValidBreak(
                    SmallBreak2StartTime,
                    SmallBreak2EndTime) &&
                IsOverlapping(
                    SmallBreak2StartTime!.Value,
                    SmallBreak2EndTime!.Value,
                    LunchBreakStartTime,
                    LunchBreakEndTime))
            {
                yield return new ValidationResult(
                    "小休憩2は昼休憩と重ならないように設定してください。",
                    new[]
                    {
                        nameof(SmallBreak2StartTime),
                        nameof(SmallBreak2EndTime)
                    }
                );
            }

            if (IsValidBreak(
                    SmallBreak1StartTime,
                    SmallBreak1EndTime) &&
                IsValidBreak(
                    SmallBreak2StartTime,
                    SmallBreak2EndTime) &&
                IsOverlapping(
                    SmallBreak1StartTime!.Value,
                    SmallBreak1EndTime!.Value,
                    SmallBreak2StartTime!.Value,
                    SmallBreak2EndTime!.Value))
            {
                yield return new ValidationResult(
                    "小休憩1と小休憩2は重ならないように設定してください。",
                    new[]
                    {
                        nameof(SmallBreak1StartTime),
                        nameof(SmallBreak1EndTime),
                        nameof(SmallBreak2StartTime),
                        nameof(SmallBreak2EndTime)
                    }
                );
            }
        }

        private IEnumerable<ValidationResult>
            ValidateOptionalBreak(
                TimeSpan? startTime,
                TimeSpan? endTime,
                string breakName,
                string startPropertyName,
                string endPropertyName)
        {
            if (!startTime.HasValue &&
                !endTime.HasValue)
            {
                yield break;
            }

            if (!startTime.HasValue ||
                !endTime.HasValue)
            {
                yield return new ValidationResult(
                    $"{breakName}は開始時刻と終了時刻を両方入力してください。",
                    new[]
                    {
                        startPropertyName,
                        endPropertyName
                    }
                );

                yield break;
            }

            if (endTime.Value <= startTime.Value)
            {
                yield return new ValidationResult(
                    $"{breakName}の終了時刻は開始時刻より後に設定してください。",
                    new[]
                    {
                        startPropertyName,
                        endPropertyName
                    }
                );

                yield break;
            }

            if (!IsWithinScheduledTime(
                    startTime.Value,
                    endTime.Value))
            {
                yield return new ValidationResult(
                    $"{breakName}は勤務時間の範囲内で設定してください。",
                    new[]
                    {
                        startPropertyName,
                        endPropertyName
                    }
                );
            }
        }

        private bool IsWithinScheduledTime(
            TimeSpan startTime,
            TimeSpan endTime)
        {
            return
                ScheduledEndTime >
                ScheduledStartTime &&
                startTime >=
                ScheduledStartTime &&
                endTime <=
                ScheduledEndTime;
        }

        private static bool IsValidBreak(
            TimeSpan? startTime,
            TimeSpan? endTime)
        {
            return
                startTime.HasValue &&
                endTime.HasValue &&
                endTime.Value >
                startTime.Value;
        }

        private static bool IsOverlapping(
            TimeSpan firstStart,
            TimeSpan firstEnd,
            TimeSpan secondStart,
            TimeSpan secondEnd)
        {
            return
                firstStart < secondEnd &&
                secondStart < firstEnd;
        }
    }
}