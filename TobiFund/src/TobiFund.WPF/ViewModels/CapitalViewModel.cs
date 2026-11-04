using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه سرمایه (پرداخت سرمایه + گردش سرمایه)</summary>
public partial class CapitalViewModel : ViewModelBase
{
    private readonly IMemberService _memberService;
    private readonly ICapitalService _capitalService;

    public CapitalViewModel(IMemberService memberService, ICapitalService capitalService)
    {
        _memberService = memberService;
        _capitalService = capitalService;
        Title = "سرمایه";
    }

    [ObservableProperty]
    private ObservableCollection<MemberDto> _members = new();

    [ObservableProperty]
    private MemberDto? _selectedMember;

    [ObservableProperty]
    private ObservableCollection<CapitalTransactionDto> _transactions = new();

    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private DateTime? _transactionDate = DateTime.Today;

    /// <summary>0 = واریز، 1 = برداشت، 2 = تعدیل</summary>
    [ObservableProperty]
    private int _typeIndex;

    [ObservableProperty]
    private string _description = string.Empty;

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
        await LoadTransactionsAsync();
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadTransactionsAsync();

    [RelayCommand]
    private async Task ShowAllAsync()
    {
        SelectedMember = null;
        await LoadTransactionsAsync();
    }

    private async Task LoadTransactionsAsync()
    {
        var list = SelectedMember is null
            ? await _capitalService.GetAllAsync()
            : await _capitalService.GetMemberTransactionsAsync(SelectedMember.Id);
        Transactions.Clear();
        foreach (var t in list)
            Transactions.Add(t);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        Message = string.Empty;
        if (SelectedMember is null)
        {
            ShowResult("ابتدا عضو را انتخاب کنید.", false);
            return;
        }
        var date = TransactionDate ?? DateTime.Today;
        if (!PersianDateHelper.TryParseAmount(AmountText, out var amount))
        {
            ShowResult("مبلغ نامعتبر است.", false);
            return;
        }

        var type = TypeIndex switch
        {
            1 => TransactionType.Withdrawal,
            2 => TransactionType.Adjustment,
            _ => TransactionType.Deposit
        };

        var result = await _capitalService.AddTransactionAsync(new CreateCapitalTransactionDto
        {
            MemberId = SelectedMember.Id,
            TransactionDate = date,
            Amount = amount,
            TransactionType = type,
            Description = Description
        });

        ShowResult(result.Message, result.Success);
        if (result.Success)
        {
            AmountText = string.Empty;
            Description = string.Empty;
            await LoadTransactionsAsync();
        }
    }

    private void ShowResult(string message, bool success)
    {
        Message = message;
        IsError = !success;
    }
}
