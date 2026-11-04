using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel داشبورد (آمار + اعضای برتر + هشدارها)</summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IMemberService _memberService;
    private readonly ILoanService _loanService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IScoreService _scoreService;
    private readonly INavigationService _navigation;

    public DashboardViewModel(
        IMemberService memberService,
        ILoanService loanService,
        ISubscriptionService subscriptionService,
        IScoreService scoreService,
        INavigationService navigation)
    {
        _memberService = memberService;
        _loanService = loanService;
        _subscriptionService = subscriptionService;
        _scoreService = scoreService;
        _navigation = navigation;
        Title = "داشبورد";
    }

    [ObservableProperty]
    private DashboardStatsDto _stats = new();

    [ObservableProperty]
    private ObservableCollection<RankingDto> _topMembers = new();

    [ObservableProperty]
    private RankingDto? _selectedTopMember;

    [ObservableProperty]
    private ObservableCollection<AlertDto> _alerts = new();

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var members = await _memberService.GetAllAsync();
            var loans = await _loanService.GetLoansAsync();
            var ranking = await _scoreService.GetRankingAsync();

            Stats = new DashboardStatsDto
            {
                TotalMembers = members.Count,
                ActiveMembers = members.Count(m => m.Status == MemberStatus.Active),
                TotalCapital = members.Sum(m => m.TotalCapital),
                TotalLoans = loans.Sum(l => l.Amount),
                TotalDueInstallments = members.Sum(m => m.DueInstallments),
                TotalDueSubscriptions = members.Sum(m => m.DueSubscription),
                TotalScore = ranking.Sum(r => (long)r.TotalScore)
            };

            TopMembers.Clear();
            foreach (var row in ranking.Take(10))
                TopMembers.Add(row);

            await BuildAlertsAsync(members);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildAlertsAsync(IReadOnlyList<MemberDto> members)
    {
        Alerts.Clear();
        var today = DateTime.Today;

        // ۱. اقساط معوق
        var overdueInstallments = await _loanService.GetInstallmentsByStatusAsync(InstallmentStatus.Overdue);
        foreach (var group in overdueInstallments.GroupBy(i => i.MemberId))
        {
            var first = group.First();
            Alerts.Add(new AlertDto
            {
                Type = AlertType.OverdueInstallment,
                Title = "قسط معوق",
                MemberId = first.MemberId,
                MemberCode = first.MemberCode,
                MemberName = first.MemberName,
                Amount = group.Sum(i => i.RemainingAmount),
                Message = $"اقساط معوق: {group.Count()} قسط به مبلغ {group.Sum(i => i.RemainingAmount):N0}"
            });
        }

        // ۲. آبونمان معوق
        var allSubscriptions = await _subscriptionService.GetSubscriptionsAsync();
        var overdueSubscriptions = allSubscriptions
            .Where(s => s.Status != PaymentStatus.Paid && s.DueDate.Date < today)
            .ToList();
        foreach (var group in overdueSubscriptions.GroupBy(s => s.MemberId))
        {
            var first = group.First();
            Alerts.Add(new AlertDto
            {
                Type = AlertType.OverdueSubscription,
                Title = "آبونمان معوق",
                MemberId = first.MemberId,
                MemberCode = first.MemberCode,
                MemberName = first.MemberName,
                Amount = group.Sum(s => s.RemainingAmount),
                Message = $"آبونمان معوق: {group.Count()} ماه به مبلغ {group.Sum(s => s.RemainingAmount):N0}"
            });
        }

        // ۳. اقساط نزدیک به سررسید (۷ روز آینده)
        var allInstallments = await _loanService.GetInstallmentsAsync();
        var upcoming = allInstallments
            .Where(i => i.RemainingAmount > 0 && i.DueDate.Date >= today && i.DueDate.Date <= today.AddDays(7))
            .ToList();
        foreach (var group in upcoming.GroupBy(i => i.MemberId))
        {
            var first = group.First();
            var earliest = group.Min(i => i.DueDate);
            Alerts.Add(new AlertDto
            {
                Type = AlertType.UpcomingInstallment,
                Title = "قسط نزدیک به سررسید",
                MemberId = first.MemberId,
                MemberCode = first.MemberCode,
                MemberName = first.MemberName,
                Amount = group.Sum(i => i.RemainingAmount),
                DueDate = earliest,
                DueDatePersian = PersianDateHelper.ToPersianDate(earliest),
                Message = $"قسط نزدیک به سررسید: {group.Count()} قسط تا ۷ روز آینده"
            });
        }

        // ۴. اعضای دارای بدهی
        foreach (var member in members.Where(m => m.DueInstallments + m.DueSubscription > 0))
        {
            Alerts.Add(new AlertDto
            {
                Type = AlertType.DebtorMember,
                Title = "عضو دارای بدهی",
                MemberId = member.Id,
                MemberCode = member.MemberCode,
                MemberName = member.FullName,
                Amount = member.DueInstallments + member.DueSubscription,
                Message = $"بدهی: اقساط {member.DueInstallments:N0} + آبونمان {member.DueSubscription:N0}"
            });
        }
    }

    [RelayCommand]
    private void OpenMember(RankingDto? ranking)
    {
        if (ranking is null) return;
        _navigation.NavigateTo<MemberDetailViewModel>(ranking.MemberId);
    }
}
