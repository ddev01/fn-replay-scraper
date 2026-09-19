using System.Diagnostics;

namespace ReplayAssistant.Core;

public static class FortniteProcessGate
{
    public const string ProcessName = "FortniteClient-Win64-Shipping";

    public static bool IsFortniteRunning()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
