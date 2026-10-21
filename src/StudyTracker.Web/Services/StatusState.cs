using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;
using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Folders;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.Web.Services;

public enum StatusProblem
{
    FolderUnavailable,
    FileMissing,
    InvalidFile
}

public sealed record AssignmentFolderInfo(AssignmentFolder Folder, DetectedFiles Files, FolderSuggestion? Suggestion);

public sealed record StatusLoadResult(
    StatusData? Data,
    FolderScan Folders,
    string? DeliverablePrefix = null,
    string? Version = null,
    StatusProblem? Problem = null,
    string? Detail = null)
{
    public AssignmentFolderInfo FolderOf(Course course, Assignment assignment)
    {
        var folder = Folders.For(course, assignment);
        var files = FolderRules.Detect(course.Code, assignment.Id, folder.Entries, DeliverablePrefix);
        return new AssignmentFolderInfo(folder, files, FolderRules.Suggest(assignment, folder, files));
    }

    public bool SameAs(StatusLoadResult? other) =>
        other is not null && Problem == other.Problem && Version == other.Version && Folders.Signature == other.Folders.Signature;
}

// Keeps the last data file snapshot and folder scan in memory. Pages read from here; FolderMonitor
// and the browser focus hook call RefreshAsync, and Changed tells open pages to render again.
public sealed class StatusState(StudyFolder folder, StatusDataStore store, FolderScanner scanner, ILogger<StatusState> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private StatusSnapshot? _snapshot;
    private StatusLoadResult? _current;

    public event Action? Changed;

    public string FolderPath => folder.RootPath;

    public async Task<StatusLoadResult> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _current = await LoadAsync(rescan: false, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        StatusLoadResult? before;
        StatusLoadResult after;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            before = _current;
            after = _current = await LoadAsync(rescan: true, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        if (!after.SameAs(before)) Changed?.Invoke();
    }

    private async Task<StatusLoadResult> LoadAsync(bool rescan, CancellationToken cancellationToken)
    {
        if (!folder.IsAvailable)
            return Fail(StatusProblem.FolderUnavailable);

        try
        {
            var reloaded = false;
            if (_snapshot is null || await store.HasChangedAsync(_snapshot, cancellationToken))
            {
                _snapshot = await store.LoadAsync(cancellationToken);
                reloaded = true;
            }

            var data = _snapshot.Data;
            if (data.Blocks.Count(b => b.IsCurrent) != 1)
                return Fail(StatusProblem.InvalidFile, "Exactly one block must be marked as current (\"atual\": true).");

            var scan = !reloaded && !rescan && _current is { Data: not null } previous
                ? previous.Folders
                : await scanner.ScanAsync(data.CurrentBlock, cancellationToken);

            return new StatusLoadResult(data, scan, folder.DeliverablePrefix, _snapshot.Version);
        }
        catch (FileNotFoundException)
        {
            return Fail(StatusProblem.FileMissing);
        }
        catch (StatusDataFormatException ex)
        {
            logger.LogWarning(ex, "Could not read {File}", store.FilePath);
            return Fail(StatusProblem.InvalidFile, ex.Message);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Could not read the study folder");
            return Fail(StatusProblem.FolderUnavailable);
        }
    }

    private StatusLoadResult Fail(StatusProblem problem, string? detail = null)
    {
        _snapshot = null;
        return new StatusLoadResult(null, FolderScan.Empty, Problem: problem, Detail: detail);
    }
}
