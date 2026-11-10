using System.Security.Cryptography;
using System.Text;

namespace FinanceTracker.Api.Security;

public static class AdminTokenHelper
{
    private const string Salt = "FinanceTracker_Admin_Salt_v1_2026";

    public static string GetAdminPassword(IConfiguration configuration)
    {
        return Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? configuration["Admin:Password"]
            ?? Environment.GetEnvironmentVariable("APP_PIN")
            ?? "1212";
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

        var expectedPassword = GetAdminPassword(configuration);
        return string.Equals(providedPassword.Trim(), expectedPassword.Trim(), StringComparison.Ordinal);
    }

    public static bool IsValidToken(string? providedToken, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(providedToken))
            return false;

        var expectedPassword = GetAdminPassword(configuration);
        var expectedToken = GenerateToken(expectedPassword);

        var trimmed = providedToken.Trim();
        return string.Equals(trimmed, expectedToken, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, expectedPassword.Trim(), StringComparison.Ordinal);
    }
}
