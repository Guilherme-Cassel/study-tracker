using StudyTracker.Core.Editing;
using StudyTracker.Core.Status;

namespace StudyTracker.Core.Tests;

public class AssignmentEditTests
{
    private static readonly DateOnly Today = new(2026, 10, 20);

    private static Assignment Open() => new()
    {
        Id = "TP1", Status = AssignmentStatus.InProgress, DueOn = new DateOnly(2026, 10, 26),
        Stages = new Stages { Brief = true, Rubrics = true }, NextStep = "Escrever."
    };

    [Fact]
    public void FromAndApplyTo_RoundTrip()
    {
        var source = Open();
        var target = new Assignment { Id = "TP1", Status = AssignmentStatus.MissingContext };

        AssignmentEdit.From(source).ApplyTo(target);

        Assert.Equal(AssignmentEdit.From(source), AssignmentEdit.From(target));
    }

    [Fact]
    public void ApplyTo_TrimsTextAndTurnsBlankIntoNull()
    {
        var work = Open();

        (AssignmentEdit.From(work) with { NextStep = "  Postar.  ", Notes = "   " }).ApplyTo(work);

        Assert.Equal("Postar.", work.NextStep);
        Assert.Null(work.Notes);
    }

    [Fact]
    public void ApplyTo_DoesNotCreateStagesWhenNoneAreDone()
    {
        var work = new Assignment { Id = "TP1", Status = AssignmentStatus.Submitted };

        (AssignmentEdit.From(work) with { Feedback = "Ok." }).ApplyTo(work);

        Assert.Null(work.Stages);
    }

    [Fact]
    public void ChangedFields_ListsOnlyRealChanges()
    {
        var original = AssignmentEdit.From(Open());
        var edit = original with { Guide = true, NextStep = "Escrever.  ", Feedback = "", DueOn = null };

        Assert.Equal([EditField.DueOn, EditField.Stages], edit.ChangedFields(original));
    }

    [Fact]
    public void Validate_RequiresDateWhenMarkingAsSubmitted()
    {
        var original = AssignmentEdit.From(Open());

        var errors = (original with { Status = AssignmentStatus.Submitted }).Validate(original, Today);

        Assert.Equal([new EditError(EditField.SubmittedOn, EditProblem.SubmittedOnRequired)], errors);
    }

    [Fact]
    public void Validate_AcceptsOldSubmissionWithoutDate()
    {
        var original = AssignmentEdit.From(new Assignment { Id = "AT", Status = AssignmentStatus.Submitted });

        Assert.Empty((original with { Feedback = "9 corretos." }).Validate(original, Today));
    }

    [Fact]
    public void Validate_RejectsSubmissionDateOnOpenWorkAndInTheFuture()
    {
        var original = AssignmentEdit.From(Open());

        Assert.Equal([new EditError(EditField.SubmittedOn, EditProblem.SubmittedOnWithoutSubmission)],
            (original with { SubmittedOn = Today }).Validate(original, Today));
        Assert.Contains(new EditError(EditField.SubmittedOn, EditProblem.SubmittedOnInFuture),
            (original with { Status = AssignmentStatus.Submitted, SubmittedOn = Today.AddDays(1) }).Validate(original, Today));
        Assert.Empty((original with { Status = AssignmentStatus.Submitted, SubmittedOn = Today }).Validate(original, Today));
    }

    [Fact]
    public void Validate_LimitsTextLength()
    {
        var original = AssignmentEdit.From(Open());
        var edit = original with { Notes = new string('x', AssignmentEdit.MaxTextLength + 1) };

        Assert.Equal([new EditError(EditField.Notes, EditProblem.TooLong)], edit.Validate(original, Today));
    }
}
