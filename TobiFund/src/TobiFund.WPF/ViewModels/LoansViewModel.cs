using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه وام‌ها (ثبت وام + لیست وام‌ها + اقساط هر وام)</summary>
public partial class LoansViewModel : ViewModelBase
{
    private readonly IMemberService _memberService;
    private readonly ILoanService _loanService;
    private readonly IScoreService _scoreService;
    private readonly ICurrentUserService _currentUser;

    public LoansViewModel(
        IMemberService memberService,
        ILoanService loanService,
        IScoreService scoreService,
        ICurrentUserService currentUser)
    {
        _memberService = memberService;
        _loanService = loanService;
        _scoreService = scoreService;
        _currentUser = currentUser;
        Title = "وام‌ها";
        IsInstallmentCountEditable = _currentUser.IsAdmin;
    }

    [ObservableProperty]
    private ObservableCollection<MemberDto> _members = new();

    [ObservableProperty]
    private MemberDto? _selectedMember;

    [ObservableProperty]
    private ObservableCollection<LoanDto> _loans = new();

    [ObservableProperty]
    private LoanDto? _selectedLoan;

    [ObservableProperty]
    private ObservableCollection<LoanInstallmentDto> _installments = new();

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private DateTime? _loanDate = DateTime.Today;

    [ObservableProperty]
    private string _installmentCountText = "10";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isInstallmentCountEditable;

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

        var loans = await _loanService.GetLoansAsync();
        Loans.Clear();
        foreach (var loan in loans)
            Loans.Add(loan);
    }

    [RelayCommand]
    private async Task CreateLoanAsync()
    {
        Message = string.Empty;
        if (SelectedMember is null)
        {
            ShowResult("ابتدا عضو را انتخاب کنید.", false);
            return;
        }
        var date = LoanDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(AmountText, out var amount))
        {
            ShowResult("مبلغ وام نامعتبر است.", false);
            return;
        }
        if (!int.TryParse(PersianDateHelper.ToEnglishDigits(InstallmentCountText.Trim()), out var count) || count <= 0)
        {
            ShowResult("تعداد اقساط نامعتبر است.", false);
            return;
        }

        var result = await _loanService.CreateLoanAsync(new CreateLoanDto
        {
            MemberId = SelectedMember.Id,
            Amount = amount,
            LoanDate = date,
            InstallmentCount = count,
            Description = Description
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            AmountText = string.Empty;
            Description = string.Empty;
            var loans = await _loanService.GetLoansAsync();
            Loans.Clear();
            foreach (var loan in loans)
                Loans.Add(loan);
            await _scoreService.RecordDailyScoresAsync();
        }
    }

    [RelayCommand]
    private async Task LoadInstallmentsAsync()
    {
        Installments.Clear();
        if (SelectedLoan is null) return;
        var list = await _loanService.GetInstallmentsAsync(loanId: SelectedLoan.Id);
        foreach (var installment in list)
            Installments.Add(installment);
    }

    private void ShowResult(string message, bool success)
    {
        Message = message;
        IsError = !success;
    }
}
