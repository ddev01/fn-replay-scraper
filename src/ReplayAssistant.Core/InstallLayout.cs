namespace ReplayAssistant.Core;

public static class InstallLayout
{
    public static void CopyToInstallDir(string sourceDirectory, AppPaths paths)
    {
        paths.EnsureDirectories();
        CopyIfExists(Path.Combine(sourceDirectory, AppPaths.ExeFileName), paths.InstalledExePath);
        CopyIfExists(
            Path.Combine(sourceDirectory, AppPaths.UpdaterFileName),
            paths.InstalledUpdaterPath
        );

        var example = Path.Combine(sourceDirectory, "appsettings.example.json");
        var sourceSettings = Path.Combine(sourceDirectory, "appsettings.json");
        if (!File.Exists(paths.SettingsPath))
        {
            if (File.Exists(sourceSettings))
            {
                File.Copy(sourceSettings, paths.SettingsPath);
            }
            else if (File.Exists(example))
            {
                File.Copy(example, paths.SettingsPath);
            }
        }
    }

    public static void CreateStartupShortcut(AppPaths paths)
    {
        StartupShortcut.Remove();
        LogonTask.Register(paths.InstalledExePath);
    }

    private static void CopyIfExists(string source, string dest)
    {
        if (!File.Exists(source))
        {
            return;
        }

        if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        File.Copy(source, dest, overwrite: true);
    }
}
