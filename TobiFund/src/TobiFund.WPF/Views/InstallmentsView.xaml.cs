using System.Windows;
using System.Windows.Controls;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Views;

public partial class InstallmentsView : UserControl
{
    public InstallmentsView()
    {
        InitializeComponent();
    }

    private InstallmentsViewModel ViewModel => (InstallmentsViewModel)DataContext;

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoadCommand.CanExecute(null))
            await ViewModel.LoadCommand.ExecuteAsync(null);
    }
}
