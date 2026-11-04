using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه مدیریت اعضا (لیست + جستجو + فرم افزودن/ویرایش + حذف منطقی + واردات اکسل)</summary>
public partial class MembersViewModel : ViewModelBase, INavigationAware
{
    private readonly IMemberService _memberService;
    private readonly IMemberImportService _importService;
    private readonly IExcelImportService _excelImport;
    private readonly INavigationService _navigation;

    public MembersViewModel(
        IMemberService memberService,
        IMemberImportService importService,
        IExcelImportService excelImport,
        INavigationService navigation)
    {
        _memberService = memberService;
        _importService = importService;
        _excelImport = excelImport;
        _navigation = navigation;
        Title = "مدیریت اعضا";
    }

    [ObservableProperty]
    private ObservableCollection<MemberDto> _members = new();

    [ObservableProperty]
    private MemberDto? _selectedMember;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // ---------- فرم عضو ----------
    [ObservableProperty]
    private int? _editingMemberId;

    [ObservableProperty]
    private string _memberCode = string.Empty;

    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private DateTime? _joinDate = DateTime.Today;

    /// <summary>0 = فعال، 1 = غیرفعال</summary>
    [ObservableProperty]
    private int _statusIndex;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _formMessage = string.Empty;

    [ObservableProperty]
    private bool _formIsError;

    public bool IsEditMode => EditingMemberId.HasValue;

    partial void OnEditingMemberIdChanged(int? value) => OnPropertyChanged(nameof(IsEditMode));

    public void OnNavigatedTo(object? parameter)
    {
        if (parameter is string s && s == "new")
        {
            New();
            _ = LoadAsync();
        }
        else
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var list = await _memberService.GetAllAsync(string.IsNullOrWhiteSpace(SearchText) ? null : SearchText);
        Members.Clear();
        foreach (var member in list)
            Members.Add(member);
    }

    [RelayCommand]
    private void New()
    {
        EditingMemberId = null;
        MemberCode = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Phone = string.Empty;
        JoinDate = DateTime.Today;
        StatusIndex = 0;
        Notes = string.Empty;
        FormMessage = string.Empty;
        FormIsError = false;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedMember is null) return;
        EditingMemberId = SelectedMember.Id;
        MemberCode = SelectedMember.MemberCode;
        FirstName = SelectedMember.FirstName;
        LastName = SelectedMember.LastName;
        Phone = SelectedMember.Phone ?? string.Empty;
        JoinDate = SelectedMember.JoinDate;
        StatusIndex = SelectedMember.Status == MemberStatus.Active ? 0 : 1;
        Notes = SelectedMember.Notes ?? string.Empty;
        FormMessage = string.Empty;
        FormIsError = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        FormMessage = string.Empty;
        FormIsError = false;

        if (EditingMemberId.HasValue)
        {
            var result = await _memberService.UpdateAsync(new UpdateMemberDto
            {
                Id = EditingMemberId.Value,
                FirstName = FirstName,
                LastName = LastName,
                Phone = Phone,
                Status = StatusIndex == 0 ? MemberStatus.Active : MemberStatus.Inactive,
                Notes = Notes
            });
            FormMessage = result.Message;
            FormIsError = !result.Success;
        }
        else
        {
            var result = await _memberService.CreateAsync(new CreateMemberDto
            {
                MemberCode = MemberCode,
                FirstName = FirstName,
                LastName = LastName,
                Phone = Phone,
                JoinDate = JoinDate ?? DateTime.Today,
                Notes = Notes
            });
            FormMessage = result.Message;
            FormIsError = !result.Success;
        }

        if (!FormIsError)
        {
            New();
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedMember is null) return;
        if (!DialogService.Confirm(
                $"آیا از حذف عضو «{SelectedMember.FullName}» مطمئن هستید؟\n(حذف منطقی است و اطلاعات مالی حفظ می‌شود)"))
            return;

        var result = await _memberService.SoftDeleteAsync(SelectedMember.Id);
        DialogService.ShowInfo(result.Message, result.Success ? "موفق" : "خطا");
        await LoadAsync();
    }

    [RelayCommand]
    private void OpenDetail()
    {
        if (SelectedMember is null) return;
        _navigation.NavigateTo<MemberDetailViewModel>(SelectedMember.Id);
    }

    /// <summary>واردات اعضا از فایل اکسل (ستون‌ها: شناسه، نام، نام خانوادگی، سرمایه، وام)</summary>
    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "فایل اکسل|*.xlsx;*.xlsm;*.xls",
            Title = "انتخاب فایل اکسل اعضا — ستون‌ها: شناسه، نام، نام خانوادگی، سرمایه، وام",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var rows = _excelImport.ReadMembers(dialog.FileName);
            if (rows.Count == 0)
            {
                DialogService.ShowWarning("هیچ ردیف معتبری در فایل یافت نشد.");
                return;
            }

            var result = await _importService.ImportMembersAsync(rows);
            var message = result.Message;
            if (result.Data is { Errors.Count: > 0 } data)
            {
                var errors = string.Join("\n", data.Errors.Take(20));
                if (data.Errors.Count > 20)
                    errors += $"\n… و {data.Errors.Count - 20} خطای دیگر";
                message += "\n\nخطاها:\n" + errors;
            }
            DialogService.ShowInfo(message, result.Success ? "واردات اکسل" : "خطا در واردات");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            DialogService.ShowError("خطا در خواندن فایل اکسل: " + ex.Message);
        }
    }
}
