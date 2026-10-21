using System.Diagnostics;

namespace StudyTracker.Infrastructure.Folders;

// Opens folders of the study folder in Windows Explorer. Only folders: a file would be opened by its default app.
public sealed class ExplorerLauncher
{
    private readonly StudyFolder _folder;
    private readonly Action<string> _start;

    public ExplorerLauncher(StudyFolder folder) : this(folder, StartExplorer) { }

    internal ExplorerLauncher(StudyFolder folder, Action<string> start)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(start);
        _folder = folder;
        _start = start;
    }

    public void OpenFolder(params string[] segments)
    {
        var path = _folder.Resolve(segments);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Folder not found: '{path}'.");
        _start(path);
    }

    private static void StartExplorer(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Opening folders is only supported on Windows.");

        var info = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"))
        {
            UseShellExecute = false
        };
        info.ArgumentList.Add(path);
        Process.Start(info)?.Dispose();
    }
}
