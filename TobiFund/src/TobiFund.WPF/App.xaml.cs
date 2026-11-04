using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using TobiFund.Application.Common;
using TobiFund.Application.Interfaces;
using TobiFund.Application.Services;
using TobiFund.Infrastructure.Data;
using TobiFund.Infrastructure.Services;
using TobiFund.WPF.Services;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF;

public partial class App : System.Windows.Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        QuestPDF.Settings.License = LicenseType.Community;

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        try
        {
            // اجرای Migrations + Seed در اولین اجرا
            var db = Services.GetRequiredService<TobiFundDbContext>();
            DbInitializer.Initialize(db);
            Services.GetRequiredService<IAppLogger>().Info("Database initialized.");
        }
        catch (Exception ex)
        {
            Services.GetRequiredService<IAppLogger>().Error("Database init failed", ex);
            MessageBox.Show("خطا در آماده‌سازی دیتابیس:" + Environment.NewLine + ex.Message,
                "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // حلقه ورود / خروج
        while (true)
        {
            var loginWindow = new LoginWindow
            {
                DataContext = Services.GetRequiredService<LoginViewModel>()
            };
            var ok = loginWindow.ShowDialog();
            if (ok != true) break;

            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            MainWindow = mainWindow;
            mainWindow.ShowDialog();
        }

        Shutdown();
    }

    private static void ConfigureServices(IServiceCollection s)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();
        s.AddSingleton<IConfiguration>(configuration);

        var dbPath = configuration["Database:Path"];
        var connectionString = TobiFundDbContext.BuildConnectionString(
            string.IsNullOrWhiteSpace(dbPath) ? null : dbPath);

        s.AddSingleton(new DbContextOptionsBuilder<TobiFundDbContext>().UseSqlite(connectionString).Options);
        s.AddSingleton<TobiFundDbContext>();
        s.AddSingleton<IApplicationDbContext>(sp => sp.GetRequiredService<TobiFundDbContext>());

        s.AddSingleton<IAppLogger, FileLogger>();
        s.AddSingleton<ICurrentUserService, CurrentUserService>();

        s.AddSingleton<IAuthService, AuthService>();
        s.AddSingleton<IMemberService, MemberService>();
        s.AddSingleton<ICapitalService, CapitalService>();
        s.AddSingleton<ILoanService, LoanService>();
        s.AddSingleton<ISubscriptionService, SubscriptionService>();
        s.AddSingleton<IScoreService, ScoreService>();
        s.AddSingleton<ISettingsService, SettingsService>();
        s.AddSingleton<IAuditService, AuditService>();
        s.AddSingleton<IBackupService, BackupService>();
        s.AddSingleton<IReportService, ReportService>();
        s.AddSingleton<IMemberImportService, MemberImportService>();

        s.AddSingleton<INavigationService, NavigationService>();
        s.AddSingleton<IExportService, ExportService>();
        s.AddSingleton<IExcelImportService, ExcelImportService>();

        s.AddSingleton<LoginViewModel>();
        s.AddSingleton<MainViewModel>();
        s.AddSingleton<ChangePasswordViewModel>();
        s.AddSingleton<DashboardViewModel>();
        s.AddSingleton<MembersViewModel>();
        s.AddSingleton<MemberDetailViewModel>();
        s.AddSingleton<CapitalViewModel>();
        s.AddSingleton<LoansViewModel>();
        s.AddSingleton<InstallmentsViewModel>();
        s.AddSingleton<SubscriptionsViewModel>();
        s.AddSingleton<RankingViewModel>();
        s.AddSingleton<ReportsViewModel>();
        s.AddSingleton<UsersViewModel>();
        s.AddSingleton<SettingsViewModel>();
        s.AddSingleton<BackupViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Services.GetService<IAuditService>()
                ?.LogAsync(AppConstants.AuditActions.Logout, description: "پایان اجرای برنامه.")
                .GetAwaiter().GetResult();
        }
        catch
        {
            // ignored
        }
        Services.Dispose();
        base.OnExit(e);
    }
}
