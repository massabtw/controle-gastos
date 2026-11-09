using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FinanceTracker.Application.Helpers;

public static class CurrencyParser
{
    public static decimal? Parse(object? amountObj)
    {
        if (amountObj == null)
            return null;

        if (amountObj is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Number)
            {
                if (jsonElement.TryGetDecimal(out var decVal))
                    return decVal;
                if (jsonElement.TryGetDouble(out var dblVal))
                    return Convert.ToDecimal(dblVal);
            }
            else if (jsonElement.ValueKind == JsonValueKind.String)
            {
                return ParseString(jsonElement.GetString());
            }
            else if (jsonElement.ValueKind == JsonValueKind.Null || jsonElement.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            return null;
        }

        if (amountObj is decimal d)
            return d;

        if (amountObj is int or long or double or float)
            return Convert.ToDecimal(amountObj, CultureInfo.InvariantCulture);

        if (amountObj is string s)
            return ParseString(s);

        return null;
    }

    public static decimal? ParseString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var clean = input.Trim();

        // Strip "R$", "r$", currency symbols and whitespaces
        clean = Regex.Replace(clean, @"(?i)r\$\s*|[\$\s]", "").Trim();

        // If the string contains both . and , (e.g. "1.250,50"), remove . and replace , with .
        if (clean.Contains('.') && clean.Contains(','))
        {
            clean = clean.Replace(".", "").Replace(',', '.');
        }
        // If it contains only , (e.g. "42,50"), replace , with .
        else if (clean.Contains(','))
        {
            clean = clean.Replace(',', '.');
        }

        // Keep only digits, dot, and negative sign
        clean = Regex.Replace(clean, @"[^0-9\.\-]", "");

        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
