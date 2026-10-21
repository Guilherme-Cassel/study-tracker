using StudyTracker.Core.Status;
using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.Web.Services;

public enum StatusProblem
{
    FolderUnavailable,
    FileMissing,
    InvalidFile
}

public sealed record StatusLoadResult(StatusData? Data, StatusProblem? Problem = null, string? Detail = null);

// Keeps the last snapshot in memory and reads the file again only when it changed on disk.
public sealed class StatusState(StudyFolder folder, StatusDataStore store, ILogger<StatusState> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private StatusSnapshot? _snapshot;

    public string FolderPath => folder.RootPath;

    public async Task<StatusLoadResult> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!folder.IsAvailable)
                return new StatusLoadResult(null, StatusProblem.FolderUnavailable);

            if (_snapshot is null || await store.HasChangedAsync(_snapshot, cancellationToken))
                _snapshot = await store.LoadAsync(cancellationToken);

            return new StatusLoadResult(_snapshot.Data);
        }
        catch (FileNotFoundException)
        {
            _snapshot = null;
            return new StatusLoadResult(null, StatusProblem.FileMissing);
        }
        catch (StatusDataFormatException ex)
        {
            logger.LogWarning(ex, "Could not read {File}", store.FilePath);
            _snapshot = null;
            return new StatusLoadResult(null, StatusProblem.InvalidFile, ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }
}
