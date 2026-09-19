using System.Runtime.InteropServices;

namespace ReplayAssistant;

/// <summary>
/// WinExe has no console by default (no flash on Startup). Opt in with --console.
/// </summary>
internal static class NativeConsole
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    public static void Attach()
    {
        if (GetConsoleWindow() == IntPtr.Zero)
        {
            if (!AttachConsole(AttachParentProcess))
            {
                AllocConsole();
            }
        }

        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
    }
}
