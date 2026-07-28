using AttendanceManagementSystem.Helpers;
using AttendanceManagementSystem.Models;

namespace AttendanceManagementSystem.Data
{
    public static class SeedData
    {
        private const string AdminEmail =
            "admin@example.com";

        private const string EmployeeEmail =
            "employee@example.com";

        public static void Initialize(
            ApplicationDbContext context)
        {
            EnsureDepartments(
                context
            );

            EnsureInitialEmployees(
                context
            );

            EnsureCurrentYearPaidLeaveBalances(
                context
            );
        }

        /// <summary>
        /// 基本部署を重複しないように登録する。
        /// </summary>
        private static void EnsureDepartments(
            ApplicationDbContext context)
        {
            var requiredDepartmentNames =
                new[]
                {
                    "営業部",
                    "開発部",
                    "管理部"
                };

            var existingDepartmentNames =
                context.Departments
                    .Select(department =>
                        department.DepartmentName)
                    .ToHashSet();

            foreach (var departmentName
                     in requiredDepartmentNames)
            {
                if (existingDepartmentNames.Contains(
                        departmentName))
                {
                    continue;
                }

                context.Departments.Add(
                    new Department
                    {
                        DepartmentName =
                            departmentName
                    }
                );
            }

            context.SaveChanges();
        }

        /// <summary>
        /// 初期管理者と初期社員を登録する。
        /// 初期パスワードで作成し、
        /// 初回ログイン時にパスワード変更を必須にする。
        /// メールアドレスを基準に重複登録を防止する。
        /// </summary>
        private static void EnsureInitialEmployees(
            ApplicationDbContext context)
        {
            var developmentDepartment =
                context.Departments
                    .First(department =>
                        department.DepartmentName ==
                        "開発部");

            var adminDepartment =
                context.Departments
                    .First(department =>
                        department.DepartmentName ==
                        "管理部");

            if (!context.Employees.Any(employee =>
                    employee.Email ==
                    AdminEmail))
            {
                context.Employees.Add(
                    new Employee
                    {
                        Name =
                            "管理者 太郎",

                        Email =
                            AdminEmail,

                        PasswordHash =
                            PasswordHelper.HashPassword(
                                "password"
                            ),

                        DepartmentId =
                            adminDepartment.DepartmentId,

                        Role =
                            "Admin",

                        JoinDate =
                            DateTime.Today,

                        IsActive =
                            true,

                        MustChangePassword =
                            true
                    }
                );
            }

            if (!context.Employees.Any(employee =>
                    employee.Email ==
                    EmployeeEmail))
            {
                context.Employees.Add(
                    new Employee
                    {
                        Name =
                            "山田 太郎",

                        Email =
                            EmployeeEmail,

                        PasswordHash =
                            PasswordHelper.HashPassword(
                                "password"
                            ),

                        DepartmentId =
                            developmentDepartment
                                .DepartmentId,

                        Role =
                            "Employee",

                        JoinDate =
                            DateTime.Today,

                        IsActive =
                            true,

                        MustChangePassword =
                            true
                    }
                );
            }

            context.SaveChanges();
        }

        /// <summary>
        /// 当年の有給残高レコードが存在しない社員に
        /// 初期レコードだけを作成する。
        ///
        /// 正式な残高は有給状況画面で、
        /// 付与履歴・繰越・失効・申請状況から再計算される。
        /// </summary>
        private static void
            EnsureCurrentYearPaidLeaveBalances(
                ApplicationDbContext context)
        {
            var currentYear =
                DateTime.Today.Year;

            var now =
                DateTime.Now;

            var existingEmployeeIds =
                context.PaidLeaveBalances
                    .Where(balance =>
                        balance.Year ==
                            currentYear)
                    .Select(balance =>
                        balance.EmployeeId)
                    .ToHashSet();

            var missingEmployees =
                context.Employees
                    .Where(employee =>
                        !existingEmployeeIds.Contains(
                            employee.EmployeeId
                        ))
                    .ToList();

            foreach (var employee
                     in missingEmployees)
            {
                context.PaidLeaveBalances.Add(
                    new PaidLeaveBalance
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        Year =
                            currentYear,

                        CurrentGrantedDays =
                            0,

                        CarriedOverDays =
                            0,

                        ExpiredDays =
                            0,

                        GrantedDays =
                            0,

                        UsedDays =
                            0,

                        ReservedDays =
                            0,

                        RemainingDays =
                            0,

                        CreatedAt =
                            now,

                        UpdatedAt =
                            now
                    }
                );
            }

            if (missingEmployees.Any())
            {
                context.SaveChanges();
            }
        }
    }
}