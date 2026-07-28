using AttendanceManagementSystem.Data;
using AttendanceManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"
        )
    )
);

// Session
builder.Services.AddSession();

// HttpContext を Service から利用するため
builder.Services.AddHttpContextAccessor();

// Application Services
builder.Services.AddScoped<PaidLeaveRuleService>();
builder.Services.AddScoped<OperationLogService>();
builder.Services.AddScoped<AttendanceStampLogService>();
builder.Services.AddScoped<MonthlyClosingService>();
builder.Services.AddScoped<AttendanceCalculationService>();
builder.Services.AddScoped<CompanyCalendarService>();
builder.Services.AddScoped<PaidLeaveGrantHistoryService>();
builder.Services.AddScoped<PaidLeaveBalanceCalculationService>();


var app = builder.Build();

// 初期データ登録
using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    SeedData.Initialize(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// 初回ログイン時のパスワード変更強制処理
app.Use(async (httpContext, next) =>
{
    var employeeId =
        httpContext.Session.GetInt32("LoginUserId");

    if (employeeId.HasValue)
    {
        var controllerName =
            httpContext.Request.RouteValues["controller"]
                ?.ToString();

        var actionName =
            httpContext.Request.RouteValues["action"]
                ?.ToString();

        var isAccountAllowedAction =
            string.Equals(
                controllerName,
                "Account",
                StringComparison.OrdinalIgnoreCase
            ) &&
            (
                string.Equals(
                    actionName,
                    "Login",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    actionName,
                    "ChangePassword",
                    StringComparison.OrdinalIgnoreCase
                ) ||
                string.Equals(
                    actionName,
                    "Logout",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        var isErrorPage =
            string.Equals(
                controllerName,
                "Home",
                StringComparison.OrdinalIgnoreCase
            ) &&
            string.Equals(
                actionName,
                "Error",
                StringComparison.OrdinalIgnoreCase
            );

        if (!isAccountAllowedAction &&
            !isErrorPage)
        {
            var database =
                httpContext.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

            var employee =
                await database.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.EmployeeId == employeeId.Value
                    );

            // ユーザーが存在しない、または無効の場合
            if (employee == null ||
                !employee.IsActive)
            {
                httpContext.Session.Clear();

                httpContext.Response.Redirect(
                    "/Account/Login"
                );

                return;
            }

            // 初回パスワード変更が未完了の場合
            if (employee.MustChangePassword)
            {
                httpContext.Response.Redirect(
                    "/Account/ChangePassword"
                );

                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Account}/{action=Login}/{id?}"
);

app.Run();