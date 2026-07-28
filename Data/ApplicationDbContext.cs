using AttendanceManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceManagementSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =====================================
        // 基本テーブル
        // =====================================

        public DbSet<Employee>
            Employees
        {
            get;
            set;
        }

        public DbSet<Department>
            Departments
        {
            get;
            set;
        }

        public DbSet<Attendance>
            Attendances
        {
            get;
            set;
        }

        public DbSet<AttendanceCorrectionRequest>
            AttendanceCorrectionRequests
        {
            get;
            set;
        }

        public DbSet<PaidLeaveRequest>
            PaidLeaveRequests
        {
            get;
            set;
        }

        public DbSet<PaidLeaveBalance>
            PaidLeaveBalances
        {
            get;
            set;
        }

        // 有給付与履歴
        public DbSet<PaidLeaveGrantHistory>
            PaidLeaveGrantHistories
        {
            get;
            set;
        }

        // 操作ログ
        public DbSet<OperationLog>
            OperationLogs
        {
            get;
            set;
        }

        // 出勤・退勤の打刻ログ
        public DbSet<AttendanceStampLog>
            AttendanceStampLogs
        {
            get;
            set;
        }

        // 月次締め処理
        public DbSet<MonthlyClosing>
            MonthlyClosings
        {
            get;
            set;
        }

        // 会社カレンダー
        public DbSet<CompanyCalendarDay>
            CompanyCalendarDays
        {
            get;
            set;
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================
            // 社員と部署
            // =====================================

            modelBuilder.Entity<Employee>()
                .HasOne(e =>
                    e.Department)
                .WithMany(d =>
                    d.Employees)
                .HasForeignKey(e =>
                    e.DepartmentId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            // =====================================
            // 勤怠と社員
            // =====================================

            modelBuilder.Entity<Attendance>()
                .HasOne(a =>
                    a.Employee)
                .WithMany()
                .HasForeignKey(a =>
                    a.EmployeeId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            // =====================================
            // 勤怠修正申請
            // =====================================

            modelBuilder
                .Entity<AttendanceCorrectionRequest>()
                .HasOne(r =>
                    r.Employee)
                .WithMany()
                .HasForeignKey(r =>
                    r.EmployeeId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            modelBuilder
                .Entity<AttendanceCorrectionRequest>()
                .HasOne(r =>
                    r.Attendance)
                .WithMany()
                .HasForeignKey(r =>
                    r.AttendanceId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            // =====================================
            // 有給申請
            // =====================================

            modelBuilder.Entity<PaidLeaveRequest>()
                .HasOne(p =>
                    p.Employee)
                .WithMany()
                .HasForeignKey(p =>
                    p.EmployeeId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );
            // =====================================
            // 有給残日数
            // =====================================

            modelBuilder.Entity<PaidLeaveBalance>(
                entity =>
                {
                    entity.HasKey(p =>
                        p.PaidLeaveBalanceId
                    );

                    entity.HasOne(p =>
                            p.Employee)
                        .WithMany()
                        .HasForeignKey(p =>
                            p.EmployeeId)
                        .OnDelete(
                            DeleteBehavior.Restrict
                        );

                    // 同じ社員・同じ管理年度の
                    // 重複登録を禁止する
                    entity.HasIndex(p =>
                        new
                        {
                            p.EmployeeId,
                            p.Year
                        })
                        .IsUnique();

                    entity.HasIndex(p =>
                        p.EmployeeId
                    );

                    entity.HasIndex(p =>
                        p.Year
                    );

                    // 日付のみ保存する
                    entity.Property(p =>
                            p.CurrentGrantDate)
                        .HasColumnType("date");

                    entity.Property(p =>
                            p.CurrentGrantExpiryDate)
                        .HasColumnType("date");

                    entity.HasIndex(p =>
                        p.CurrentGrantDate
                    );

                    entity.HasIndex(p =>
                        p.CurrentGrantExpiryDate
                    );
                }
            );

            // =====================================
            // 有給付与履歴
            // =====================================

            modelBuilder.Entity<PaidLeaveGrantHistory>(
                entity =>
                {
                    entity.HasKey(h =>
                        h.PaidLeaveGrantHistoryId
                    );

                    entity.HasOne(h =>
                            h.Employee)
                        .WithMany()
                        .HasForeignKey(h =>
                            h.EmployeeId)
                        .OnDelete(
                            DeleteBehavior.Restrict
                        );

                    // 日付のみ保存する
                    entity.Property(h =>
                            h.GrantDate)
                        .HasColumnType("date");

                    entity.Property(h =>
                            h.AttendanceCheckStartDate)
                        .HasColumnType("date");

                    entity.Property(h =>
                            h.AttendanceCheckEndDate)
                        .HasColumnType("date");

                    entity.Property(h =>
                            h.ExpiryDate)
                        .HasColumnType("date");

                    entity.Property(h =>
                            h.GrantStatus)
                        .HasMaxLength(20)
                        .IsRequired();

                    entity.Property(h =>
                            h.DecisionReason)
                        .HasMaxLength(500);

                    // 同じ社員・同じ付与日の
                    // 重複登録を禁止する
                    entity.HasIndex(h =>
                        new
                        {
                            h.EmployeeId,
                            h.GrantDate
                        })
                        .IsUnique();

                    entity.HasIndex(h =>
                        h.EmployeeId
                    );

                    entity.HasIndex(h =>
                        h.GrantDate
                    );

                    entity.HasIndex(h =>
                        h.GrantStatus
                    );
                }
            );

            // =====================================
            // 操作ログ
            // =====================================

            // 社員情報が削除されても
            // 操作ログは残す
            modelBuilder.Entity<OperationLog>()
                .HasOne(l =>
                    l.Employee)
                .WithMany()
                .HasForeignKey(l =>
                    l.EmployeeId)
                .OnDelete(
                    DeleteBehavior.SetNull
                );

            modelBuilder.Entity<OperationLog>()
                .HasIndex(l =>
                    l.CreatedAt);

            modelBuilder.Entity<OperationLog>()
                .HasIndex(l =>
                    l.EmployeeId);

            modelBuilder.Entity<OperationLog>()
                .HasIndex(l =>
                    l.ActionName);

            // =====================================
            // 打刻ログ
            // =====================================

            modelBuilder.Entity<AttendanceStampLog>()
                .HasOne(l =>
                    l.Attendance)
                .WithMany()
                .HasForeignKey(l =>
                    l.AttendanceId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            modelBuilder.Entity<AttendanceStampLog>()
                .HasOne(l =>
                    l.Employee)
                .WithMany()
                .HasForeignKey(l =>
                    l.EmployeeId)
                .OnDelete(
                    DeleteBehavior.Restrict
                );

            modelBuilder.Entity<AttendanceStampLog>()
                .HasIndex(l =>
                    l.CreatedAt);

            modelBuilder.Entity<AttendanceStampLog>()
                .HasIndex(l =>
                    l.StampedAt);

            modelBuilder.Entity<AttendanceStampLog>()
                .HasIndex(l =>
                    l.EmployeeId);

            modelBuilder.Entity<AttendanceStampLog>()
                .HasIndex(l =>
                    l.AttendanceId);

            modelBuilder.Entity<AttendanceStampLog>()
                .HasIndex(l =>
                    l.StampType);

            modelBuilder.Entity<AttendanceStampLog>()
                .HasIndex(l =>
                    new
                    {
                        l.EmployeeId,
                        l.StampedAt
                    });

            // =====================================
            // 月次締め処理
            // =====================================

            modelBuilder.Entity<MonthlyClosing>()
                .HasOne(m =>
                    m.ClosedByEmployee)
                .WithMany()
                .HasForeignKey(m =>
                    m.ClosedByEmployeeId)
                .OnDelete(
                    DeleteBehavior.NoAction
                );

            modelBuilder.Entity<MonthlyClosing>()
                .HasOne(m =>
                    m.ReopenedByEmployee)
                .WithMany()
                .HasForeignKey(m =>
                    m.ReopenedByEmployeeId)
                .OnDelete(
                    DeleteBehavior.NoAction
                );

            // 同じ年月の重複登録を禁止
            modelBuilder.Entity<MonthlyClosing>()
                .HasIndex(m =>
                    new
                    {
                        m.TargetYear,
                        m.TargetMonth
                    })
                .IsUnique();

            modelBuilder.Entity<MonthlyClosing>()
                .HasIndex(m =>
                    m.IsClosed);

            // =====================================
            // 会社カレンダー
            // =====================================

            // 時刻部分を保存せず、
            // 日付だけを保存する
            modelBuilder.Entity<CompanyCalendarDay>()
                .Property(c =>
                    c.CalendarDate)
                .HasColumnType("date");

            // 同じ日付の重複登録を禁止する
            modelBuilder.Entity<CompanyCalendarDay>()
                .HasIndex(c =>
                    c.CalendarDate)
                .IsUnique();

            // 日付区分による検索を高速化する
            modelBuilder.Entity<CompanyCalendarDay>()
                .HasIndex(c =>
                    c.DayType);

            // 出勤日・休日による検索を高速化する
            modelBuilder.Entity<CompanyCalendarDay>()
                .HasIndex(c =>
                    c.IsWorkingDay);

            // 日付と出勤日状態を同時に検索する
            modelBuilder.Entity<CompanyCalendarDay>()
                .HasIndex(c =>
                    new
                    {
                        c.CalendarDate,
                        c.IsWorkingDay
                    });

            modelBuilder.Entity<CompanyCalendarDay>()
                .Property(c =>
                    c.DayType)
                .HasMaxLength(30)
                .IsRequired();

            modelBuilder.Entity<CompanyCalendarDay>()
                .Property(c =>
                    c.HolidayName)
                .HasMaxLength(100);

            modelBuilder.Entity<CompanyCalendarDay>()
                .Property(c =>
                    c.Note)
                .HasMaxLength(500);
        }
    }
}