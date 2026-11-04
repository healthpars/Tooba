using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Services;

/// <summary>سرویس ناوبری بین صفحات (MVVM)</summary>
public interface INavigationService
{
    ViewModelBase? CurrentView { get; }
    event Action? CurrentViewChanged;
    void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase;
}
