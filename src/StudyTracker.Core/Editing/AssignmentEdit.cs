using StudyTracker.Core.Status;

namespace StudyTracker.Core.Editing;

public enum EditField
{
    Status,
    DueOn,
    SubmittedOn,
    Stages,
    NextStep,
    Feedback,
    Notes
}

public enum EditProblem
{
    SubmittedOnRequired,
    SubmittedOnWithoutSubmission,
    SubmittedOnInFuture,
    TooLong
}

public sealed record EditError(EditField Field, EditProblem Problem);

// The editable fields of an assignment, as plain values, so two versions can be compared field by field.
public sealed record AssignmentEdit(
    AssignmentStatus Status,
    DateOnly? DueOn,
    DateOnly? SubmittedOn,
    bool Brief,
    bool Rubrics,
    bool Definition,
    bool Guide,
    bool Artifacts,
    string? NextStep,
    string? Feedback,
    string? Notes)
{
    public const int MaxTextLength = 2000;

    public static AssignmentEdit From(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        var s = assignment.Stages;
        return new AssignmentEdit(
            assignment.Status, assignment.DueOn, assignment.SubmittedOn,
            s?.Brief ?? false, s?.Rubrics ?? false, s?.Definition ?? false, s?.Guide ?? false, s?.Artifacts ?? false,
            assignment.NextStep, assignment.Feedback, assignment.Notes);
    }

    public bool HasStage(Stage stage) => stage switch
    {
        Stage.Brief => Brief,
        Stage.Rubrics => Rubrics,
        Stage.Definition => Definition,
        Stage.Guide => Guide,
        Stage.Artifacts => Artifacts,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };

    public AssignmentEdit Normalized() => this with
    {
        NextStep = Clean(NextStep),
        Feedback = Clean(Feedback),
        Notes = Clean(Notes)
    };

    public IReadOnlyList<EditField> ChangedFields(AssignmentEdit other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var a = Normalized();
        var b = other.Normalized();
        var fields = new List<EditField>();
        if (a.Status != b.Status) fields.Add(EditField.Status);
        if (a.DueOn != b.DueOn) fields.Add(EditField.DueOn);
        if (a.SubmittedOn != b.SubmittedOn) fields.Add(EditField.SubmittedOn);
        if (Enum.GetValues<Stage>().Any(s => a.HasStage(s) != b.HasStage(s))) fields.Add(EditField.Stages);
        if (a.NextStep != b.NextStep) fields.Add(EditField.NextStep);
        if (a.Feedback != b.Feedback) fields.Add(EditField.Feedback);
        if (a.Notes != b.Notes) fields.Add(EditField.Notes);
        return fields;
    }

    // "original" is the assignment as it was before editing: an old submission without a date stays valid.
    public IReadOnlyList<EditError> Validate(AssignmentEdit original, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(original);
        var errors = new List<EditError>();
        var submitted = Status == AssignmentStatus.Submitted;

        if (submitted && SubmittedOn is null && original.Status != AssignmentStatus.Submitted)
            errors.Add(new EditError(EditField.SubmittedOn, EditProblem.SubmittedOnRequired));
        if (!submitted && SubmittedOn is not null)
            errors.Add(new EditError(EditField.SubmittedOn, EditProblem.SubmittedOnWithoutSubmission));
        if (SubmittedOn > today)
            errors.Add(new EditError(EditField.SubmittedOn, EditProblem.SubmittedOnInFuture));

        foreach (var (field, text) in new[] { (EditField.NextStep, NextStep), (EditField.Feedback, Feedback), (EditField.Notes, Notes) })
        {
            if (text?.Length > MaxTextLength) errors.Add(new EditError(field, EditProblem.TooLong));
        }
        return errors;
    }

    public void ApplyTo(Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        var edit = Normalized();

        assignment.Status = edit.Status;
        assignment.DueOn = edit.DueOn;
        assignment.SubmittedOn = edit.SubmittedOn;
        assignment.NextStep = edit.NextStep;
        assignment.Feedback = edit.Feedback;
        assignment.Notes = edit.Notes;

        var anyStage = Enum.GetValues<Stage>().Any(edit.HasStage);
        if (assignment.Stages is null && !anyStage) return;
        assignment.Stages ??= new Stages();
        foreach (var stage in Enum.GetValues<Stage>()) assignment.Stages.Set(stage, edit.HasStage(stage));
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
