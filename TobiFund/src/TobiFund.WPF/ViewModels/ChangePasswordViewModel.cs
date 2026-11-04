using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.Interfaces;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه تغییر رمز عبور</summary>
public partial class ChangePasswordViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordViewModel(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
        Title = "تغییر رمز عبور";
    }

    [ObservableProperty]
    private string _oldPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    public event Action<bool>? RequestClose;

    [RelayCommand]
    private async Task SaveAsync()
    {
        Message = string.Empty;
        IsError = false;

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            Message = "رمز عبور جدید را وارد کنید.";
            IsError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            Message = "رمز عبور جدید با تکرار آن یکسان نیست.";
            IsError = true;
            return;
        }

        var userId = _currentUser.CurrentUser?.Id ?? 0;
        var result = await _authService.ChangePasswordAsync(userId, OldPassword, NewPassword);
        Message = result.Message;
        IsError = !result.Success;

        if (result.Success)
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            RequestClose?.Invoke(true);
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    /// <summary>پاک‌سازی تمام فیلدها (برای نمایش بعدی پنجره، چون ViewModel به‌صورت Singleton ثبت شده است)</summary>
    public void Clear()
    {
        OldPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        Message = string.Empty;
        IsError = false;
    }
}
