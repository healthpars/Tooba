using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه آبونمان (وضعیت + تولید ۱۲ ماه + پرداخت)</summary>
public partial class SubscriptionsViewModel : ViewModelBase
{
    private readonly IMemberService _memberService;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsViewModel(IMemberService memberService, ISubscriptionService subscriptionService)
    {
        _memberService = memberService;
        _subscriptionService = subscriptionService;
        Title = "آبونمان";
    }

    [ObservableProperty]
    private ObservableCollection<MemberDto> _members = new();

    [ObservableProperty]
    private MemberDto? _selectedMember;

    [ObservableProperty]
    private ObservableCollection<SubscriptionDto> _subscriptions = new();

    [ObservableProperty]
    private SubscriptionDto? _selectedSubscription;

    [ObservableProperty]
    private string _yearText = PersianDateHelper.CurrentPersianYear.ToString();

    [ObservableProperty]
    private string _payAmountText = string.Empty;

    [ObservableProperty]
    private DateTime? _payDate = DateTime.Today;

    [ObservableProperty]
    private string _payDescription = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var members = await _memberService.GetAllAsync();
        Members.Clear();
        foreach (var member in members)
            Members.Add(member);
        await LoadSubscriptionsAsync();
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadSubscriptionsAsync();

    private async Task LoadSubscriptionsAsync()
    {
        var list = await _subscriptionService.GetSubscriptionsAsync(memberId: SelectedMember?.Id);
        Subscriptions.Clear();
        foreach (var s in list)
            Subscriptions.Add(s);
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        Message = string.Empty;
        if (SelectedMember is null)
        {
            ShowResult("ابتدا عضو را انتخاب کنید.", false);
            return;
        }
        if (!int.TryParse(PersianDateHelper.ToEnglishDigits(YearText.Trim()), out var year))
        {
            ShowResult("سال شمسی نامعتبر است.", false);
            return;
        }

        var result = await _subscriptionService.EnsureSubscriptionsAsync(SelectedMember.Id, year);
        ShowResult(result.Message, result.Success);
        await LoadSubscriptionsAsync();
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        Message = string.Empty;
        if (SelectedSubscription is null)
        {
            ShowResult("ابتدا یک آبونمان را انتخاب کنید.", false);
            return;
        }
        var date = PayDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(PayAmountText, out var amount))
        {
            ShowResult("مبلغ پرداختی نامعتبر است.", false);
            return;
        }

        var result = await _subscriptionService.PaySubscriptionAsync(new CreateSubscriptionPaymentDto
        {
            SubscriptionId = SelectedSubscription.Id,
            PaymentDate = date,
            Amount = amount,
            Description = PayDescription
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            PayAmountText = string.Empty;
            PayDescription = string.Empty;
            await LoadSubscriptionsAsync();
        }
    }

    private void ShowResult(string message, bool success)
    {
        Message = message;
        IsError = !success;
    }
}
