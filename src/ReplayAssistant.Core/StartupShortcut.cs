using System.Runtime.InteropServices;

namespace ReplayAssistant.Core;

public static class StartupShortcut
{
    public const string ShortcutFileName = "Replay Assistant.lnk";

    public static string StartupFolder =>
        Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    public static string ShortcutPath => Path.Combine(StartupFolder, ShortcutFileName);

    public static void Create(string targetExePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetExePath);
        var working = Path.GetDirectoryName(targetExePath) ?? "";
        var type = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is unavailable.");
        var shell = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Could not create WScript.Shell.");
        try
        {
            dynamic shortcut = ((dynamic)shell).CreateShortcut(ShortcutPath);
            shortcut.TargetPath = targetExePath;
            shortcut.WorkingDirectory = working;
            shortcut.WindowStyle = 7;
            shortcut.Description = "Replay Assistant";
            shortcut.IconLocation = targetExePath + ",0";
            shortcut.Save();
            Marshal.FinalReleaseComObject(shortcut);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }

    public static void Remove()
    {
        if (File.Exists(ShortcutPath))
        {
            File.Delete(ShortcutPath);
        }
    }
}
