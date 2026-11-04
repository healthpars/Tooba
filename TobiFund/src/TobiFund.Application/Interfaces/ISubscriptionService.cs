using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface ISubscriptionService
{
    /// <summary>ایجاد ۱۲ رکورد آبونمان ماهانه برای عضو در سال شمسی (اگر موجود نباشند). تعداد ایجادشده برگردانده می‌شود.</summary>
    Task<ServiceResult<int>> EnsureSubscriptionsAsync(int memberId, int persianYear);
    Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsAsync(int? memberId = null, int? persianYear = null);
    Task<ServiceResult<SubscriptionPaymentDto>> PaySubscriptionAsync(CreateSubscriptionPaymentDto dto);
    Task<decimal> GetDueSubscriptionAsync(int memberId);
    Task<IReadOnlyList<SubscriptionPaymentDto>> GetPaymentsAsync(int subscriptionId);
}
