using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه رتبه‌بندی اعضا (مرتب‌سازی بر اساس امتیاز)</summary>
public partial class RankingViewModel : ViewModelBase
{
    private readonly IScoreService _scoreService;
    private readonly INavigationService _navigation;

    public RankingViewModel(IScoreService scoreService, INavigationService navigation)
    {
        _scoreService = scoreService;
        _navigation = navigation;
        Title = "رتبه‌بندی اعضا";
    }

    [ObservableProperty]
    private ObservableCollection<RankingDto> _ranking = new();

    [ObservableProperty]
    private RankingDto? _selectedRanking;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var list = await _scoreService.GetRankingAsync();
        Ranking.Clear();
        foreach (var row in list)
            Ranking.Add(row);
    }

    [RelayCommand]
    private void OpenMember(RankingDto? ranking)
    {
        if (ranking is null) return;
        _navigation.NavigateTo<MemberDetailViewModel>(ranking.MemberId);
    }
}
