using System.Security.Cryptography;

namespace TobiFund.Application.Common;

/// <summary>
/// هش امن رمز عبور با PBKDF2-SHA256.
/// رمز عبور هرگز به‌صورت متن ساده ذخیره نمی‌شود.
/// فرمت ذخیره‌سازی: PBKDF2.v1.{iterations}.{salt-base64}.{hash-base64}
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "PBKDF2v1"; // بدون نقطه — جداکننده فیلدها '.' است

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Prefix}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;
        try
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 4 || parts[0] != Prefix) return false;
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch
        {
            return false;
        }
    }
}
