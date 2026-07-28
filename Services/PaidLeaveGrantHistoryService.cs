using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Services
{
    public class PaidLeaveGrantHistoryService
    {
        private readonly ApplicationDbContext
            _context;

        public PaidLeaveGrantHistoryService(
            ApplicationDbContext context)
        {
            _context =
                context;
        }

        /// <summary>
        /// 現在の有給付与判定結果を
        /// 有給付与履歴へ登録または更新する。
        /// </summary>
        public PaidLeaveGrantHistory?
            SynchronizeCurrentGrantHistory(
                Employee employee,
                PaidLeaveRuleResult ruleResult)
        {
            // まだ初回付与日を迎えていない場合は
            // 付与履歴を作成しない
            if (!ruleResult.CurrentGrantDate.HasValue)
            {
                return null;
            }

            var grantDate =
                ruleResult.CurrentGrantDate
                    .Value
                    .Date;

            var history =
                _context.PaidLeaveGrantHistories
                    .FirstOrDefault(h =>
                        h.EmployeeId ==
                            employee.EmployeeId &&
                        h.GrantDate ==
                            grantDate
                    );

            var now =
                DateTime.Now;

            if (history == null)
            {
                history =
                    new PaidLeaveGrantHistory
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        GrantDate =
                            grantDate,

                        CreatedAt =
                            now
                    };

                _context.PaidLeaveGrantHistories
                    .Add(history);
            }

            history.AttendanceCheckStartDate =
                ruleResult
                    .AttendanceCheckStartDate
                    .Date;

            history.AttendanceCheckEndDate =
                ruleResult
                    .AttendanceCheckEndDate
                    .Date;

            history.TotalWorkDays =
                Math.Max(
                    0,
                    ruleResult
                        .TotalWorkDaysInCheckPeriod
                );

            history.AttendedDays =
                Math.Max(
                    0,
                    ruleResult
                        .AttendedDaysInCheckPeriod
                );

            history.AttendanceRate =
                Math.Max(
                    0,
                    ruleResult.AttendanceRate
                );

            history.IsAttendanceRateEnough =
                ruleResult
                    .IsAttendanceRateEnough;

            history.GrantStatus =
                ruleResult.IsEligible &&
                ruleResult.IsAttendanceRateEnough
                    ? "付与"
                    : "不付与";

            history.GrantedDays =
                history.GrantStatus == "付与"
                    ? Math.Max(
                        0,
                        ruleResult.GrantedDays
                    )
                    : 0;

            history.UsedDays =
                history.GrantStatus == "付与"
                    ? Math.Max(
                        0,
                        ruleResult
                            .UsedDaysAfterCurrentGrant
                    )
                    : 0;

            history.RemainingDays =
                Math.Max(
                    0,
                    history.GrantedDays -
                    history.UsedDays
                );

            history.ExpiryDate =
                history.GrantStatus == "付与"
                    ? ruleResult.LegalExpiryDate
                    : null;

            history.DecisionReason =
                string.IsNullOrWhiteSpace(
                    ruleResult.Message)
                    ? null
                    : ruleResult.Message.Length <= 500
                        ? ruleResult.Message
                        : ruleResult.Message[..500];

            history.UpdatedAt =
                now;

            _context.SaveChanges();

            return history;
        }

        /// <summary>
        /// 社員の有給付与履歴を
        /// 新しい付与日順で取得する。
        /// </summary>
        public List<PaidLeaveGrantHistory>
            GetEmployeeGrantHistories(
                int employeeId)
        {
            return _context
                .PaidLeaveGrantHistories
                .AsNoTracking()
                .Where(h =>
                    h.EmployeeId ==
                        employeeId)
                .OrderByDescending(h =>
                    h.GrantDate)
                .ThenByDescending(h =>
                    h.PaidLeaveGrantHistoryId)
                .ToList();
        }
    }
}