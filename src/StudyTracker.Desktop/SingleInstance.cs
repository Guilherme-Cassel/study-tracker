using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StudyTracker.Desktop;

// Opening the app again brings the window that is already open to the front instead of starting a second copy.
internal static partial class SingleInstance
{
    private const int RestoreWindow = 9;

    public static void BringExistingToFront()
    {
        using var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
        {
            using (process)
            {
                if (process.Id == current.Id || process.MainWindowHandle == IntPtr.Zero) continue;
                if (IsIconic(process.MainWindowHandle)) ShowWindow(process.MainWindowHandle, RestoreWindow);
                SetForegroundWindow(process.MainWindowHandle);
                return;
            }
        }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr window, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr window);
}
