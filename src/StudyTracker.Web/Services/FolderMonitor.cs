using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Folders;

namespace StudyTracker.Web.Services;

// Refreshes StatusState when files change: right away through the file watcher, and by polling,
// because Google Drive's virtual drive does not always report changes.
public sealed class FolderMonitor(StudyFolder folder, StatusState state, IConfiguration configuration, ILogger<FolderMonitor> logger)
    : BackgroundService
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(700);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = configuration.GetValue("StudyFolder:PollSeconds", 30);
        if (seconds < 5)
            throw new InvalidOperationException("StudyFolder:PollSeconds must be at least 5.");

        using var watcher = new StudyFolderWatcher(folder, Debounce);
        watcher.Changed += () => _ = RefreshAsync(stoppingToken);
        watcher.Failed += ex => logger.LogWarning(ex, "Folder watcher stopped, falling back to polling");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        try
        {
            do
            {
                if (!watcher.IsRunning && watcher.Start())
                    logger.LogInformation("Watching {Folder}", folder.RootPath);
                await RefreshAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await state.RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not refresh the study folder");
        }
    }
}
