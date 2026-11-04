using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه گزارش‌ها (Print + Export Excel + Export PDF)</summary>
public partial class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly IMemberService _memberService;
    private readonly IExportService _exportService;

    public ReportsViewModel(
        IReportService reportService,
        IMemberService memberService,
        IExportService exportService)
    {
        _reportService = reportService;
        _memberService = memberService;
        _exportService = exportService;
        Title = "گزارش‌ها";
    }

    public string[] ReportTypes { get; } =
    {
        "لیست اعضا",
        "گردش سرمایه",
        "وام‌های پرداخت‌شده",
        "اقساط",
        "آبونمان",
        "رتبه‌بندی امتیاز",
        "رویدادهای سیستم"
    };

    [ObservableProperty]
    private int _selectedReportTypeIndex;

    // فیلترها
    [ObservableProperty]
    private ObservableCollection<MemberDto> _members = new();

    [ObservableProperty]
    private MemberDto? _selectedMember;

    [ObservableProperty]
    private bool _useMemberFilter;

    [ObservableProperty]
    private DateTime? _fromDate;

    [ObservableProperty]
    private DateTime? _toDate;

    // نتایج گزارش
    [ObservableProperty]
    private ObservableCollection<MemberDto> _membersResult = new();

    [ObservableProperty]
    private ObservableCollection<CapitalTransactionDto> _capitalResult = new();

    [ObservableProperty]
    private ObservableCollection<LoanDto> _loansResult = new();

    [ObservableProperty]
    private ObservableCollection<LoanInstallmentDto> _installmentsResult = new();

    [ObservableProperty]
    private ObservableCollection<SubscriptionDto> _subscriptionsResult = new();

    [ObservableProperty]
    private ObservableCollection<RankingDto> _rankingResult = new();

    [ObservableProperty]
    private ObservableCollection<AuditLogDto> _auditResult = new();

    partial void OnSelectedReportTypeIndexChanged(int value) => _ = LoadReportAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        var members = await _memberService.GetAllAsync();
        Members.Clear();
        foreach (var member in members)
            Members.Add(member);
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        MembersResult.Clear();
        CapitalResult.Clear();
        LoansResult.Clear();
        InstallmentsResult.Clear();
        SubscriptionsResult.Clear();
        RankingResult.Clear();
        AuditResult.Clear();

        DateTime? from = FromDate;
        DateTime? to = ToDate;

        int? memberId = UseMemberFilter ? SelectedMember?.Id : null;

        switch (SelectedReportTypeIndex)
        {
            case 0:
                foreach (var m in await _reportService.MembersReportAsync())
                    MembersResult.Add(m);
                break;
            case 1:
                foreach (var t in await _reportService.CapitalReportAsync(memberId, from, to))
                    CapitalResult.Add(t);
                break;
            case 2:
                foreach (var l in await _reportService.LoansReportAsync(memberId))
                    LoansResult.Add(l);
                break;
            case 3:
                foreach (var i in await _reportService.InstallmentsReportAsync(null, memberId))
                    InstallmentsResult.Add(i);
                break;
            case 4:
                foreach (var s in await _reportService.SubscriptionsReportAsync(null, memberId))
                    SubscriptionsResult.Add(s);
                break;
            case 5:
                foreach (var r in await _reportService.ScoresReportAsync())
                    RankingResult.Add(r);
                break;
            case 6:
                foreach (var a in await _reportService.AuditReportAsync(from, to))
                    AuditResult.Add(a);
                break;
        }
    }

    private (string Title, string[] Columns, List<string[]> Rows)? GetCurrentReport()
    {
        switch (SelectedReportTypeIndex)
        {
            case 0:
                return ("گزارش لیست اعضا",
                    new[] { "شناسه", "نام", "نام خانوادگی", "تلفن", "تاریخ عضویت", "وضعیت", "سرمایه", "مانده وام", "اقساط معوق", "آبونمان معوق", "امتیاز", "رتبه" },
                    MembersResult.Select(m => new[]
                    {
                        m.MemberCode, m.FirstName, m.LastName, m.Phone ?? "", m.JoinDatePersian, m.StatusName,
                        F(m.TotalCapital), F(m.OutstandingLoan), F(m.DueInstallments), F(m.DueSubscription),
                        F(m.TotalScore), F(m.Rank)
                    }).ToList());
            case 1:
                return ("گزارش گردش سرمایه",
                    new[] { "تاریخ", "شناسه عضو", "نام عضو", "نوع", "مبلغ", "توضیحات", "ثبت‌کننده" },
                    CapitalResult.Select(t => new[]
                    {
                        t.TransactionDatePersian, t.MemberCode, t.MemberName, t.TypeName,
                        F(t.Amount), t.Description ?? "", t.CreatedBy ?? ""
                    }).ToList());
            case 2:
                return ("گزارش وام‌های پرداخت‌شده",
                    new[] { "تاریخ", "شناسه عضو", "نام عضو", "مبلغ وام", "تعداد اقساط", "مبلغ قسط", "پرداخت‌شده", "مانده", "توضیحات" },
                    LoansResult.Select(l => new[]
                    {
                        l.LoanDatePersian, l.MemberCode, l.MemberName, F(l.Amount), F(l.InstallmentCount),
                        F(l.InstallmentAmount), F(l.PaidAmount), F(l.RemainingAmount), l.Description ?? ""
                    }).ToList());
            case 3:
                return ("گزارش اقساط",
                    new[] { "شماره قسط", "شناسه عضو", "نام عضو", "سررسید", "مبلغ", "پرداخت‌شده", "مانده", "وضعیت" },
                    InstallmentsResult.Select(i => new[]
                    {
                        F(i.InstallmentNumber), i.MemberCode, i.MemberName, i.DueDatePersian,
                        F(i.Amount), F(i.PaidAmount), F(i.RemainingAmount), i.StatusName
                    }).ToList());
            case 4:
                return ("گزارش آبونمان",
                    new[] { "سال/ماه", "شناسه عضو", "نام عضو", "سررسید", "مبلغ", "پرداخت‌شده", "مانده", "وضعیت" },
                    SubscriptionsResult.Select(s => new[]
                    {
                        $"{s.YearMonth} {s.MonthName}", s.MemberCode, s.MemberName, s.DueDatePersian,
                        F(s.Amount), F(s.PaidAmount), F(s.RemainingAmount), s.StatusName
                    }).ToList());
            case 5:
                return ("گزارش رتبه‌بندی اعضا",
                    new[] { "رتبه", "شناسه", "نام", "سرمایه", "مانده وام", "اقساط معوق", "آبونمان معوق", "امتیاز" },
                    RankingResult.Select(r => new[]
                    {
                        F(r.Rank), r.MemberCode, r.FullName, F(r.TotalCapital),
                        F(r.OutstandingLoan), F(r.DueInstallments), F(r.DueSubscription), F(r.TotalScore)
                    }).ToList());
            case 6:
                return ("گزارش رویدادهای سیستم",
                    new[] { "تاریخ", "کاربر", "عملیات", "نوع موجودیت", "شناسه", "توضیحات" },
                    AuditResult.Select(a => new[]
                    {
                        a.CreatedAtPersian, a.Username, a.Action, a.EntityType ?? "", a.EntityId ?? "", a.Description ?? ""
                    }).ToList());
            default:
                return null;
        }
    }

    private static string F(decimal value) => PersianDateHelper.ToPersianDigits(value.ToString("N0"));
    private static string F(int value) => PersianDateHelper.ToPersianDigits(value.ToString("N0"));
    private static string F(long value) => PersianDateHelper.ToPersianDigits(value.ToString("N0"));

    [RelayCommand]
    private void ExportExcel()
    {
        var report = GetCurrentReport();
        if (report is null || report.Value.Rows.Count == 0)
        {
            DialogService.ShowWarning("ابتدا گزارش را بارگذاری کنید؛ داده‌ای برای خروجی وجود ندارد.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook|*.xlsx",
            FileName = "Report.xlsx",
            Title = "ذخیره گزارش Excel"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _exportService.ExportToExcel(report.Value.Title, report.Value.Columns, report.Value.Rows, dialog.FileName);
            DialogService.ShowInfo("گزارش Excel با موفقیت ذخیره شد.");
        }
        catch (Exception ex)
        {
            DialogService.ShowError("خطا در ذخیره Excel: " + ex.Message);
        }
    }

    [RelayCommand]
    private void ExportPdf()
    {
        var report = GetCurrentReport();
        if (report is null || report.Value.Rows.Count == 0)
        {
            DialogService.ShowWarning("ابتدا گزارش را بارگذاری کنید؛ داده‌ای برای خروجی وجود ندارد.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF|*.pdf",
            FileName = "Report.pdf",
            Title = "ذخیره گزارش PDF"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _exportService.ExportToPdf(report.Value.Title, report.Value.Columns, report.Value.Rows, dialog.FileName);
            DialogService.ShowInfo("گزارش PDF با موفقیت ذخیره شد.");
        }
        catch (Exception ex)
        {
            DialogService.ShowError("خطا در ذخیره PDF: " + ex.Message);
        }
    }

    [RelayCommand]
    private void Print()
    {
        var report = GetCurrentReport();
        if (report is null || report.Value.Rows.Count == 0)
        {
            DialogService.ShowWarning("ابتدا گزارش را بارگذاری کنید؛ داده‌ای برای چاپ وجود ندارد.");
            return;
        }

        try
        {
            _exportService.Print(report.Value.Title, report.Value.Columns, report.Value.Rows);
        }
        catch (Exception ex)
        {
            DialogService.ShowError("خطا در چاپ: " + ex.Message);
        }
    }
}
