using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReplayAssistant;

internal static partial class MemoryPressure
{
    public static void ReleaseAfterParse()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        SetProcessWorkingSetSize(
            Process.GetCurrentProcess().Handle,
            nint.MinusOne,
            nint.MinusOne
        );
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessWorkingSetSize(
        nint process,
        nint minimumWorkingSetSize,
        nint maximumWorkingSetSize
    );
}
