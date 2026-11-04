using TobiFund.Application.Common;
using TobiFund.Application.DTOs;
using TobiFund.Domain.Enums;

namespace TobiFund.Tests;

/// <summary>تست‌های احراز هویت و هش رمز عبور (بخش ۳، ۳۱ سند)</summary>
public class AuthServiceTests
{
    [Fact]
    public void PasswordHasher_Verify_Works()
    {
        var hash = PasswordHasher.Hash("123456789");
        Assert.NotEqual("123456789", hash); // هرگز Plain Text ذخیره نمی‌شود
        Assert.True(PasswordHasher.Verify("123456789", hash));
        Assert.False(PasswordHasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void PasswordHasher_SamePasswordDifferentHashes()
    {
        var h1 = PasswordHasher.Hash("secret");
        var h2 = PasswordHasher.Hash("secret");
        Assert.NotEqual(h1, h2); // Salt تصادفی
        Assert.True(PasswordHasher.Verify("secret", h1));
        Assert.True(PasswordHasher.Verify("secret", h2));
    }

    [Fact]
    public async Task Login_WithSeededAdmin_Succeeds()
    {
        using var ts = new TestServices();
        // کاربر admin با رمز 123456789 توسط DbInitializer ساخته می‌شود؛ اینجا دستی Seed می‌کنیم
        await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "admin",
            FullName = "مدیر سیستم",
            Role = UserRole.Admin,
            Password = "123456789"
        });

        var ok = await ts.Auth.LoginAsync("admin", "123456789");
        Assert.True(ok.Success);
        Assert.True(ok.Data!.IsDefaultPassword);
        Assert.Equal(UserRole.Admin, ok.Data.User.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_FailsWithPersianMessage()
    {
        using var ts = new TestServices();
        await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "admin", FullName = "مدیر", Role = UserRole.Admin, Password = "123456789"
        });

        var fail = await ts.Auth.LoginAsync("admin", "wrong");
        Assert.False(fail.Success);
        Assert.Equal("نام کاربری یا رمز عبور صحیح نیست.", fail.Message);
    }

    [Fact]
    public async Task ChangePassword_Works_AndOldPasswordStopsWorking()
    {
        using var ts = new TestServices();
        var user = await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "op1", FullName = "اپراتور", Role = UserRole.Operator, Password = "111111"
        });
        var userId = user.Data!.Id;

        var change = await ts.Auth.ChangePasswordAsync(userId, "111111", "newpass123");
        Assert.True(change.Success);

        Assert.False((await ts.Auth.LoginAsync("op1", "111111")).Success);
        Assert.True((await ts.Auth.LoginAsync("op1", "newpass123")).Success);
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_Fails()
    {
        using var ts = new TestServices();
        var user = await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "op1", FullName = "اپراتور", Role = UserRole.Operator, Password = "111111"
        });
        var result = await ts.Auth.ChangePasswordAsync(user.Data!.Id, "bad-old", "newpass123");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Login_AuditLog_IsWritten()
    {
        using var ts = new TestServices();
        await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "op1", FullName = "اپراتور", Role = UserRole.Operator, Password = "111111"
        });
        await ts.Auth.LoginAsync("op1", "111111");

        var logs = await ts.Audit.GetRecentAsync(10);
        Assert.Contains(logs, l => l.Action == AppConstants.AuditActions.Login && l.Username == "op1");
    }

    [Fact]
    public async Task Login_EmptyFields_ReturnsEnterCredentialsMessage()
    {
        using var ts = new TestServices();
        var r1 = await ts.Auth.LoginAsync("", "123456789");
        var r2 = await ts.Auth.LoginAsync("admin", "");
        var r3 = await ts.Auth.LoginAsync("   ", "   ");
        Assert.False(r1.Success);
        Assert.False(r2.Success);
        Assert.False(r3.Success);
        Assert.Equal("نام کاربری و رمز عبور را وارد کنید.", r1.Message);
        Assert.Equal("نام کاربری و رمز عبور را وارد کنید.", r2.Message);
        Assert.Equal("نام کاربری و رمز عبور را وارد کنید.", r3.Message);
    }

    [Fact]
    public async Task Login_WithPersianDigits_Succeeds()
    {
        using var ts = new TestServices();
        await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "admin", FullName = "مدیر", Role = UserRole.Admin, Password = "123456789"
        });

        // کاربر با کیبورد فارسی رمز را به‌صورت ۱۲۳۴۵۶۷۸۹ وارد می‌کند
        var ok = await ts.Auth.LoginAsync("admin", "۱۲۳۴۵۶۷۸۹");
        Assert.True(ok.Success);
        Assert.True(ok.Data!.IsDefaultPassword);
    }

    [Fact]
    public async Task Login_UsernameCaseInsensitive_Succeeds()
    {
        using var ts = new TestServices();
        await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "admin", FullName = "مدیر", Role = UserRole.Admin, Password = "123456789"
        });

        Assert.True((await ts.Auth.LoginAsync("ADMIN", "123456789")).Success);
        Assert.True((await ts.Auth.LoginAsync("Admin", "123456789")).Success);
        Assert.True((await ts.Auth.LoginAsync("  admin  ", "123456789")).Success);
    }

    [Fact]
    public async Task ChangePassword_WithPersianDigits_Works()
    {
        using var ts = new TestServices();
        var user = await ts.Auth.CreateUserAsync(new CreateUserDto
        {
            Username = "op1", FullName = "اپراتور", Role = UserRole.Operator, Password = "111111"
        });
        var userId = user.Data!.Id;

        // رمز عبور قدیمی به‌صورت ارقام فارسی وارد می‌شود
        var change = await ts.Auth.ChangePasswordAsync(userId, "۱۱۱۱۱۱", "newpass123");
        Assert.True(change.Success);

        // رمز جدید با ارقام فارسی قابل ورود است
        Assert.True((await ts.Auth.LoginAsync("op1", "newpass123")).Success);
    }
}
