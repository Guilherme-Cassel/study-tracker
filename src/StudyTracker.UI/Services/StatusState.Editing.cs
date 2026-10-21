using StudyTracker.Core.Editing;
using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.UI.Services;

public enum SaveStatus
{
    Saved,
    NothingToDo,
    Conflict,
    NotFound,
    Failed
}

public sealed record SaveOutcome(SaveStatus Status, IReadOnlyList<EditField>? ChangedElsewhere = null, AssignmentEdit? Current = null)
{
    public static readonly SaveOutcome Saved = new(SaveStatus.Saved);
    public static readonly SaveOutcome NothingToDo = new(SaveStatus.NothingToDo);
    public static readonly SaveOutcome NotFound = new(SaveStatus.NotFound);
    public static readonly SaveOutcome Failed = new(SaveStatus.Failed);
}

public sealed partial class StatusState
{
    // Applies the edit to the latest file on disk. It only refuses when this same assignment changed in the
    // meantime (fields of "original" differ from the file); edits elsewhere in the file are kept.
    public Task<SaveOutcome> SaveEditAsync(string courseCode, string assignmentId, AssignmentEdit original, AssignmentEdit edit,
        string historyText, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(edit);
        ArgumentException.ThrowIfNullOrWhiteSpace(historyText);

        return WriteAsync(rescan: false, (data, _, item) =>
        {
            var current = AssignmentEdit.From(item.Assignment);
            var changed = current.ChangedFields(original);
            if (changed.Count > 0)
                return (new SaveOutcome(SaveStatus.Conflict, changed, current), null);

            edit.ApplyTo(item.Assignment);
            return (null, historyText);
        }, courseCode, assignmentId, cancellationToken);
    }

    // Recomputes the suggestion from a fresh scan, so what gets applied is what is on disk now.
    public Task<SaveOutcome> ApplyFolderSuggestionAsync(string courseCode, string assignmentId, CancellationToken cancellationToken = default) =>
        WriteAsync(rescan: true, (_, scan, item) =>
        {
            var assignmentFolder = scan.For(item.Course, item.Assignment);
            var files = FolderRules.Detect(item.Course.Code, item.Assignment.Id, assignmentFolder.Entries, folder.DeliverablePrefix);
            if (FolderRules.Suggest(item.Assignment, assignmentFolder, files) is not { } suggestion)
                return (SaveOutcome.NothingToDo, null);

            suggestion.ApplyTo(item.Assignment);
            return (null, $"{item.Course.Code} {item.Assignment.Id}: painel acertado pela pasta ({Display.Describe(suggestion)}).");
        }, courseCode, assignmentId, cancellationToken);

    private async Task<SaveOutcome> WriteAsync(
        bool rescan,
        Func<StatusData, FolderScan, AssignmentItem, (SaveOutcome? Early, string? History)> change,
        string courseCode, string assignmentId, CancellationToken cancellationToken)
    {
        var changed = false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!folder.IsAvailable) return SaveOutcome.Failed;

            var fresh = await store.LoadAsync(cancellationToken);
            if (fresh.Version != _snapshot?.Version)
            {
                _snapshot = fresh;
                changed = true;
            }

            var data = fresh.Data;
            if (data.Blocks.Count(b => b.IsCurrent) != 1) return SaveOutcome.Failed;
            var block = data.CurrentBlock;
            if (block.FindAssignment(courseCode, assignmentId) is not { } item) return SaveOutcome.NotFound;

            var scan = rescan || _current?.Folders is null || changed
                ? await scanner.ScanAsync(block, cancellationToken)
                : _current.Folders;

            var (early, history) = change(data, scan, item);
            if (early is not null)
            {
                _current = new StatusLoadResult(data, scan, folder.DeliverablePrefix, fresh.Version);
                return early;
            }

            var today = time.Today();
            data.UpdatedOn = today;
            data.AddHistory(today, history!);

            _snapshot = await store.SaveAsync(fresh, data, cancellationToken);
            _current = new StatusLoadResult(_snapshot.Data, scan, folder.DeliverablePrefix, _snapshot.Version);
            changed = true;
            return SaveOutcome.Saved;
        }
        catch (StatusDataConflictException)
        {
            return new SaveOutcome(SaveStatus.Conflict, []);
        }
        catch (Exception ex) when (ex is IOException or StatusDataFormatException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not save {File}", store.FilePath);
            return SaveOutcome.Failed;
        }
        finally
        {
            _gate.Release();
            if (changed) Changed?.Invoke();
        }
    }
}
