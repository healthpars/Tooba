using TobiFund.Application.Common;
using TobiFund.Application.DTOs;

namespace TobiFund.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResultDto>> LoginAsync(string username, string password);
    Task<ServiceResult> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserDto dto);
    Task<IReadOnlyList<UserDto>> GetUsersAsync();
    Task<ServiceResult> SetUserActiveAsync(int userId, bool isActive);
    string HashPassword(string password);
}
