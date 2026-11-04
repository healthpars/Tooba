using Microsoft.Extensions.DependencyInjection;
using TobiFund.WPF.ViewModels;

namespace TobiFund.WPF.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ViewModelBase? CurrentView { get; private set; }

    public event Action? CurrentViewChanged;

    public void NavigateTo<TViewModel>(object? parameter = null) where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        if (viewModel is INavigationAware aware)
            aware.OnNavigatedTo(parameter);
        CurrentView = viewModel;
        CurrentViewChanged?.Invoke();
    }
}
