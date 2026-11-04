using System.Windows;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF;

public partial class ChangePasswordWindow : Window
{
    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordViewModel viewModel)
        {
            // پاک‌سازی فیلدها در هر بار نمایش (ViewModel Singleton است و مقادیر قبلی را نگه می‌دارد)
            viewModel.Clear();
            viewModel.RequestClose -= OnRequestClose;
            viewModel.RequestClose += OnRequestClose;
        }
    }

    private void OnRequestClose(bool success)
    {
        DialogResult = success;
        Close();
    }
}
