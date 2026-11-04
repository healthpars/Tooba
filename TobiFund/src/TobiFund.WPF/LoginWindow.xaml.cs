using System.Windows;
using TobiFund.WPF.Services;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel)
        {
            viewModel.RequestClose -= OnRequestClose;
            viewModel.RequestClose += OnRequestClose;
        }
        UsernameBox.Focus();
    }

    protected override void OnClosed(EventArgs e)
    {
        // پاک‌سازی هنگام بستن پنجره انجام می‌شود (نه در Loaded) تا مقادیر تایپ‌شده پاک نشوند
        if (DataContext is LoginViewModel viewModel)
            viewModel.Clear();
        base.OnClosed(e);
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel viewModel) return;
        try
        {
            // نام کاربری و رمز عبور مستقیماً از کنترل‌ها خوانده می‌شوند
            await viewModel.LoginAsync(UsernameBox.Text, PasswordBox.Password);
        }
        catch (Exception ex)
        {
            DialogService.ShowError("خطا در ورود به سیستم: " + ex.Message);
        }
    }

    private void OnRequestClose(bool success)
    {
        DialogResult = success;
        Close();
    }
}
