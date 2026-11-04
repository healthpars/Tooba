using System.Windows;
using System.Windows.Controls;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Views;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
    }

    private BackupViewModel ViewModel => (BackupViewModel)DataContext;

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadCommand.Execute(null);
    }
}
