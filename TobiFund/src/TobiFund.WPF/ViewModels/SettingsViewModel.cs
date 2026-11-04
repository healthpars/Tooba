using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;

namespace TobiFund.WPF.ViewModels;

/// <summary>آیتم قابل‌ویرایش تنظیمات</summary>
public class SettingItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

/// <summary>ViewModel صفحه تنظیمات (فقط مدیر)</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Title = "تنظیمات";
    }

    [ObservableProperty]
    private ObservableCollection<SettingItem> _settings = new();

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var settings = await _settingsService.GetAllAsync();
        Settings.Clear();
        foreach (var s in settings)
        {
            Settings.Add(new SettingItem
            {
                Key = s.Key,
                Value = s.Value,
                Description = s.Description ?? string.Empty
            });
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var result = await _settingsService.SaveAllAsync(
            Settings.Select(s => new SettingDto { Key = s.Key, Value = s.Value }));
        Message = result.Message;
        IsError = !result.Success;
    }
}
