namespace StudyTracker.Infrastructure.Folders;

// Raises Changed once per burst of file system events. Google Drive's virtual drive can drop events,
// so callers should also poll; this only makes most changes show up right away.
public sealed class StudyFolderWatcher : IDisposable
{
    private readonly StudyFolder _folder;
    private readonly TimeSpan _debounce;
    private readonly Lock _lock = new();
    private FileSystemWatcher? _watcher;
    private Timer? _timer;

    public StudyFolderWatcher(StudyFolder folder, TimeSpan debounce)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentOutOfRangeException.ThrowIfLessThan(debounce, TimeSpan.Zero);
        _folder = folder;
        _debounce = debounce;
    }

    public event Action? Changed;

    public event Action<Exception>? Failed;

    public bool IsRunning => _watcher is not null;

    public bool Start()
    {
        lock (_lock)
        {
            if (_watcher is not null) return true;
            if (!_folder.IsAvailable) return false;

            try
            {
                var watcher = new FileSystemWatcher(_folder.RootPath)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    InternalBufferSize = 64 * 1024
                };
                watcher.Created += OnEvent;
                watcher.Deleted += OnEvent;
                watcher.Changed += OnEvent;
                watcher.Renamed += OnEvent;
                watcher.Error += OnError;
                watcher.EnableRaisingEvents = true;
                _watcher = watcher;
                _timer = new Timer(_ => RaiseChanged());
                return true;
            }
            catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
            {
                Failed?.Invoke(ex);
                return false;
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _watcher?.Dispose();
            _watcher = null;
            _timer?.Dispose();
            _timer = null;
        }
    }

    public void Dispose() => Stop();

    private void RaiseChanged()
    {
        try
        {
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Failed?.Invoke(ex);
        }
    }

    private void OnEvent(object sender, FileSystemEventArgs e)
    {
        lock (_lock) _timer?.Change(_debounce, Timeout.InfiniteTimeSpan);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Failed?.Invoke(e.GetException());
        Stop();
    }
}
