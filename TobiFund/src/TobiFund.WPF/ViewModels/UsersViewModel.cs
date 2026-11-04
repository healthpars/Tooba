using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;
using TobiFund.Domain.Enums;

namespace TobiFund.WPF.ViewModels;

/// <summary>ViewModel صفحه کاربران (فقط مدیر — RBAC)</summary>
public partial class UsersViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public UsersViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "کاربران";
    }

    [ObservableProperty]
    private ObservableCollection<UserDto> _users = new();

    [ObservableProperty]
    private UserDto? _selectedUser;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary>0 = مدیر، 1 = اپراتور</summary>
    [ObservableProperty]
    private int _roleIndex = 1;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var users = await _authService.GetUsersAsync();
        Users.Clear();
        foreach (var user in users)
            Users.Add(user);
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        Message = string.Empty;
        var result = await _authService.CreateUserAsync(new CreateUserDto
        {
            Username = Username,
            FullName = FullName,
            Password = Password,
            Role = RoleIndex == 0 ? UserRole.Admin : UserRole.Operator
        });

        Message = result.Message;
        IsError = !result.Success;
        if (result.Success)
        {
            Username = string.Empty;
            FullName = string.Empty;
            Password = string.Empty;
            RoleIndex = 1;
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (SelectedUser is null) return;
        var result = await _authService.SetUserActiveAsync(SelectedUser.Id, !SelectedUser.IsActive);
        if (!result.Success)
        {
            Services.DialogService.ShowError(result.Message);
            return;
        }
        await LoadAsync();
    }
}
