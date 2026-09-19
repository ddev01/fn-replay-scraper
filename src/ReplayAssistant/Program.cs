using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReplayAssistant.Core;

namespace ReplayAssistant;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var console = HasFlag(args, "--console");
        if (console)
        {
            NativeConsole.Attach();
            Console.WriteLine("Replay Assistant console mode. Ctrl+C to exit.");
        }

        var paths = AppPaths.Default;
        paths.EnsureDirectories();
        var cwdSettings = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var settings = SettingsLoader.Load(
            paths.SettingsPath,
            cwdSettings,
            Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        );

        if (HasFlag(args, "--uninstall-startup"))
        {
            StartupShortcut.Remove();
            LogonTask.Remove();
            Directory.CreateDirectory(paths.LogsDirectory);
            File.AppendAllText(
                Path.Combine(paths.LogsDirectory, "replay-assistant.log"),
                $"{DateTimeOffset.Now:u} Information ReplayAssistant: Removed logon task and Startup shortcut{Environment.NewLine}"
            );
            if (console)
            {
                Console.WriteLine("Removed logon task and Startup shortcut.");
            }

            return 0;
        }

        if (HasFlag(args, "--install-startup") || HasFlag(args, "--install"))
        {
            InstallLayout.CopyToInstallDir(AppContext.BaseDirectory, paths);
            InstallLayout.CreateStartupShortcut(paths);
            File.AppendAllText(
                Path.Combine(paths.LogsDirectory, "replay-assistant.log"),
                $"{DateTimeOffset.Now:u} Information ReplayAssistant: Installed logon task -> {paths.InstalledExePath}{Environment.NewLine}"
            );
            if (console)
            {
                Console.WriteLine($"Installed logon task -> {paths.InstalledExePath}");
            }

            return 0;
        }

        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            if (console)
            {
                Console.Error.WriteLine(
                    "Replay Assistant is already running. Exit the other process first."
                );
            }

            return 0;
        }

        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ReplayAssistant", "1.0"));
        var store = new StateStore(paths.DatabasePath);
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new FileLoggerProvider(paths.LogsDirectory));
        if (console)
        {
            builder.Logging.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss ";
            });
        }
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(paths);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(http);
        builder.Services.AddSingleton(new ReplayParser(timeout: settings.ParseTimeout));
        builder.Services.AddSingleton(new IdentityApiClient(http, settings));
        builder.Services.AddSingleton(new GitHubUpdateChecker(http, settings));
        builder.Services.AddHostedService<Worker>();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        using var host = builder.Build();
        var logger = host
            .Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ReplayAssistant");
        HostLog.Starting(logger, version);
        HostLog.ApiReady(logger, settings.CanPost, settings.ApiBaseUri?.Host ?? "");
        host.Run();
        return 0;
    }

    private static bool HasFlag(string[] args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
}
