using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TobiFund.Domain.Entities;

namespace TobiFund.Application.Interfaces;

/// <summary>არ contextual DbContext که توسط لایه Infrastructure پیاده‌سازی می‌شود</summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Member> Members { get; }
    DbSet<CapitalTransaction> CapitalTransactions { get; }
    DbSet<Loan> Loans { get; }
    DbSet<LoanInstallment> LoanInstallments { get; }
    DbSet<InstallmentPayment> InstallmentPayments { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SubscriptionPayment> SubscriptionPayments { get; }
    DbSet<DailyScore> DailyScores { get; }
    DbSet<Setting> Settings { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
