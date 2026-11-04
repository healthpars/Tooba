using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های منطق وام و اقساط (بخش ۱۰-۱۴ سند)</summary>
public class LoanServiceTests
{
    [Fact]
    public async Task CreateLoan_100000_10Installments_Each10000()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        var result = await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId,
            Amount = 100000m,
            LoanDate = DateTime.Today,
            InstallmentCount = 10
        });

        Assert.True(result.Success);
        Assert.Equal(10000m, result.Data!.InstallmentAmount);

        var installments = await ts.Loans.GetInstallmentsAsync(memberId: memberId);
        Assert.Equal(10, installments.Count);
        Assert.All(installments, i => Assert.Equal(10000m, i.Amount));
        Assert.Equal(Enumerable.Range(1, 10), installments.Select(i => i.InstallmentNumber));
    }

    [Fact]
    public async Task CreateLoan_FirstDueDate_IsLoanDatePlus30Days()
    {
        // 1405/01/10 + 30 روز = 1405/02/09
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        var loanDate = PersianDateHelper.PersianToGregorian(1405, 1, 10);

        var result = await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId,
            Amount = 100000m,
            LoanDate = loanDate,
            InstallmentCount = 10
        });

        Assert.True(result.Success);
        var installments = await ts.Loans.GetInstallmentsAsync(memberId: memberId);
        var first = installments.OrderBy(i => i.InstallmentNumber).First();
        Assert.Equal(PersianDateHelper.PersianToGregorian(1405, 2, 9), first.DueDate);
    }

    [Fact]
    public async Task CreateLoan_InstallmentsAreOnePersianMonthApart()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        var loanDate = PersianDateHelper.PersianToGregorian(1405, 1, 10);

        await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId, Amount = 120000m, LoanDate = loanDate, InstallmentCount = 12
        });

        var installments = (await ts.Loans.GetInstallmentsAsync(memberId: memberId))
            .OrderBy(i => i.InstallmentNumber).ToList();

        var expected = PersianDateHelper.PersianToGregorian(1405, 2, 9);
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(expected, installments[i].DueDate);
            expected = PersianDateHelper.AddPersianMonths(expected, 1);
        }
    }

    [Fact]
    public async Task CreateLoan_RemainderGoesToLastInstallment()
    {
        // 100,000 / 3 = 33,333.33 → قسط پایه ۳۳۳۳۳ و قسط آخر ۳۳۳۳۴
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        var result = await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId, Amount = 100000m, InstallmentCount = 3
        });

        Assert.True(result.Success);
        var installments = (await ts.Loans.GetInstallmentsAsync(memberId: memberId))
            .OrderBy(i => i.InstallmentNumber).ToList();
        Assert.Equal(33333m, installments[0].Amount);
        Assert.Equal(33333m, installments[1].Amount);
        Assert.Equal(33334m, installments[2].Amount);
        Assert.Equal(100000m, installments.Sum(i => i.Amount));
    }

    [Fact]
    public async Task CreateLoan_Validations()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        var zeroAmount = await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 0, InstallmentCount = 10 });
        Assert.False(zeroAmount.Success);

        var negativeAmount = await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = -5, InstallmentCount = 10 });
        Assert.False(negativeAmount.Success);

        var zeroCount = await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 1000, InstallmentCount = 0 });
        Assert.False(zeroCount.Success);

        var badMember = await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = 9999, Amount = 1000, InstallmentCount = 10 });
        Assert.False(badMember.Success);
    }

    [Fact]
    public async Task PayInstallment_PartialPayment_SetsPartiallyPaid()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 100000m, InstallmentCount = 10 });

        var installment = (await ts.Loans.GetInstallmentsAsync(memberId: memberId)).First();
        var result = await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
        {
            InstallmentId = installment.Id, Amount = 4000m
        });

        Assert.True(result.Success);
        var updated = (await ts.Loans.GetInstallmentsAsync(memberId: memberId)).First(i => i.Id == installment.Id);
        Assert.Equal(4000m, updated.PaidAmount);
        Assert.Equal(6000m, updated.RemainingAmount);
        Assert.Equal(InstallmentStatus.PartiallyPaid, updated.Status);
    }

    [Fact]
    public async Task PayInstallment_FullPayment_SetsPaid()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 100000m, InstallmentCount = 10 });

        var installment = (await ts.Loans.GetInstallmentsAsync(memberId: memberId)).First();
        var result = await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
        {
            InstallmentId = installment.Id, Amount = installment.Amount
        });

        Assert.True(result.Success);
        var updated = (await ts.Loans.GetInstallmentsAsync(memberId: memberId)).First(i => i.Id == installment.Id);
        Assert.Equal(InstallmentStatus.Paid, updated.Status);
        Assert.Equal(0m, updated.RemainingAmount);
    }

    [Fact]
    public async Task PayInstallment_Overpayment_Rejected()
    {
        // قسط بیش از مبلغ بدهی پرداخت شود → خطا
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 100000m, InstallmentCount = 10 });

        var installment = (await ts.Loans.GetInstallmentsAsync(memberId: memberId)).First();
        var result = await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
        {
            InstallmentId = installment.Id, Amount = installment.Amount + 1
        });
        Assert.False(result.Success);
    }

    [Fact]
    public async Task PayInstallment_NegativeOrZeroAmount_Rejected()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Loans.CreateLoanAsync(new CreateLoanDto { MemberId = memberId, Amount = 100000m, InstallmentCount = 10 });
        var installment = (await ts.Loans.GetInstallmentsAsync(memberId: memberId)).First();

        Assert.False((await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto { InstallmentId = installment.Id, Amount = 0 })).Success);
        Assert.False((await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto { InstallmentId = installment.Id, Amount = -100 })).Success);
    }

    [Fact]
    public async Task DueInstallments_SumsOnlyPastDueUnpaid()
    {
        // DueInstallments = SUM(Amount - PaidAmount) برای DueDate <= امروز و مانده > 0
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        var loanDate = DateTime.Today.AddDays(-90); // وام ۹۰ روز پیش → چند قسط سررسید گذشته

        await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId, Amount = 100000m, LoanDate = loanDate, InstallmentCount = 10
        });
        await ts.Loans.RefreshStatusesAsync();

        var due = await ts.Loans.GetDueInstallmentsAsync(memberId);
        Assert.True(due > 0);

        // پرداخت کامل اولین قسط سررسیدشده → از بدهی کم می‌شود
        var overdue = (await ts.Loans.GetInstallmentsByStatusAsync(InstallmentStatus.Overdue)).First();
        await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
        {
            InstallmentId = overdue.Id, Amount = overdue.RemainingAmount
        });

        var dueAfter = await ts.Loans.GetDueInstallmentsAsync(memberId);
        Assert.Equal(due - overdue.Amount, dueAfter);
    }

    [Fact]
    public async Task RefreshStatuses_PastDueUnpaidInstallment_BecomesOverdue()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        var loanDate = DateTime.Today.AddDays(-40);

        await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId, Amount = 50000m, LoanDate = loanDate, InstallmentCount = 5
        });
        await ts.Loans.RefreshStatusesAsync();

        var overdue = await ts.Loans.GetInstallmentsByStatusAsync(InstallmentStatus.Overdue);
        Assert.NotEmpty(overdue);
        Assert.All(overdue, i => Assert.True(i.DueDate.Date < DateTime.Today));
    }
}
