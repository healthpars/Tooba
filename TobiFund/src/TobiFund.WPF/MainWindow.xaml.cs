using System.Windows;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
            await viewModel.LoadedCommand.ExecuteAsync(null);
    }
}
