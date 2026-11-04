using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه اقساط (فیلتر وضعیت + پرداخت قسط)</summary>
public partial class InstallmentsViewModel : ViewModelBase
{
    private readonly ILoanService _loanService;
    private readonly IScoreService _scoreService;

    public InstallmentsViewModel(ILoanService loanService, IScoreService scoreService)
    {
        _loanService = loanService;
        _scoreService = scoreService;
        Title = "اقساط";
    }

    /// <summary>0=همه، 1=آینده، 2=سررسید، 3=پرداخت جزئی، 4=پرداخت‌شده، 5=معوق</summary>
    [ObservableProperty]
    private int _statusFilterIndex;

    [ObservableProperty]
    private ObservableCollection<LoanInstallmentDto> _installments = new();

    [ObservableProperty]
    private LoanInstallmentDto? _selectedInstallment;

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

    partial void OnStatusFilterIndexChanged(int value) => _ = LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        IReadOnlyList<LoanInstallmentDto> list = StatusFilterIndex switch
        {
            1 => await _loanService.GetInstallmentsByStatusAsync(InstallmentStatus.Future),
            2 => await _loanService.GetInstallmentsByStatusAsync(InstallmentStatus.Due),
            3 => await _loanService.GetInstallmentsByStatusAsync(InstallmentStatus.PartiallyPaid),
            4 => await _loanService.GetInstallmentsByStatusAsync(InstallmentStatus.Paid),
            5 => await _loanService.GetInstallmentsByStatusAsync(InstallmentStatus.Overdue),
            _ => await _loanService.GetInstallmentsAsync()
        };

        Installments.Clear();
        foreach (var installment in list)
            Installments.Add(installment);
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        Message = string.Empty;
        if (SelectedInstallment is null)
        {
            ShowResult("ابتدا یک قسط را انتخاب کنید.", false);
            return;
        }
        var date = PayDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(PayAmountText, out var amount))
        {
            ShowResult("مبلغ پرداختی نامعتبر است.", false);
            return;
        }

        var result = await _loanService.PayInstallmentAsync(new CreateInstallmentPaymentDto
        {
            InstallmentId = SelectedInstallment.Id,
            PaymentDate = date,
            Amount = amount,
            Description = PayDescription
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            PayAmountText = string.Empty;
            PayDescription = string.Empty;
            await _scoreService.RecordDailyScoresAsync();
            await LoadAsync();
        }
    }

    private void ShowResult(string message, bool success)
    {
        Message = message;
        IsError = !success;
    }
}
