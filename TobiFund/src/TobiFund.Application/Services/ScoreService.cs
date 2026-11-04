using Microsoft.EntityFrameworkCore;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Entities;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Services;

/// <summary>
/// موتور امتیازدهی:
/// DailyScore = Floor(سرمایه / واحد سرمایه) * امتیاز سرمایه
///            - Floor(مانده وام / واحد وام) * امتیاز منفی وام
/// امتیاز به‌صورت Snapshot روزانه ذخیره می‌شود تا تغییر موجودی‌های آینده
/// امتیاز روزهای گذشته را تغییر ندهد.
/// </summary>
public class ScoreService : IScoreService
{
    /// <summary>حداکثر تعداد روزهای گذشته که در یک اجرا جبران (Catch-up) می‌شوند</summary>
    private const int MaxCatchUpDays = 1095; // حدود ۳ سال

    private readonly IApplicationDbContext _context;
    private readonly ISettingsService _settings;
    private readonly IAppLogger _logger;

    public ScoreService(IApplicationDbContext context, ISettingsService settings, IAppLogger logger)
    {
        _context = context;
        _settings = settings;
        _logger = logger;
    }

    public async Task<(int Positive, int Negative, int Net)> CalculateScoreAsync(decimal capital, decimal outstandingLoan)
    {
        var capitalUnit = await _settings.GetDecimalAsync(SettingKeys.CapitalScoreUnit, 1000m);
        var capitalValue = await _settings.GetIntAsync(SettingKeys.CapitalScoreValue, 1);
        var loanUnit = await _settings.GetDecimalAsync(SettingKeys.LoanScoreUnit, 1000m);
        var loanValue = await _settings.GetIntAsync(SettingKeys.LoanScoreValue, 1);

        if (capitalUnit <= 0) capitalUnit = 1000m;
        if (loanUnit <= 0) loanUnit = 1000m;

        var positive = (int)Math.Floor(capital / capitalUnit) * capitalValue;
        var negative = (int)Math.Floor(outstandingLoan / loanUnit) * loanValue;
        return (positive, negative, positive - negative);
    }

    public async Task<ServiceResult<int>> RecordDailyScoresAsync(DateTime? upToDate = null)
    {
        var today = (upToDate ?? DateTime.Today).Date;
        var members = await _context.Members
            .Where(m => !m.IsDeleted && m.Status == MemberStatus.Active)
            .ToListAsync();

        var created = 0;
        await using var transaction = await _context.BeginTransactionAsync();
        try
        {
            foreach (var member in members)
            {
                var lastScoreDate = await _context.DailyScores
                    .Where(s => s.MemberId == member.Id)
                    .OrderByDescending(s => s.ScoreDate)
                    .Select(s => (DateTime?)s.ScoreDate)
                    .FirstOrDefaultAsync();

                if (member.JoinDate.Date > today) continue;

                var start = lastScoreDate.HasValue
                    ? lastScoreDate.Value.Date.AddDays(1)
                    : member.JoinDate.Date;

                // Snapshot «امروز» همیشه به‌روز می‌شود (upsert)،
                // حتی اگر رکورد امروز از قبل موجود باشد
                if (start > today)
                    start = today;

                // محدود کردن catch-up
                if ((today - start).TotalDays > MaxCatchUpDays)
                {
                    _logger.Warning($"Catch-up امتیاز برای عضو {member.MemberCode} به {MaxCatchUpDays} روز محدود شد.");
                    start = today.AddDays(-(MaxCatchUpDays - 1));
                }

                for (var date = start; date <= today; date = date.AddDays(1))
                {
                    var nextDay = date.AddDays(1);
                    var existing = await _context.DailyScores
                        .FirstOrDefaultAsync(s => s.MemberId == member.Id && s.ScoreDate >= date && s.ScoreDate < nextDay);

                    var capital = await SummaryCalculator.GetCapitalAtAsync(_context, member.Id, date);
                    var loan = await SummaryCalculator.GetOutstandingLoanAtAsync(_context, member.Id, date);
                    var (positive, negative, net) = await CalculateScoreAsync(capital, loan);

                    if (existing is null)
                    {
                        _context.DailyScores.Add(new DailyScore
                        {
                            MemberId = member.Id,
                            ScoreDate = date,
                            CapitalAmount = capital,
                            LoanAmount = loan,
                            PositiveScore = positive,
                            NegativeScore = negative,
                            NetScore = net,
                            CreatedAt = DateTime.Now
                        });
                        created++;
                    }
                    else if (date == today)
                    {
                        // فقط Snapshot «امروز» قابل به‌روزرسانی است؛ روزهای گذشته هرگز تغییر نمی‌کنند.
                        existing.CapitalAmount = capital;
                        existing.LoanAmount = loan;
                        existing.PositiveScore = positive;
                        existing.NegativeScore = negative;
                        existing.NetScore = net;
                    }
                    // روزهای گذشته: اگر رکورد موجود باشد، دست‌نخورده می‌ماند (Snapshot immutability)
                }
            }

            if (created > 0 || members.Count > 0)
                await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return ServiceResult<int>.Ok(created, $"{created} Snapshot امتیاز روزانه ثبت شد.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error("خطا در ثبت امتیاز روزانه", ex);
            return ServiceResult<int>.Fail("خطا در ثبت امتیاز روزانه.");
        }
    }

    public async Task<DailyScoreDto?> GetDailyScoreAsync(int memberId, DateTime date)
    {
        var nextDay = date.Date.AddDays(1);
        var score = await _context.DailyScores
            .FirstOrDefaultAsync(s => s.MemberId == memberId && s.ScoreDate >= date.Date && s.ScoreDate < nextDay);
        return score is null ? null : MapToDto(score);
    }

    public async Task<IReadOnlyList<DailyScoreDto>> GetMemberDailyScoresAsync(int memberId)
    {
        var scores = await _context.DailyScores
            .Where(s => s.MemberId == memberId)
            .OrderBy(s => s.ScoreDate)
            .ToListAsync();
        return scores.Select(MapToDto).ToList();
    }

    public Task<int> GetTotalScoreAsync(int memberId) =>
        SummaryCalculator.GetTotalScoreAsync(_context, memberId);

    public async Task<IReadOnlyList<RankingDto>> GetRankingAsync()
    {
        var members = await _context.Members
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.MemberCode)
            .ToListAsync();

        var rows = new List<RankingDto>();
        foreach (var member in members)
        {
            rows.Add(new RankingDto
            {
                MemberId = member.Id,
                MemberCode = member.MemberCode,
                FullName = member.FullName,
                TotalCapital = await SummaryCalculator.GetCapitalAsync(_context, member.Id),
                OutstandingLoan = await SummaryCalculator.GetOutstandingLoanAsync(_context, member.Id),
                DueInstallments = await SummaryCalculator.GetDueInstallmentsAsync(_context, member.Id),
                DueSubscription = await SummaryCalculator.GetDueSubscriptionAsync(_context, member.Id),
                TotalScore = await SummaryCalculator.GetTotalScoreAsync(_context, member.Id)
            });
        }

        return rows
            .OrderByDescending(r => r.TotalScore)
            .ThenByDescending(r => r.TotalCapital)
            .Select((r, index) =>
            {
                r.Rank = index + 1;
                return r;
            })
            .ToList();
    }

    private static DailyScoreDto MapToDto(DailyScore s) => new()
    {
        Id = s.Id,
        MemberId = s.MemberId,
        ScoreDate = s.ScoreDate,
        ScoreDatePersian = PersianDateHelper.ToPersianDate(s.ScoreDate),
        CapitalAmount = s.CapitalAmount,
        LoanAmount = s.LoanAmount,
        PositiveScore = s.PositiveScore,
        NegativeScore = s.NegativeScore,
        NetScore = s.NetScore
    };
}
