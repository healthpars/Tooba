using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface IScoreService
{
    /// <summary>
    /// ثبت Snapshot امتیاز روزانه برای اعضای فعال.
    /// برای روزهای گذشته فقط در صورت نبود رکورد ثبت می‌شود (Snapshot immutable). برای «امروز» رکورد موجود به‌روز می‌شود.
    /// </summary>
    Task<ServiceResult<int>> RecordDailyScoresAsync(DateTime? upToDate = null);
    Task<DailyScoreDto?> GetDailyScoreAsync(int memberId, DateTime date);
    Task<IReadOnlyList<DailyScoreDto>> GetMemberDailyScoresAsync(int memberId);
    Task<int> GetTotalScoreAsync(int memberId);
    Task<IReadOnlyList<RankingDto>> GetRankingAsync();

    /// <summary>محاسبه امتیاز یک روز: مثبت (سرمایه) - منفی (مانده وام)</summary>
    Task<(int Positive, int Negative, int Net)> CalculateScoreAsync(decimal capital, decimal outstandingLoan);
}
