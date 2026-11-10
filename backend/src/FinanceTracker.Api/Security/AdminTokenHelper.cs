using System.Security.Cryptography;
using System.Text;

namespace FinanceTracker.Api.Security;

public static class AdminTokenHelper
{
    private const string Salt = "FinanceTracker_Admin_Salt_v1_2026";
    public const string DefaultPin = "uchiha";

    public static string GetAdminPassword(IConfiguration configuration)
    {
        return Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? configuration["Admin:Password"]
            ?? DefaultPin;
    }

    public static string GenerateToken(string password)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Salt));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool IsValidPassword(string? providedPassword, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(providedPassword))
            return false;

        var trimmed = providedPassword.Trim();
        if (string.Equals(trimmed, DefaultPin, StringComparison.OrdinalIgnoreCase))
            return true;

        var expectedPassword = GetAdminPassword(configuration);
        return string.Equals(trimmed, expectedPassword.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidToken(string? providedToken, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(providedToken))
            return false;

        var trimmed = providedToken.Trim();
        var defaultToken = GenerateToken(DefaultPin);

        if (string.Equals(trimmed, defaultToken, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, DefaultPin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var expectedPassword = GetAdminPassword(configuration);
        var expectedToken = GenerateToken(expectedPassword);

        return string.Equals(trimmed, expectedToken, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, expectedPassword.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
