using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Application.Interfaces;

/// <summary>کاربر جاری برنامه (برای Audit و kiểm دسترسی)</summary>
public interface ICurrentUserService
{
    UserDto? CurrentUser { get; }
    void SetCurrentUser(UserDto? user);

    string UserName => CurrentUser?.Username ?? "System";
    bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
}
