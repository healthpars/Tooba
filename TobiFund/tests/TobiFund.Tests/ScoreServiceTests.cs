using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های منطق امتیاز (بخش ۱۹-۲۳ سند)</summary>
public class ScoreServiceTests
{
    [Fact]
    public async Task CalculateScore_Capital500k_Loan200k_Net300()
    {
        // سرمایه = 500000، وام = 200000 → مثبت 500 - منفی 200 = 300
        using var ts = new TestServices();
        var (positive, negative, net) = await ts.Scores.CalculateScoreAsync(500000m, 200000m);
        Assert.Equal(500, positive);
        Assert.Equal(200, negative);
        Assert.Equal(300, net);
    }

    [Fact]
    public async Task DailyScore_UsesOutstandingLoan_NotOriginalLoanAmount()
    {
        // وام اولیه 100,000 — بازپرداخت 30,000 — مانده 70,000 → منفی = 70
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = memberId, Amount = 500000m, TransactionType = TransactionType.Deposit
        });
        var loan = await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId, Amount = 100000m, InstallmentCount = 10
        });
        Assert.True(loan.Success);

        // پرداخت ۳ قسط ۱۰هزاری = ۳۰هزار
        var installments = await ts.Loans.GetInstallmentsAsync(memberId: memberId);
        foreach (var inst in installments.Take(3))
        {
            var pay = await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
            {
                InstallmentId = inst.Id, Amount = inst.Amount
            });
            Assert.True(pay.Success);
        }

        var result = await ts.Scores.RecordDailyScoresAsync();
        Assert.True(result.Success);

        var today = await ts.Scores.GetDailyScoreAsync(memberId, DateTime.Today);
        Assert.NotNull(today);
        Assert.Equal(500000m, today!.CapitalAmount);
        Assert.Equal(70000m, today.LoanAmount); // مانده وام = ۷۰هزار
        Assert.Equal(500, today.PositiveScore);
        Assert.Equal(70, today.NegativeScore);
        Assert.Equal(430, today.NetScore);
    }

    [Fact]
    public async Task DailyScore_PaidOffLoan_NoNegativeScore()
    {
        // وام کاملاً تسویه → مانده = 0 → امتیاز منفی = 0
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = memberId, Amount = 100000m, TransactionType = TransactionType.Deposit
        });
        var loan = await ts.Loans.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = memberId, Amount = 50000m, InstallmentCount = 5
        });

        var installments = await ts.Loans.GetInstallmentsAsync(memberId: memberId);
        foreach (var inst in installments)
        {
            var pay = await ts.Loans.PayInstallmentAsync(new CreateInstallmentPaymentDto
            {
                InstallmentId = inst.Id, Amount = inst.RemainingAmount
            });
            Assert.True(pay.Success);
        }

        await ts.Scores.RecordDailyScoresAsync();
        var today = await ts.Scores.GetDailyScoreAsync(memberId, DateTime.Today);
        Assert.NotNull(today);
        Assert.Equal(0m, today!.LoanAmount);
        Assert.Equal(0, today.NegativeScore);
        Assert.Equal(100, today.NetScore); // 100000 / 1000
    }

    [Fact]
    public async Task RecordDailyScores_SameDayTwice_NoDuplicate()
    {
        // باز شدن چند bewerben برنامه در یک روز → رکورد تکراری ایجاد نشود
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = memberId, Amount = 10000m, TransactionType = TransactionType.Deposit
        });

        var first = await ts.Scores.RecordDailyScoresAsync();
        var second = await ts.Scores.RecordDailyScoresAsync();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(0, second.Data); // اجرای دوم در همان روز، رکورد جدیدی اضافه نمی‌کند
        var scores = await ts.Scores.GetMemberDailyScoresAsync(memberId);
        Assert.Single(scores);
    }

    [Fact]
    public async Task RecordDailyScores_CatchUpForMissedDays_UsesHistoricalSnapshot()
    {
        // عضو با سرمایه در ۳ روز پیش ثبت شود؛ Snapshot باید وضعیت همان روز را نشان دهد
        using var ts = new TestServices();
        var threeDaysAgo = DateTime.Today.AddDays(-3);
        var memberId = await ts.CreateMemberAsync(joinDate: threeDaysAgo);
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = memberId, Amount = 250000m, TransactionDate = threeDaysAgo, TransactionType = TransactionType.Deposit
        });

        var result = await ts.Scores.RecordDailyScoresAsync();
        Assert.True(result.Success);
        Assert.True(result.Data >= 3); // mindestens ۳ روز (۳ روز پیش + ۲ روز پیش + دیروز + امروز)

        var scores = await ts.Scores.GetMemberDailyScoresAsync(memberId);
        Assert.True(scores.Count >= 3);

        // Snapshot دیروز: سرمایه ۲۵۰هزار → ۲۵۰ امتیاز
        var yesterday = scores.First(s => s.ScoreDate.Date == DateTime.Today.AddDays(-1).Date);
        Assert.Equal(250000m, yesterday.CapitalAmount);
        Assert.Equal(250, yesterday.NetScore);

        // حالا سرمایه امروز ۴۰۰هزار شود؛ Snapshot دیروز نباید تغییر کند
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = memberId, Amount = 150000m, TransactionDate = DateTime.Today, TransactionType = TransactionType.Deposit
        });
        await ts.Scores.RecordDailyScoresAsync();

        var scoresAfter = await ts.Scores.GetMemberDailyScoresAsync(memberId);
        var yesterdayAfter = scoresAfter.First(s => s.ScoreDate.Date == DateTime.Today.AddDays(-1).Date);
        Assert.Equal(250000m, yesterdayAfter.CapitalAmount); // pasado تغییر نکرده
        Assert.Equal(250, yesterdayAfter.NetScore);

        var todayScore = scoresAfter.First(s => s.ScoreDate.Date == DateTime.Today.Date);
        Assert.Equal(400000m, todayScore.CapitalAmount); // امروز به‌روز شده
        Assert.Equal(400, todayScore.NetScore);
    }

    [Fact]
    public async Task TotalScore_IsSumOfNetScores_AndRankingWorks()
    {
        using var ts = new TestServices();
        var m1 = await ts.CreateMemberAsync("1001", "علی", "رضایی");
        var m2 = await ts.CreateMemberAsync("1002", "رضا", "محمدی");

        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = m1, Amount = 500000m, TransactionType = TransactionType.Deposit });
        await ts.Capital.AddTransactionAsync(new CreateCapitalTransactionDto { MemberId = m2, Amount = 200000m, TransactionType = TransactionType.Deposit });

        await ts.Scores.RecordDailyScoresAsync();

        Assert.Equal(500, await ts.Scores.GetTotalScoreAsync(m1));
        Assert.Equal(200, await ts.Scores.GetTotalScoreAsync(m2));

        var ranking = await ts.Scores.GetRankingAsync();
        Assert.Equal(2, ranking.Count);
        Assert.Equal(m1, ranking[0].MemberId);
        Assert.Equal(1, ranking[0].Rank);
        Assert.Equal(m2, ranking[1].MemberId);
        Assert.Equal(2, ranking[1].Rank);
    }
}
