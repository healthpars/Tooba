using CommunityToolkit.Mvvm.ComponentModel;
using TobiFund.Application.Interfaces;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه ورود (Login)</summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public LoginViewModel(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
        Title = "ورود به سیستم";
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>درخواست بستن پنجره (true = ورود موفق)</summary>
    public event Action<bool>? RequestClose;

    /// <summary>
    /// ورود به سیستم. نام کاربری و رمز عبور مستقیماً از کنترل‌های صفحه خوانده شده
    /// و به این متد پاس داده می‌شوند تا ورود همیشه قابل‌اطمینان باشد.
    /// </summary>
    public async Task LoginAsync(string? username, string? password)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _authService.LoginAsync(username ?? string.Empty, password ?? string.Empty);
            if (!result.Success || result.Data is null)
            {
                ErrorMessage = result.Message;
                return;
            }

            _currentUser.SetCurrentUser(result.Data.User);
            AppSession.MustChangePassword = result.Data.IsDefaultPassword;
            RequestClose?.Invoke(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Clear() => ErrorMessage = string.Empty;
}
