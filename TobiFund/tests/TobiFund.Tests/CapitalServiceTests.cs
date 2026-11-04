using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های سرمایه و قوانین تراکنش مالی (بخش ۸، ۳۲-۳۳ سند)</summary>
public class CapitalServiceTests
{
    [Fact]
    public async Task Balance_IsSumOfTransactions_DepositMinusWithdrawal()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 500000m, TransactionType = TransactionType.Deposit });
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 100000m, TransactionType = TransactionType.Deposit });
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 50000m, TransactionType = TransactionType.Withdrawal });

        var balance = await ts.Capital.GetBalanceAsync(memberId);
        Assert.Equal(550000m, balance);
    }

    [Fact]
    public async Task AddTransaction_NegativeOrZeroAmount_Rejected()
    {
        // مبلغ منفی یا صفر ثبت نشود
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        Assert.False((await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = -100m, TransactionType = TransactionType.Deposit })).Success);
        Assert.False((await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 0m, TransactionType = TransactionType.Deposit })).Success);
    }

    [Fact]
    public async Task AddTransaction_InvalidMember_Rejected()
    {
        using var ts = new TestServices();
        var result = await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = 9999, Amount = 1000m, TransactionType = TransactionType.Deposit
        });
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Transactions_HistoryIsPreserved_NeverOnlyFinalValue()
    {
        // تمام پرداخت‌ها در جدول تراکنش‌ها ذخیره می‌شوند
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 100m, TransactionType = TransactionType.Deposit });
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 200m, TransactionType = TransactionType.Deposit });
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 300m, TransactionType = TransactionType.Deposit });

        var history = await ts.Capital.GetMemberTransactionsAsync(memberId);
        Assert.Equal(3, history.Count);
        Assert.Equal(600m, history.Sum(t => t.Amount));
    }
}
