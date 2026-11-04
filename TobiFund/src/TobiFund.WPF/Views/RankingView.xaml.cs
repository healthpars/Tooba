using System.Windows;
using System.Windows.Controls;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Views;

public partial class RankingView : UserControl
{
    public RankingView()
    {
        InitializeComponent();
    }

    private RankingViewModel ViewModel => (RankingViewModel)DataContext;

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoadCommand.CanExecute(null))
            await ViewModel.LoadCommand.ExecuteAsync(null);
    }

    private void Ranking_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ViewModel.OpenMemberCommand.Execute(ViewModel.SelectedRanking);
    }
}
