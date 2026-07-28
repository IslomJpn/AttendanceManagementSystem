using System;
using System.Collections.Generic;
using System.Linq;

namespace AttendanceManagementSystem.Services
{
    public class PaidLeaveRuleService
    {
        public PaidLeaveRuleResult Calculate(
            DateTime joinDate,
            DateTime targetDate,
            int totalWorkDaysInCheckPeriod,
            int attendedDaysInCheckPeriod,
            double usedDaysAfterCurrentGrant)
        {
            joinDate =
                joinDate.Date;

            targetDate =
                targetDate.Date;

            DateTime? currentGrantDate =
                GetCurrentGrantDate(
                    joinDate,
                    targetDate
                );

            DateTime nextGrantDate =
                GetNextGrantDate(
                    joinDate,
                    targetDate
                );

            double attendanceRate =
                CalculateAttendanceRate(
                    totalWorkDaysInCheckPeriod,
                    attendedDaysInCheckPeriod
                );

            if (currentGrantDate == null)
            {
                return new PaidLeaveRuleResult
                {
                    IsEligible =
                        false,

                    JoinDate =
                        joinDate,

                    TargetDate =
                        targetDate,

                    CurrentGrantDate =
                        null,

                    NextGrantDate =
                        nextGrantDate,

                    GrantedDays =
                        0,

                    TotalWorkDaysInCheckPeriod =
                        totalWorkDaysInCheckPeriod,

                    AttendedDaysInCheckPeriod =
                        attendedDaysInCheckPeriod,

                    AttendanceRate =
                        attendanceRate,

                    IsAttendanceRateEnough =
                        false,

                    UsedDaysAfterCurrentGrant =
                        usedDaysAfterCurrentGrant,

                    RemainingFiveDayRequirement =
                        0,

                    IsFiveDayAlertTarget =
                        false,

                    Message =
                        "入社から6か月未満のため、有給付与対象外です。"
                };
            }

            DateTime grantDate =
                currentGrantDate.Value;

            DateTime checkStartDate =
                GetAttendanceCheckStartDate(
                    joinDate,
                    grantDate
                );

            DateTime checkEndDate =
                grantDate.AddDays(-1);

            bool isAttendanceRateEnough =
                totalWorkDaysInCheckPeriod > 0 &&
                attendanceRate >= 80.0;

            int grantedDays =
                GetGrantedDays(
                    joinDate,
                    grantDate
                );

            if (!isAttendanceRateEnough)
            {
                return new PaidLeaveRuleResult
                {
                    IsEligible =
                        false,

                    JoinDate =
                        joinDate,

                    TargetDate =
                        targetDate,

                    AttendanceCheckStartDate =
                        checkStartDate,

                    AttendanceCheckEndDate =
                        checkEndDate,

                    CurrentGrantDate =
                        grantDate,

                    NextGrantDate =
                        nextGrantDate,

                    GrantedDays =
                        0,

                    TotalWorkDaysInCheckPeriod =
                        totalWorkDaysInCheckPeriod,

                    AttendedDaysInCheckPeriod =
                        attendedDaysInCheckPeriod,

                    AttendanceRate =
                        attendanceRate,

                    IsAttendanceRateEnough =
                        false,

                    UsedDaysAfterCurrentGrant =
                        usedDaysAfterCurrentGrant,

                    RemainingFiveDayRequirement =
                        0,

                    IsFiveDayAlertTarget =
                        false,

                    Message =
                        "出勤率が80%未満のため、有給付与対象外です。"
                };
            }

            DateTime fiveDayDeadline =
                grantDate
                    .AddYears(1)
                    .AddDays(-1);

            DateTime legalExpiryDate =
                grantDate
                    .AddYears(2)
                    .AddDays(-1);

            double remainingFiveDayRequirement =
                Math.Max(
                    0,
                    5 - usedDaysAfterCurrentGrant
                );

            bool isFiveDayAlertTarget =
                grantedDays >= 10 &&
                remainingFiveDayRequirement > 0 &&
                targetDate <= fiveDayDeadline;

            string message;

            if (isFiveDayAlertTarget)
            {
                message =
                    $"年5日取得義務があります。" +
                    $"{fiveDayDeadline:yyyy/MM/dd}までに、" +
                    $"あと{remainingFiveDayRequirement:0.#}日" +
                    "取得が必要です。";
            }
            else
            {
                message =
                    "年5日取得義務は達成済み、" +
                    "またはアラート対象外です。";
            }

            return new PaidLeaveRuleResult
            {
                IsEligible =
                    true,

                JoinDate =
                    joinDate,

                TargetDate =
                    targetDate,

                AttendanceCheckStartDate =
                    checkStartDate,

                AttendanceCheckEndDate =
                    checkEndDate,

                CurrentGrantDate =
                    grantDate,

                NextGrantDate =
                    nextGrantDate,

                FiveDayDeadline =
                    fiveDayDeadline,

                LegalExpiryDate =
                    legalExpiryDate,

                GrantedDays =
                    grantedDays,

                TotalWorkDaysInCheckPeriod =
                    totalWorkDaysInCheckPeriod,

                AttendedDaysInCheckPeriod =
                    attendedDaysInCheckPeriod,

                AttendanceRate =
                    attendanceRate,

                IsAttendanceRateEnough =
                    true,

                UsedDaysAfterCurrentGrant =
                    usedDaysAfterCurrentGrant,

                RemainingFiveDayRequirement =
                    remainingFiveDayRequirement,

                IsFiveDayAlertTarget =
                    isFiveDayAlertTarget,

                Message =
                    message
            };
        }

        /// <summary>
        /// 入社日から対象日までに到来した
        /// すべての有給付与期間を取得する。
        /// </summary>
        public List<PaidLeaveGrantPeriod>
            GetGrantPeriods(
                DateTime joinDate,
                DateTime targetDate)
        {
            joinDate =
                joinDate.Date;

            targetDate =
                targetDate.Date;

            return GetGrantDates(
                    joinDate,
                    targetDate
                )
                .Where(grantDate =>
                    grantDate <= targetDate)
                .OrderBy(grantDate =>
                    grantDate)
                .Select(grantDate =>
                    new PaidLeaveGrantPeriod
                    {
                        GrantDate =
                            grantDate,

                        AttendanceCheckStartDate =
                            GetAttendanceCheckStartDate(
                                joinDate,
                                grantDate
                            ),

                        AttendanceCheckEndDate =
                            grantDate.AddDays(-1),

                        GrantedDays =
                            GetGrantedDays(
                                joinDate,
                                grantDate
                            ),

                        ExpiryDate =
                            grantDate
                                .AddYears(2)
                                .AddDays(-1)
                    })
                .ToList();
        }

        private double CalculateAttendanceRate(
            int totalWorkDays,
            int attendedDays)
        {
            if (totalWorkDays <= 0)
            {
                return 0;
            }

            return Math.Round(
                (double)attendedDays /
                totalWorkDays *
                100,
                1
            );
        }

        private DateTime? GetCurrentGrantDate(
            DateTime joinDate,
            DateTime targetDate)
        {
            List<DateTime> grantDates =
                GetGrantDates(
                    joinDate,
                    targetDate
                );

            DateTime? currentGrantDate =
                grantDates
                    .Where(date =>
                        date <= targetDate)
                    .OrderBy(date =>
                        date)
                    .LastOrDefault();

            if (currentGrantDate ==
                DateTime.MinValue)
            {
                return null;
            }

            return currentGrantDate;
        }

        private DateTime GetNextGrantDate(
            DateTime joinDate,
            DateTime targetDate)
        {
            List<DateTime> grantDates =
                GetGrantDates(
                    joinDate,
                    targetDate.AddYears(2)
                );

            return grantDates
                .Where(date =>
                    date > targetDate)
                .OrderBy(date =>
                    date)
                .First();
        }

        private List<DateTime> GetGrantDates(
            DateTime joinDate,
            DateTime untilDate)
        {
            var grantDates =
                new List<DateTime>
                {
                    joinDate.AddMonths(6),
                    joinDate.AddMonths(18),
                    joinDate.AddMonths(30),
                    joinDate.AddMonths(42),
                    joinDate.AddMonths(54),
                    joinDate.AddMonths(66),
                    joinDate.AddMonths(78)
                };

            DateTime nextDate =
                joinDate
                    .AddMonths(78)
                    .AddYears(1);

            while (nextDate <=
                   untilDate.AddYears(2))
            {
                grantDates.Add(
                    nextDate
                );

                nextDate =
                    nextDate.AddYears(1);
            }

            return grantDates;
        }

        private DateTime GetAttendanceCheckStartDate(
            DateTime joinDate,
            DateTime currentGrantDate)
        {
            if (currentGrantDate ==
                joinDate.AddMonths(6))
            {
                return joinDate;
            }

            List<DateTime> grantDates =
                GetGrantDates(
                    joinDate,
                    currentGrantDate
                );

            return grantDates
                .Where(date =>
                    date < currentGrantDate)
                .OrderBy(date =>
                    date)
                .Last();
        }

        private int GetGrantedDays(
            DateTime joinDate,
            DateTime grantDate)
        {
            int months =
                GetMonthDifference(
                    joinDate,
                    grantDate
                );

            if (months >= 78)
            {
                return 20;
            }

            if (months >= 66)
            {
                return 18;
            }

            if (months >= 54)
            {
                return 16;
            }

            if (months >= 42)
            {
                return 14;
            }

            if (months >= 30)
            {
                return 12;
            }

            if (months >= 18)
            {
                return 11;
            }

            if (months >= 6)
            {
                return 10;
            }

            return 0;
        }

        private int GetMonthDifference(
            DateTime startDate,
            DateTime endDate)
        {
            return
                (endDate.Year - startDate.Year) *
                12 +
                endDate.Month -
                startDate.Month;
        }
    }

    public class PaidLeaveGrantPeriod
    {
        public DateTime GrantDate
        {
            get;
            set;
        }

        public DateTime AttendanceCheckStartDate
        {
            get;
            set;
        }

        public DateTime AttendanceCheckEndDate
        {
            get;
            set;
        }

        public int GrantedDays
        {
            get;
            set;
        }

        public DateTime ExpiryDate
        {
            get;
            set;
        }
    }

    public class PaidLeaveRuleResult
    {
        public bool IsEligible
        {
            get;
            set;
        }

        public DateTime JoinDate
        {
            get;
            set;
        }

        public DateTime TargetDate
        {
            get;
            set;
        }

        public DateTime AttendanceCheckStartDate
        {
            get;
            set;
        }

        public DateTime AttendanceCheckEndDate
        {
            get;
            set;
        }

        public DateTime? CurrentGrantDate
        {
            get;
            set;
        }

        public DateTime NextGrantDate
        {
            get;
            set;
        }

        public DateTime? FiveDayDeadline
        {
            get;
            set;
        }

        public DateTime? LegalExpiryDate
        {
            get;
            set;
        }

        public int GrantedDays
        {
            get;
            set;
        }

        public int TotalWorkDaysInCheckPeriod
        {
            get;
            set;
        }

        public int AttendedDaysInCheckPeriod
        {
            get;
            set;
        }

        public double AttendanceRate
        {
            get;
            set;
        }

        public bool IsAttendanceRateEnough
        {
            get;
            set;
        }

        public double UsedDaysAfterCurrentGrant
        {
            get;
            set;
        }

        public double RemainingFiveDayRequirement
        {
            get;
            set;
        }

        public bool IsFiveDayAlertTarget
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        } = string.Empty;
    }
}