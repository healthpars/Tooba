using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Interfaces;
using TobiFund.Application.Services;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;
using TobiFund.Infrastructure.Data;
using TobiFund.Infrastructure.Services;

namespace TobiFund.Tests;

/// <summary>زیرساخت تست — سرویس‌ها با دیتابیس SQLite داخل حافظه</summary>
public sealed class TestServices : IDisposable
{
    private readonly SqliteConnection _connection;

    public TobiFundDbContext Context { get; }
    public IAuthService Auth { get; }
    public IMemberService Members { get; }
    public ICapitalService Capital { get; }
    public ILoanService Loans { get; }
    public ISubscriptionService Subscriptions { get; }
    public IScoreService Scores { get; }
    public ISettingsService Settings { get; }
    public IAuditService Audit { get; }
    public ICurrentUserService CurrentUser { get; }
    public IAppLogger Logger { get; } = new NullLogger();

    public TestServices()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TobiFundDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new TobiFundDbContext(options);
        Context.Database.EnsureCreated();

        CurrentUser = new CurrentUserService();
        CurrentUser.SetCurrentUser(new TobiFund.Application.DTOs.UserDto
        {
            Id = 1,
            Username = "tester",
            FullName = "کاربر تست",
            Role = UserRole.Admin
        });

        Audit = new AuditService(Context, CurrentUser, Logger);
        Settings = new SettingsService(Context, Audit, CurrentUser, Logger);
        Auth = new AuthService(Context, Audit, Logger);
        Members = new MemberService(Context, Audit, CurrentUser, Logger);
        Capital = new CapitalService(Context, Audit, CurrentUser, Logger);
        Loans = new LoanService(Context, Audit, CurrentUser, Settings, Logger);
        Subscriptions = new SubscriptionService(Context, Audit, CurrentUser, Settings, Logger);
        Scores = new ScoreService(Context, Settings, Logger);

        SeedDefaults();
    }

    private void SeedDefaults()
    {
        foreach (var kv in TobiFund.Application.Common.SettingKeys.Defaults)
        {
            Context.Settings.Add(new Setting { Key = kv.Key, Value = kv.Value });
        }
        Context.SaveChanges();
    }

    /// <summary>ایجاد یک عضو فعال و برگرداندن شناسه آن</summary>
    public async Task<int> CreateMemberAsync(
        string code = "1001",
        string firstName = "علی",
        string lastName = "رضایی",
        DateTime? joinDate = null)
    {
        var result = await Members.CreateAsync(new TobiFund.Application.DTOs.CreateMemberDto
        {
            MemberCode = code,
            FirstName = firstName,
            LastName = lastName,
            JoinDate = joinDate ?? DateTime.Today
        });
        if (!result.Success)
            throw new InvalidOperationException("CreateMember failed: " + result.Message);
        return result.Data!.Id;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    private sealed class NullLogger : IAppLogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception? exception = null) { }
    }
}
