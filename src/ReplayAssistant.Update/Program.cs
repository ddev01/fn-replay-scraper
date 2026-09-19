using System.Diagnostics;

namespace ReplayAssistant.Update;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var pid = GetInt(args, "--pid");
        var from = GetArg(args, "--from");
        var to = GetArg(args, "--to");
        var launch = GetArg(args, "--launch") ?? to;
        if (pid <= 0 || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return 1;
        }

        try
        {
            if (pid > 0)
            {
                try
                {
                    using var parent = Process.GetProcessById(pid);
                    parent.WaitForExit(60_000);
                }
                catch (ArgumentException)
                {
                    // Already exited.
                }
            }

            for (var i = 0; i < 20; i++)
            {
                try
                {
                    File.Copy(from, to, overwrite: true);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(250);
                    if (i == 19)
                    {
                        throw;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(launch) && File.Exists(launch))
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = launch,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(launch),
                    }
                );
            }

            return 0;
        }
        catch (IOException)
        {
            return 2;
        }
        catch (UnauthorizedAccessException)
        {
            return 2;
        }
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static int GetInt(string[] args, string name)
    {
        var text = GetArg(args, name);
        return int.TryParse(text, out var value) ? value : 0;
    }
}
