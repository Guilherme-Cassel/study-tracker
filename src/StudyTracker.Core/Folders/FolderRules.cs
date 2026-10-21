using StudyTracker.Core.Status;

namespace StudyTracker.Core.Folders;

public sealed record DetectedFiles(bool Brief, bool Rubrics, bool Guide, bool Artifacts, IReadOnlyList<string> MisnamedDeliverables)
{
    public bool Has(Stage stage) => stage switch
    {
        Stage.Brief => Brief,
        Stage.Rubrics => Rubrics,
        Stage.Guide => Guide,
        Stage.Artifacts => Artifacts,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "This stage cannot be detected from file names.")
    };
}

public sealed record FolderSuggestion(bool FolderNowExists, IReadOnlyList<Stage> Mark, IReadOnlyList<Stage> Unmark, bool MoveToReady)
{
    public void ApplyTo(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        if (FolderNowExists) assignment.FolderMissing = false;
        if (Mark.Count > 0 || Unmark.Count > 0)
        {
            assignment.Stages ??= new Stages();
            foreach (var stage in Mark) assignment.Stages.Set(stage, true);
            foreach (var stage in Unmark) assignment.Stages.Set(stage, false);
        }
        if (MoveToReady)
        {
            assignment.Status = AssignmentStatus.Ready;
            if (FolderRules.IsAskForContextStep(assignment.NextStep)) assignment.NextStep = null;
        }
    }
}

// File naming rules from the study guide (section 3.2): "Enunciado TP2.docx", "Rubricas TP2.txt",
// "Guia_TP2.md" and deliverables named "<prefix>_<COURSE>_<TP2>.<ext>".
public static class FolderRules
{
    public static readonly Stage[] DetectableStages = [Stage.Brief, Stage.Rubrics, Stage.Guide, Stage.Artifacts];

    private const string AskForContextPrefix = "Mandar enunciado";

    public static bool IsAskForContextStep(string? nextStep) =>
        nextStep?.StartsWith(AskForContextPrefix, StringComparison.OrdinalIgnoreCase) == true;

    public static string DeliverableBaseName(string deliverablePrefix, string courseCode, string assignmentId) =>
        $"{deliverablePrefix}_{courseCode}_{assignmentId}";

    public static DetectedFiles Detect(string courseCode, string assignmentId, IEnumerable<FolderEntry> entries, string? deliverablePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(courseCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(assignmentId);
        ArgumentNullException.ThrowIfNull(entries);

        var files = entries.Where(e => !e.IsDirectory).Select(e => e.Name).ToList();
        bool Any(Func<string, bool> match) => files.Any(match);
        bool StartsWith(string name, string start) => name.StartsWith(start, StringComparison.OrdinalIgnoreCase);
        bool Is(string name, string expected) => string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);

        var hasPrefix = !string.IsNullOrWhiteSpace(deliverablePrefix);
        var deliverable = hasPrefix
            ? DeliverableBaseName(deliverablePrefix!, courseCode, assignmentId) + "."
            : $"_{courseCode}_{assignmentId}.";

        return new DetectedFiles(
            Brief: Any(n => StartsWith(n, $"Enunciado {assignmentId}.")),
            Rubrics: Any(n => Is(n, $"Rubricas {assignmentId}.txt")),
            Guide: Any(n => Is(n, $"Guia_{assignmentId}.md")),
            Artifacts: hasPrefix
                ? Any(n => StartsWith(n, deliverable))
                : Any(n => n.Contains(deliverable, StringComparison.OrdinalIgnoreCase)),
            MisnamedDeliverables: hasPrefix
                ? files.Where(n => StartsWith(n, deliverablePrefix!) && !StartsWith(n, deliverable)).ToList()
                : []);
    }

    // What the data file should change to match the folder. Never applied without the user asking.
    public static FolderSuggestion? Suggest(Assignment assignment, AssignmentFolder folder, DetectedFiles files)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(files);

        if (!folder.Exists) return null;

        var folderNowExists = assignment.FolderMissing;
        List<Stage> mark = [], unmark = [];
        var moveToReady = false;

        if (assignment.Status != AssignmentStatus.Submitted)
        {
            var stages = assignment.Stages;
            foreach (var stage in DetectableStages)
            {
                var recorded = stages?.Get(stage) ?? false;
                if (files.Has(stage) && !recorded) mark.Add(stage);
                if (!files.Has(stage) && recorded) unmark.Add(stage);
            }
            moveToReady = assignment.Status == AssignmentStatus.MissingContext && files.Brief && files.Rubrics;
        }

        return folderNowExists || mark.Count > 0 || unmark.Count > 0 || moveToReady
            ? new FolderSuggestion(folderNowExists, mark, unmark, moveToReady)
            : null;
    }
}
