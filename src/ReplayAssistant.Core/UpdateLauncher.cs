using System.Diagnostics;

namespace ReplayAssistant.Core;

public static class UpdateLauncher
{
    public static bool TryStartUpdater(string updaterExe, string downloadedExe, string runningExe)
    {
        if (!File.Exists(updaterExe) || !File.Exists(downloadedExe) || !File.Exists(runningExe))
        {
            return false;
        }

        var pid = Environment.ProcessId;
        var args =
            $"--pid {pid} --from \"{downloadedExe}\" --to \"{runningExe}\" --launch \"{runningExe}\"";
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = updaterExe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        );
        return process is not null;
    }
}
