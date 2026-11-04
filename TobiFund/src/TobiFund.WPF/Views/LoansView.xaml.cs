using System.Windows;
using System.Windows.Controls;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Views;

public partial class LoansView : UserControl
{
    public LoansView()
    {
        InitializeComponent();
    }

    private LoansViewModel ViewModel => (LoansViewModel)DataContext;

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoadCommand.CanExecute(null))
            await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void LoansGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.LoadInstallmentsCommand.CanExecute(null))
            await ViewModel.LoadInstallmentsCommand.ExecuteAsync(null);
    }
}
