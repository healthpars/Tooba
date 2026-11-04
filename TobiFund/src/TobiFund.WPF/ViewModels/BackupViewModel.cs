using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Windows;
using TobiFund.Application.Interfaces;
using TobiFund.WPF.Services;

namespace TobiFund.WPF.ViewModels;

/// <summary>آیتم فایل پشتیبان (نمایش نام فایل)</summary>
public class BackupFileItem
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public override string ToString() => FileName;
}

/// <summary>ViewModel صفحه پشتیبان‌گیری (Backup/Restore — فقط مدیر)</summary>
public partial class BackupViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;

    public BackupViewModel(IBackupService backupService)
    {
        _backupService = backupService;
        Title = "پشتیبان‌گیری";
    }

    [ObservableProperty]
    private ObservableCollection<BackupFileItem> _backupFiles = new();

    [ObservableProperty]
    private BackupFileItem? _selectedBackupFile;

    [ObservableProperty]
    private string _databasePathText = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [RelayCommand]
    private void Load()
    {
        DatabasePathText = _backupService.DatabasePath;
        BackupFiles.Clear();
        foreach (var file in _backupService.GetBackupFiles())
        {
            BackupFiles.Add(new BackupFileItem
            {
                FileName = Path.GetFileName(file),
                FullPath = file
            });
        }
    }

    [RelayCommand]
    private async Task BackupAsync()
    {
        Message = string.Empty;
        var result = await _backupService.BackupAsync();
        Message = result.Message;
        IsError = !result.Success;
        if (result.Success)
            Load();
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        Message = string.Empty;

        var path = SelectedBackupFile?.FullPath;
        if (path is null)
        {
            // انتخاب فایل از خارج از فهرست
            var dialog = new OpenFileDialog
            {
                Filter = "SQLite Database|*.db",
                Title = "انتخاب فایل پشتیبان برای بازیابی"
            };
            if (dialog.ShowDialog() != true) return;
            path = dialog.FileName;
        }

        if (!DialogService.Confirm(
                "⚠️ بازیابی دیتابیس فعلی را با فایل پشتیبان جایگزین می‌کند.\nآیا مطمئن هستید؟"))
            return;

        var result = await _backupService.RestoreAsync(path);
        Message = result.Message;
        IsError = !result.Success;

        if (result.Success && DialogService.Confirm(result.Message + "\n\nآیا برنامه راه‌اندازی مجدد شود؟"))
            Restart();
    }

    private static void Restart()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                System.Diagnostics.Process.Start(exePath);
        }
        catch
        {
            // ignored
        }
        System.Windows.Application.Current.Shutdown();
    }
}
