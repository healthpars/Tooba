using System.Windows;
using System.Windows.Controls;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private DashboardViewModel ViewModel => (DashboardViewModel)DataContext;

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoadCommand.CanExecute(null))
            await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private void TopMembers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ViewModel.OpenMemberCommand.Execute(ViewModel.SelectedTopMember);
    }
}
