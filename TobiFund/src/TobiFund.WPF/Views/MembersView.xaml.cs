using System.Windows;
using System.Windows.Controls;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Views;

public partial class MembersView : UserControl
{
    public MembersView()
    {
        InitializeComponent();
    }

    private MembersViewModel ViewModel => (MembersViewModel)DataContext;

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoadCommand.CanExecute(null))
            await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private void MembersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ViewModel.OpenDetailCommand.Execute(null);
    }
}
