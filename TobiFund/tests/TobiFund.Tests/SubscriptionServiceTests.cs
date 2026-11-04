using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های آبونمان (بخش ۱۵-۱۸ سند)</summary>
public class SubscriptionServiceTests
{
    [Fact]
    public async Task EnsureSubscriptions_Creates12PerYear()
    {
        // هر عضو در هر سال ۱۲ آبونمان ماهانه دارد
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        var result = await ts.Subscriptions.EnsureSubscriptionsAsync(memberId, 1405);
        Assert.True(result.Success);
        Assert.Equal(12, result.Data);

        var list = await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId);
        Assert.Equal(12, list.Count);
        Assert.Equal(Enumerable.Range(1, 12), list.Select(s => s.PersianMonth).OrderBy(m => m));
        Assert.All(list, s => Assert.Equal(150000m, s.Amount)); // مبلغ پیش‌فرض
    }

    [Fact]
    public async Task EnsureSubscriptions_CalledTwice_DoesNotDuplicate()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();

        await ts.Subscriptions.EnsureSubscriptionsAsync(memberId, 1405);
        var second = await ts.Subscriptions.EnsureSubscriptionsAsync(memberId, 1405);

        Assert.True(second.Success);
        Assert.Equal(0, second.Data); // رکورد تکراری ایجاد نشده
        var list = await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId);
        Assert.Equal(12, list.Count);
    }

    [Fact]
    public async Task PaySubscription_PartialAndFull()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Subscriptions.EnsureSubscriptionsAsync(memberId, 1405);
        var sub = (await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId)).First();

        var partial = await ts.Subscriptions.PaySubscriptionAsync(new CreateSubscriptionPaymentDto
        {
            SubscriptionId = sub.Id, Amount = 50000m
        });
        Assert.True(partial.Success);

        var list = await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId);
        var updated = list.First(s => s.Id == sub.Id);
        Assert.Equal(PaymentStatus.PartiallyPaid, updated.Status);
        Assert.Equal(100000m, updated.RemainingAmount);

        var full = await ts.Subscriptions.PaySubscriptionAsync(new CreateSubscriptionPaymentDto
        {
            SubscriptionId = sub.Id, Amount = 100000m
        });
        Assert.True(full.Success);
        list = await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId);
        updated = list.First(s => s.Id == sub.Id);
        Assert.Equal(PaymentStatus.Paid, updated.Status);
        Assert.Equal(0m, updated.RemainingAmount);
    }

    [Fact]
    public async Task PaySubscription_Overpayment_Rejected()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        await ts.Subscriptions.EnsureSubscriptionsAsync(memberId, 1405);
        var sub = (await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId)).First();

        var result = await ts.Subscriptions.PaySubscriptionAsync(new CreateSubscriptionPaymentDto
        {
            SubscriptionId = sub.Id, Amount = sub.Amount + 1
        });
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DueSubscription_SumsOnlyPastDueUnpaid()
    {
        using var ts = new TestServices();
        var memberId = await ts.CreateMemberAsync();
        var currentYear = PersianDateHelper.CurrentPersianYear;

        await ts.Subscriptions.EnsureSubscriptionsAsync(memberId, currentYear);
        await ts.Loans.RefreshStatusesAsync();

        var due = await ts.Subscriptions.GetDueSubscriptionAsync(memberId);
        // آبونمان‌های ماه‌های گذشته سال جاری (تا امروز) باید معوق باشند
        Assert.True(due > 0);

        var all = await ts.Subscriptions.GetSubscriptionsAsync(memberId: memberId);
        var expected = all
            .Where(s => s.DueDate.Date <= DateTime.Today && s.RemainingAmount > 0)
            .Sum(s => s.RemainingAmount);
        Assert.Equal(expected, due);
    }
}
