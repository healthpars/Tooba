using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه جزئیات عضو (۷ تب: اطلاعات، سرمایه، وام‌ها، اقساط، آبونمان، امتیازات، گردش مالی)</summary>
public partial class MemberDetailViewModel : ViewModelBase, INavigationAware
{
    private readonly IMemberService _memberService;
    private readonly ICapitalService _capitalService;
    private readonly ILoanService _loanService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IScoreService _scoreService;
    private readonly ICurrentUserService _currentUser;
    private readonly INavigationService _navigation;

    private int _memberId;

    public MemberDetailViewModel(
        IMemberService memberService,
        ICapitalService capitalService,
        ILoanService loanService,
        ISubscriptionService subscriptionService,
        IScoreService scoreService,
        ICurrentUserService currentUser,
        INavigationService navigation)
    {
        _memberService = memberService;
        _capitalService = capitalService;
        _loanService = loanService;
        _subscriptionService = subscriptionService;
        _scoreService = scoreService;
        _currentUser = currentUser;
        _navigation = navigation;
    }

    [ObservableProperty]
    private MemberDto? _member;

    [ObservableProperty]
    private MemberSummaryDto? _summary;

    [ObservableProperty]
    private ObservableCollection<CapitalTransactionDto> _capitalTransactions = new();

    [ObservableProperty]
    private ObservableCollection<LoanDto> _loans = new();

    [ObservableProperty]
    private ObservableCollection<LoanInstallmentDto> _installments = new();

    [ObservableProperty]
    private ObservableCollection<SubscriptionDto> _subscriptions = new();

    [ObservableProperty]
    private ObservableCollection<DailyScoreDto> _dailyScores = new();

    [ObservableProperty]
    private LoanInstallmentDto? _selectedInstallment;

    [ObservableProperty]
    private SubscriptionDto? _selectedSubscription;

    // ---------- فرم ثبت سرمایه ----------
    [ObservableProperty]
    private string _capitalAmountText = string.Empty;

    [ObservableProperty]
    private DateTime? _capitalDate = DateTime.Today;

    /// <summary>0 = واریز، 1 = برداشت، 2 = تعدیل</summary>
    [ObservableProperty]
    private int _capitalTypeIndex;

    [ObservableProperty]
    private string _capitalDescription = string.Empty;

    // ---------- فرم ثبت وام ----------
    [ObservableProperty]
    private string _loanAmountText = string.Empty;

    [ObservableProperty]
    private DateTime? _loanDate = DateTime.Today;

    [ObservableProperty]
    private string _loanInstallmentCountText = "10";

    [ObservableProperty]
    private string _loanDescription = string.Empty;

    /// <summary>آیا کاربر مدیر است (امکان تغییر تعداد اقساط)</summary>
    [ObservableProperty]
    private bool _isInstallmentCountEditable;

    // ---------- فرم پرداخت قسط ----------
    [ObservableProperty]
    private string _installmentPayAmountText = string.Empty;

    [ObservableProperty]
    private DateTime? _installmentPayDate = DateTime.Today;

    [ObservableProperty]
    private string _installmentPayDescription = string.Empty;

    // ---------- فرم پرداخت آبونمان ----------
    [ObservableProperty]
    private string _subscriptionPayAmountText = string.Empty;

    [ObservableProperty]
    private DateTime? _subscriptionPayDate = DateTime.Today;

    [ObservableProperty]
    private string _subscriptionPayDescription = string.Empty;

    // ---------- تولید آبونمان ----------
    [ObservableProperty]
    private string _subscriptionYearText = PersianDateHelper.CurrentPersianYear.ToString();

    // ---------- پیام ----------
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is int id)
        {
            _memberId = id;
            IsInstallmentCountEditable = _currentUser.IsAdmin;
            _ = LoadAllAsync();
        }
    }

    [RelayCommand]
    private async Task LoadAllAsync()
    {
        Member = await _memberService.GetByIdAsync(_memberId);
        if (Member is null)
        {
            ShowResult("عضو یافت نشد.", false);
            return;
        }

        Title = $"جزئیات عضو — {Member.FullName}";
        Summary = await _memberService.GetSummaryAsync(_memberId);

        CapitalTransactions.Clear();
        foreach (var t in await _capitalService.GetMemberTransactionsAsync(_memberId))
            CapitalTransactions.Add(t);

        Loans.Clear();
        foreach (var l in await _loanService.GetLoansAsync(_memberId))
            Loans.Add(l);

        Installments.Clear();
        foreach (var i in await _loanService.GetInstallmentsAsync(memberId: _memberId))
            Installments.Add(i);

        Subscriptions.Clear();
        foreach (var s in await _subscriptionService.GetSubscriptionsAsync(memberId: _memberId))
            Subscriptions.Add(s);

        DailyScores.Clear();
        foreach (var s in await _scoreService.GetMemberDailyScoresAsync(_memberId))
            DailyScores.Add(s);
    }

    [RelayCommand]
    private async Task AddCapitalAsync()
    {
        Message = string.Empty;
        var date = CapitalDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(CapitalAmountText, out var amount))
        {
            ShowResult("مبلغ نامعتبر است.", false);
            return;
        }

        var type = CapitalTypeIndex switch
        {
            1 => TransactionType.Withdrawal,
            2 => TransactionType.Adjustment,
            _ => TransactionType.Deposit
        };

        var result = await _capitalService.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = _memberId,
            TransactionDate = date,
            Amount = amount,
            TransactionType = type,
            Description = CapitalDescription
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            CapitalAmountText = string.Empty;
            CapitalDescription = string.Empty;
            await RefreshAfterFinancialChangeAsync();
        }
    }

    [RelayCommand]
    private async Task CreateLoanAsync()
    {
        Message = string.Empty;
        var date = LoanDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(LoanAmountText, out var amount))
        {
            ShowResult("مبلغ وام نامعتبر است.", false);
            return;
        }
        if (!int.TryParse(PersianDateHelper.ToEnglishDigits(LoanInstallmentCountText.Trim()),
                out var count) || count <= 0)
        {
            ShowResult("تعداد اقساط نامعتبر است.", false);
            return;
        }

        var result = await _loanService.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = _memberId,
            Amount = amount,
            LoanDate = date,
            InstallmentCount = count,
            Description = LoanDescription
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            LoanAmountText = string.Empty;
            LoanDescription = string.Empty;
            await RefreshAfterFinancialChangeAsync();
        }
    }

    [RelayCommand]
    private async Task PayInstallmentAsync()
    {
        Message = string.Empty;
        if (SelectedInstallment is null)
        {
            ShowResult("ابتدا یک قسط را از جدول انتخاب کنید.", false);
            return;
        }
        var date = InstallmentPayDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(InstallmentPayAmountText, out var amount))
        {
            ShowResult("مبلغ پرداختی نامعتبر است.", false);
            return;
        }

        var result = await _loanService.PayInstallmentAsync(new CreateInstallmentPaymentDto
        {
            InstallmentId = SelectedInstallment.Id,
            PaymentDate = date,
            Amount = amount,
            Description = InstallmentPayDescription
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            InstallmentPayAmountText = string.Empty;
            InstallmentPayDescription = string.Empty;
            await RefreshAfterFinancialChangeAsync();
        }
    }

    [RelayCommand]
    private async Task PaySubscriptionAsync()
    {
        Message = string.Empty;
        if (SelectedSubscription is null)
        {
            ShowResult("ابتدا یک آبونمان را از جدول انتخاب کنید.", false);
            return;
        }
        var date = SubscriptionPayDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(SubscriptionPayAmountText, out var amount))
        {
            ShowResult("مبلغ پرداختی نامعتبر است.", false);
            return;
        }

        var result = await _subscriptionService.PaySubscriptionAsync(new CreateSubscriptionPaymentDto
        {
            SubscriptionId = SelectedSubscription.Id,
            PaymentDate = date,
            Amount = amount,
            Description = SubscriptionPayDescription
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            SubscriptionPayAmountText = string.Empty;
            SubscriptionPayDescription = string.Empty;
            await LoadAllAsync();
        }
    }

    [RelayCommand]
    private async Task GenerateSubscriptionsAsync()
    {
        Message = string.Empty;
        if (!int.TryParse(PersianDateHelper.ToEnglishDigits(SubscriptionYearText.Trim()), out var year))
        {
            ShowResult("سال شمسی نامعتبر است.", false);
            return;
        }

        var result = await _subscriptionService.EnsureSubscriptionsAsync(_memberId, year);
        ShowResult(result.Message, result.Success);
        if (result.Success)
            await LoadAllAsync();
    }

    [RelayCommand]
    private void Back() => _navigation.NavigateTo<MembersViewModel>();

    /// <summary>پس از هر تغییر مالی: Snapshot امروز به‌روز می‌شود و صفحه بازخوانی می‌شود</summary>
    private async Task RefreshAfterFinancialChangeAsync()
    {
        await _scoreService.RecordDailyScoresAsync();
        await LoadAllAsync();
    }

    private void ShowResult(string message, bool success)
    {
        Message = message;
        IsError = !success;
    }
}
