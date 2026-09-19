namespace ReplayAssistant.Core;

public static class PlatformMapper
{
    public const string Epic = "epic";
    public const string Psn = "psn";
    public const string Xbl = "xbl";
    public const string Nintendo = "nintendo";

    public static string? ToNameFn(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var p = raw.Trim().ToUpperInvariant();
        return p switch
        {
            "WIN" or "WINDOWS" or "PC" or "EPIC" => Epic,
            "PSN" or "PS4" or "PS5" or "PS4PRO" or "PLAYSTATION" => Psn,
            "XBL" or "XBOX" or "XB1" or "XSX" or "XSS" or "XBOXONE" or "XBOXSERIES" => Xbl,
            "SWT" or "SWITCH" or "NSW" or "NINTENDO" => Nintendo,
            _ => null,
        };
    }
}
