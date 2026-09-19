namespace ReplayAssistant.Core;

public sealed class AppPaths
{
    public const string ProductFolderName = "ReplayAssistant";
    public const string ExeFileName = "ReplayAssistant.exe";
    public const string UpdaterFileName = "ReplayAssistant.Update.exe";
    public const string MutexName = @"Local\ReplayAssistant.SingleInstance";

    public AppPaths(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        RootDirectory = rootDirectory;
    }

    public string RootDirectory { get; }

    public string DatabasePath => Path.Combine(RootDirectory, "state.db");

    public string LogsDirectory => Path.Combine(RootDirectory, "logs");

    public string UpdateDirectory => Path.Combine(RootDirectory, "update");

    public string SettingsPath => Path.Combine(RootDirectory, "appsettings.json");

    public string InstalledExePath => Path.Combine(RootDirectory, ExeFileName);

    public string InstalledUpdaterPath => Path.Combine(RootDirectory, UpdaterFileName);

    public static AppPaths Default { get; } =
        new(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ProductFolderName
            )
        );

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(UpdateDirectory);
    }
}
