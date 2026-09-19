using System.Text.RegularExpressions;

namespace ReplayAssistant.Core;

public static partial class EpicAccountId
{
    [GeneratedRegex("^[0-9A-Fa-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex Hex32();

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = value.Trim().Replace("-", "", StringComparison.Ordinal);
        return Hex32().IsMatch(compact) ? compact.ToUpperInvariant() : null;
    }
}
