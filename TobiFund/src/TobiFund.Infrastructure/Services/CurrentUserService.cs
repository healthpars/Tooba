using TobiFund.Application.DTOs;
using TobiFund.Application.Interfaces;

namespace TobiFund.Infrastructure.Services;

/// <summary>نگه‌داری کاربر جاری (Session desktop تک‌کاربره)</summary>
public class CurrentUserService : ICurrentUserService
{
    public UserDto? CurrentUser { get; private set; }

    public void SetCurrentUser(UserDto? user) => CurrentUser = user;
}
