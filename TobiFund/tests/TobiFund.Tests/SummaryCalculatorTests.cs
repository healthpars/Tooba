using TobiFund.Application.DTOs;
using TobiFund.Application.Services;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های محاسبات مالی (بخش ۱۴، ۱۷، ۲۲، ۴۰ سند)</summary>
public class SummaryCalculatorTests
{
    [Fact]
    public void ComputeInstallmentStatus_Rules()
    {
        var today = new DateTime(2026, 10, 8);
        // پرداخت کامل
        Assert.Equal(InstallmentStatus.Paid, SummaryCalculator.ComputeInstallmentStatus(10000m, 10000m, today.AddDays(-5), today));
        // پرداخت جزئی
        Assert.Equal(InstallmentStatus.PartiallyPaid, SummaryCalculator.ComputeInstallmentStatus(10000m, 4000m, today.AddDays(-5), today));
        // سررسید گذشته → معوق
        Assert.Equal(InstallmentStatus.Overdue, SummaryCalculator.ComputeInstallmentStatus(10000m, 0m, today.AddDays(-1), today));
        // سررسید امروز
        Assert.Equal(InstallmentStatus.Due, SummaryCalculator.ComputeInstallmentStatus(10000m, 0m, today, today));
        // آینده
        Assert.Equal(InstallmentStatus.Future, SummaryCalculator.ComputeInstallmentStatus(10000m, 0m, today.AddDays(10), today));
    }

    [Fact]
    public async Task GetOutstandingLoan_ReflectsPayments()
    {
        // مانده وام = مبلغ وام - پرداخت‌ها (‌نه مبلغ اولیه)
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 100000m, InstallmentCount = 10 });

        Assert.Equal(100000m, await ts.Loans.GetOutstandingLoanAsync(memberId));

        // پرداخت ۳ قسط کامل (۳۰هزار) → مانده ۷۰هزار
        var installments = (await ts.Loans.GetInstallmentsAsync(memberId: memberId))
            .OrderBy(i => i.InstallmentNumber).Take(3).ToList();
        foreach (var inst in installments)
        {
            var pay = await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
            {
                InstallmentId = inst.Id, Amount = inst.Amount
            });
            Assert.True(pay.Success);
        }

        Assert.Equal(70000m, await ts.Loans.GetOutstandingLoanAsync(memberId));
    }

    [Fact]
    public async Task HistoricalSnapshot_CapitalAndLoanAtDate()
    {
        // Snapshot تاریخی: وضعیت همان روز بازسازی می‌شود
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        var day1 = DateTime.Today.AddDays(-10);
        var day2 = DateTime.Today.AddDays(-5);

        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 100000m, TransactionDate = day1, TransactionType = TransactionType.Deposit });
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = memberId, Amount = 200000m, TransactionDate = day2, TransactionType = TransactionType.Deposit });
        await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 50000m, LoanDate = day2, InstallmentCount = 5 });

        // در روز ۱: سرمایه ۱۰۰هزار، vam noch 0
        Assert.Equal(100000m, await ts.Capital.GetBalanceAtAsync(memberId, day1));
        Assert.Equal(0m, await ts.Loans.GetOutstandingLoanAtAsync(memberId, day1));

        // در روز ۲: سرمایه ۳۰۰هزار، vam ۵۰هزار
        Assert.Equal(300000m, await ts.Capital.GetBalanceAtAsync(memberId, day2));
        Assert.Equal(50000m, await ts.Loans.GetOutstandingLoanAtAsync(memberId, day2));

        // امروز: سرمایه ۳۰۰هزار، vam ۵۰هزار (بدون پرداخت)
        Assert.Equal(300000m, await ts.Capital.GetBalanceAtAsync(memberId, DateTime.Today));
        Assert.Equal(50000m, await ts.Loans.GetOutstandingLoanAtAsync(memberId, DateTime.Today));
    }
}
