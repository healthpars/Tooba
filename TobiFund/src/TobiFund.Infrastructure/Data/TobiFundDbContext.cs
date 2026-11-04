using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TobiFund.Application.Common;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;

namespace TobiFund.Infrastructure.Data;

/// <summary>
/// DbContext اصلی برنامه (SQLite).
/// مسیر دیتابیس: %AppData%/TobiFund/Data/Fund.db
/// </summary>
public class TobiFundDbContext : DbContext, IApplicationDbContext
{
    public TobiFundDbContext(DbContextOptions<TobiFundDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<CapitalTransaction> CapitalTransactions => Set<CapitalTransaction>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<InstallmentPayment> InstallmentPayments => Set<InstallmentPayment>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();
    public DbSet<DailyScore> DailyScores => Set<DailyScore>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TobiFundDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>مسیر پیش‌فرض دیتابیس در AppData کاربر (مستقل از محل اجرای EXE)</summary>
    public static string GetDefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppConstants.AppFolderName, "Data", AppConstants.DatabaseFileName);
    }

    /// <summary>ساخت Connection String (پوشه دیتابیس در صورت نیاز ساخته می‌شود)</summary>
    public static string BuildConnectionString(string? databasePath)
    {
        var path = string.IsNullOrWhiteSpace(databasePath) ? GetDefaultDatabasePath() : databasePath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        return $"Data Source={path}";
    }
}
