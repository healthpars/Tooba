using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TobiFund.Application.Common;
using TobiFund.Application.Interfaces;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel پنجره اصلی (منوی کناری + ناوبری + RBAC)</summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly ICurrentUserService _currentUser;
    private readonly ILoanService _loanService;
    private readonly IScoreService _scoreService;
    private readonly IAuditService _audit;

    public MainViewModel(
        INavigationService navigation,
        ICurrentUserService currentUser,
        ILoanService loanService,
        IScoreService scoreService,
        IAuditService audit)
    {
        _navigation = navigation;
        _currentUser = currentUser;
        _loanService = loanService;
        _scoreService = scoreService;
        _audit = audit;

        Title = "صندوق طوبی";
        CurrentUserName = _currentUser.CurrentUser?.FullName ?? string.Empty;
        CurrentUserRole = _currentUser.CurrentUser?.RoleName ?? string.Empty;
        IsAdmin = _currentUser.IsAdmin;

        _navigation.CurrentViewChanged += () => CurrentView = _navigation.CurrentView;
    }

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentUserName;

    [ObservableProperty]
    private string _currentUserRole;

    [ObservableProperty]
    private bool _isAdmin;

    [ObservableProperty]
    private bool _showDefaultPasswordWarning;

    /// <summary>مقداردهی اولیه هنگام بارگذاری پنجره</summary>
    [RelayCommand]
    private async Task LoadedAsync()
    {
        ShowDefaultPasswordWarning = AppSession.MustChangePassword;
        try
        {
            // به‌روزرسانی وضعیت‌ها و ثبت Snapshot امتیاز روزانه
            await _loanService.RefreshStatusesAsync();
            await _scoreService.RecordDailyScoresAsync();
        }
        catch
        {
            // خطا نباید مانع ورود شود
        }
        _navigation.NavigateTo<DashboardViewModel>();
    }

    [RelayCommand]
    private void GoDashboard() => _navigation.NavigateTo<DashboardViewModel>();

    [RelayCommand]
    private void GoMembers() => _navigation.NavigateTo<MembersViewModel>();

    [RelayCommand]
    private void GoNewMember() => _navigation.NavigateTo<MembersViewModel>("new");

    [RelayCommand]
    private void GoCapital() => _navigation.NavigateTo<CapitalViewModel>();

    [RelayCommand]
    private void GoLoans() => _navigation.NavigateTo<LoansViewModel>();

    [RelayCommand]
    private void GoInstallments() => _navigation.NavigateTo<InstallmentsViewModel>();

    [RelayCommand]
    private void GoSubscriptions() => _navigation.NavigateTo<SubscriptionsViewModel>();

    [RelayCommand]
    private void GoRanking() => _navigation.NavigateTo<RankingViewModel>();

    [RelayCommand]
    private void GoReports() => _navigation.NavigateTo<ReportsViewModel>();

    [RelayCommand]
    private void GoUsers() => _navigation.NavigateTo<UsersViewModel>();

    [RelayCommand]
    private void GoSettings() => _navigation.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    private void GoBackup() => _navigation.NavigateTo<BackupViewModel>();

    [RelayCommand]
    private void GoChangePassword()
    {
        var viewModel = App.Services.GetRequiredService<ChangePasswordViewModel>();
        var window = new ChangePasswordWindow
        {
            DataContext = viewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void DismissWarning() => ShowDefaultPasswordWarning = false;

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!DialogService.Confirm("آیا برای خروج از سیستم مطمئن هستید؟"))
            return;

        await _audit.LogAsync(AppConstants.AuditActions.Logout, description: "خروج کاربر از سیستم.");
        _currentUser.SetCurrentUser(null);
        AppSession.MustChangePassword = false;
        System.Windows.Application.Current.MainWindow?.Close();
    }
}
