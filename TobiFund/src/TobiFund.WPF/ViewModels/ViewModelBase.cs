using CommunityToolkit.Mvvm.ComponentModel;

namespace TobiFund.WPF.ViewModels;

/// <summary>پایه ViewModelها</summary>
public abstract class ViewModelBase : ObservableObject
{
    public virtual string Title { get; set; } = "صندوق طوبی";
}

/// <summary>ViewModelهایی که پارامتر ناوبری دریافت می‌کنند</summary>
public interface INavigationAware
{
    void OnNavigatedTo(object? parameter);
}
