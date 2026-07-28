using AttendanceManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Services
{
    public class PaidLeaveBalanceCalculationService
    {
        private readonly ApplicationDbContext
            _context;

        public PaidLeaveBalanceCalculationService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public PaidLeaveBalanceCalculationResult Calculate(
            int employeeId,
            DateTime targetDate)
        {
            targetDate = targetDate.Date;

            var histories =
                _context.PaidLeaveGrantHistories
                    .AsNoTracking()
                    .Where(history =>
                        history.EmployeeId == employeeId &&
                        history.GrantStatus == "付与" &&
                        history.GrantedDays > 0 &&
                        history.GrantDate <= targetDate)
                    .OrderBy(history =>
                        history.GrantDate)
                    .ThenBy(history =>
                        history.PaidLeaveGrantHistoryId)
                    .ToList();

            if (!histories.Any())
            {
                return new PaidLeaveBalanceCalculationResult
                {
                    TargetDate = targetDate
                };
            }

            var buckets =
                histories
                    .Select(history =>
                        new PaidLeaveGrantBucket
                        {
                            PaidLeaveGrantHistoryId =
                                history.PaidLeaveGrantHistoryId,

                            GrantDate =
                                history.GrantDate.Date,

                            ExpiryDate =
                                (
                                    history.ExpiryDate ??
                                    history.GrantDate
                                        .AddYears(2)
                                        .AddDays(-1)
                                ).Date,

                            GrantedDays =
                                Math.Max(
                                    0,
                                    history.GrantedDays
                                ),

                            RemainingDays =
                                Math.Max(
                                    0,
                                    history.GrantedDays
                                )
                        })
                    .ToList();

            var approvedRequests =
                _context.PaidLeaveRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.EmployeeId == employeeId &&
                        request.Status == "承認" &&
                        request.Days > 0)
                    .OrderBy(request =>
                        request.LeaveDate)
                    .ThenBy(request =>
                        request.CreatedAt)
                    .ThenBy(request =>
                        request.PaidLeaveRequestId)
                    .ToList();

            double reservedDays = 0;


            foreach (var request in approvedRequests)
            {
                var leaveDate =
                    request.LeaveDate.Date;

                var remainingUsage =
                    Math.Max(
                        0,
                        request.Days
                    );
                var allocatedUsage =
                    0.0;

                // 有効期限が近い古い付与分から先に使用する
                var availableBuckets =
                    buckets
                        .Where(bucket =>
                            bucket.GrantDate <= leaveDate &&
                            bucket.ExpiryDate >= leaveDate &&
                            bucket.RemainingDays > 0)
                        .OrderBy(bucket =>
                            bucket.ExpiryDate)
                        .ThenBy(bucket =>
                            bucket.GrantDate)
                        .ToList();

                foreach (var bucket
                         in availableBuckets)
                {
                    if (remainingUsage <= 0)
                    {
                        break;
                    }

                    var usedFromBucket =
                        Math.Min(
                            bucket.RemainingDays,
                            remainingUsage
                        );

                    bucket.RemainingDays -=
                        usedFromBucket;

                    bucket.UsedDays +=
                        usedFromBucket;

                    allocatedUsage +=
                        usedFromBucket;

                    remainingUsage -=
                        usedFromBucket;
                }

                if (leaveDate > targetDate)
                {
                    reservedDays +=
                        allocatedUsage;
                }
            }

            var currentBucket =
                buckets
                    .Where(bucket =>
                        bucket.GrantDate <= targetDate)
                    .OrderByDescending(bucket =>
                        bucket.GrantDate)
                    .First();

            var currentGrantDate =
                currentBucket.GrantDate;

            // 現在の付与日の直前までに使用された分を反映し、
            // 付与日時点で繰越可能だった残日数を計算する
            var carryoverBuckets =
                histories
                    .Where(history =>
                        history.GrantDate.Date <
                            currentGrantDate)
                    .Select(history =>
                        new PaidLeaveGrantBucket
                        {
                            GrantDate =
                                history.GrantDate.Date,

                            ExpiryDate =
                                (
                                    history.ExpiryDate ??
                                    history.GrantDate
                                        .AddYears(2)
                                        .AddDays(-1)
                                ).Date,

                            GrantedDays =
                                Math.Max(
                                    0,
                                    history.GrantedDays
                                ),

                            RemainingDays =
                                Math.Max(
                                    0,
                                    history.GrantedDays
                                )
                        })
                    .ToList();

            var requestsBeforeCurrentGrant =
                approvedRequests
                    .Where(request =>
                        request.LeaveDate.Date <
                            currentGrantDate)
                    .ToList();

            foreach (var request
                     in requestsBeforeCurrentGrant)
            {
                var leaveDate =
                    request.LeaveDate.Date;

                var remainingUsage =
                    Math.Max(
                        0,
                        request.Days
                    );

                var availableBuckets =
                    carryoverBuckets
                        .Where(bucket =>
                            bucket.GrantDate <= leaveDate &&
                            bucket.ExpiryDate >= leaveDate &&
                            bucket.RemainingDays > 0)
                        .OrderBy(bucket =>
                            bucket.ExpiryDate)
                        .ThenBy(bucket =>
                            bucket.GrantDate)
                        .ToList();

                foreach (var bucket
                         in availableBuckets)
                {
                    if (remainingUsage <= 0)
                    {
                        break;
                    }

                    var usedFromBucket =
                        Math.Min(
                            bucket.RemainingDays,
                            remainingUsage
                        );

                    bucket.RemainingDays -=
                        usedFromBucket;

                    remainingUsage -=
                        usedFromBucket;
                }
            }

            var carriedOverDays =
                carryoverBuckets
                    .Where(bucket =>
                        bucket.ExpiryDate >=
                            currentGrantDate)
                    .Sum(bucket =>
                        bucket.RemainingDays);

            var expiredDays =
                buckets
                    .Where(bucket =>
                        bucket.ExpiryDate <
                            targetDate)
                    .Sum(bucket =>
                        bucket.RemainingDays);

            var remainingDays =
                buckets
                    .Where(bucket =>
                        bucket.ExpiryDate >=
                            targetDate)
                    .Sum(bucket =>
                        bucket.RemainingDays);

            var usedDaysAfterCurrentGrant =
                approvedRequests
                    .Where(request =>
                        request.LeaveDate.Date >=
                            currentGrantDate &&
                        request.LeaveDate.Date <=
                            targetDate)
                    .Sum(request =>
                        request.Days);

            var currentGrantedDays =
                currentBucket.GrantedDays;

            var availableGrantedDays =
                currentGrantedDays +
                carriedOverDays;

            return new PaidLeaveBalanceCalculationResult
            {
                TargetDate =
                    targetDate,

                CurrentGrantDate =
                    currentGrantDate,

                CurrentGrantExpiryDate =
                    currentBucket.ExpiryDate,

                CurrentGrantedDays =
                    currentGrantedDays,

                CarriedOverDays =
                    carriedOverDays,

                ExpiredDays =
                    expiredDays,

                GrantedDays =
                    availableGrantedDays,

                UsedDays =
                    Math.Max(
                        0,
                        usedDaysAfterCurrentGrant
                    ),

                ReservedDays =
                    Math.Max(
                        0,
                        reservedDays
                    ),

                RemainingDays =
                    Math.Max(
                        0,
                        remainingDays
                    )
            };
        }

        private class PaidLeaveGrantBucket
        {
            public int PaidLeaveGrantHistoryId
            {
                get;
                set;
            }

            public DateTime GrantDate
            {
                get;
                set;
            }

            public DateTime ExpiryDate
            {
                get;
                set;
            }

            public double GrantedDays
            {
                get;
                set;
            }

            public double UsedDays
            {
                get;
                set;
            }

            public double RemainingDays
            {
                get;
                set;
            }
        }
    }

    public class PaidLeaveBalanceCalculationResult
    {
        public DateTime TargetDate
        {
            get;
            set;
        }

        public DateTime? CurrentGrantDate
        {
            get;
            set;
        }

        public DateTime? CurrentGrantExpiryDate
        {
            get;
            set;
        }

        public double CurrentGrantedDays
        {
            get;
            set;
        }

        public double CarriedOverDays
        {
            get;
            set;
        }

        public double ExpiredDays
        {
            get;
            set;
        }

        public double GrantedDays
        {
            get;
            set;
        }

        public double UsedDays
        {
            get;
            set;
        }

        public double ReservedDays
        {
            get;
            set;
        }

        public double RemainingDays
        {
            get;
            set;
        }
    }
}