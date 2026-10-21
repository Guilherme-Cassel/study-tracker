using StudyTracker.Core.Status;

namespace StudyTracker.Core.Tests;

public class StatusQueriesTests
{
    private static Assignment Work(string id, AssignmentStatus status, DateOnly? due = null) =>
        new() { Id = id, Status = status, DueOn = due };

    private static Course NewCourse(string code, params Assignment[] work) =>
        new() { Code = code, Name = $"Disciplina {code}", Folder = code, Assignments = [.. work] };

    private static Block CurrentBlock() => new()
    {
        Name = "Atual", Code = "26E3-26E4", Folder = "Atual", IsCurrent = true,
        Groups =
        [
            new CourseGroup
            {
                Title = "26E4",
                Courses =
                [
                    NewCourse("DR3",
                        Work("TP1", AssignmentStatus.ReadyToSubmit, new DateOnly(2026, 10, 26)),
                        Work("TP2", AssignmentStatus.MissingContext),
                        Work("AT", AssignmentStatus.MissingContext)),
                    NewCourse("DR4",
                        Work("TP1", AssignmentStatus.Submitted, new DateOnly(2026, 10, 26)),
                        Work("TP2", AssignmentStatus.InProgress, new DateOnly(2026, 11, 9)),
                        Work("TP3", AssignmentStatus.Ready))
                ]
            },
            new CourseGroup { Title = "Projeto de Bloco", Courses = [NewCourse("PB", Work("TP4", AssignmentStatus.InProgress, new DateOnly(2026, 11, 2)))] },
            new CourseGroup { Title = "26E3", Courses = [NewCourse("DR1")] }
        ]
    };

    private static Block CompletedBlock() => new()
    {
        Name = "Antigo", Code = "25E3-25E4", Folder = "Antigo",
        CompletedCourses =
        [
            new CompletedCourse { Code = "DR1", Name = "A", Folder = "A", AssignmentIds = ["TP1", "TP2", "TP3", "AT"] },
            new CompletedCourse { Code = "DR2", Name = "B", Folder = "B", AssignmentIds = ["TP1", "AT"] }
        ]
    };

    private static string Label(AssignmentItem i) => $"{i.Course.Code} {i.Assignment.Id}";

    [Fact]
    public void Priority_PutsReadyToSubmitFirstAndSubmittedLast()
    {
        var ordered = Enum.GetValues<AssignmentStatus>().OrderBy(s => s.Priority()).ToList();

        Assert.Equal(
            [AssignmentStatus.ReadyToSubmit, AssignmentStatus.InProgress, AssignmentStatus.Ready, AssignmentStatus.MissingContext, AssignmentStatus.Submitted],
            ordered);
    }

    [Fact]
    public void CountByStatus_CountsEveryStatusIncludingZero()
    {
        var counts = CurrentBlock().CountByStatus();

        Assert.Equal(5, counts.Count);
        Assert.Equal(1, counts[AssignmentStatus.ReadyToSubmit]);
        Assert.Equal(2, counts[AssignmentStatus.InProgress]);
        Assert.Equal(1, counts[AssignmentStatus.Ready]);
        Assert.Equal(2, counts[AssignmentStatus.MissingContext]);
        Assert.Equal(1, counts[AssignmentStatus.Submitted]);
    }

    [Fact]
    public void NextSteps_OrdersByPriorityThenDeadline_AndSkipsSubmittedAndMissingContext()
    {
        var steps = CurrentBlock().NextSteps().Select(Label);

        Assert.Equal(["DR3 TP1", "PB TP4", "DR4 TP2", "DR4 TP3"], steps);
    }

    [Fact]
    public void UpcomingDeadlines_ListsOpenWorkWithDeadlineByDate()
    {
        var deadlines = CurrentBlock().UpcomingDeadlines().Select(Label);

        Assert.Equal(["DR3 TP1", "PB TP4", "DR4 TP2"], deadlines);
    }

    [Fact]
    public void MissingContext_GroupsByCourse()
    {
        var gaps = CurrentBlock().MissingContext();

        var gap = Assert.Single(gaps);
        Assert.Equal("DR3", gap.Course.Code);
        Assert.Equal(["TP2", "AT"], gap.Assignments.Select(a => a.Id));
    }

    [Fact]
    public void Progress_OfCourseCountsSubmitted()
    {
        var dr4 = CurrentBlock().FindCourse("dr4")!.Course;

        Assert.Equal(new Progress(1, 3), dr4.Progress());
    }

    [Fact]
    public void Progress_OfEmptyCourseIsZero()
    {
        var progress = CurrentBlock().FindCourse("DR1")!.Course.Progress();

        Assert.Equal(0, progress.Total);
        Assert.Equal(0, progress.Ratio);
    }

    [Fact]
    public void Progress_OfCompletedBlockCountsEverythingAsSubmitted()
    {
        Assert.Equal(new Progress(6, 6), CompletedBlock().Progress());
    }

    [Fact]
    public void Progress_OfWholeCourseAddsAllBlocks()
    {
        var data = new StatusData { Blocks = [CompletedBlock(), CurrentBlock()] };

        Assert.Equal(new Progress(7, 13), data.Progress());
    }

    [Fact]
    public void FindCourse_ReturnsNullForUnknownCode()
    {
        Assert.Null(CurrentBlock().FindCourse("XX9"));
    }

    [Theory]
    [InlineData(2026, 10, 26, 6)]
    [InlineData(2026, 10, 20, 0)]
    [InlineData(2026, 10, 18, -2)]
    public void DaysUntil_CountsCalendarDays(int y, int m, int d, int expected)
    {
        Assert.Equal(expected, new DateOnly(y, m, d).DaysUntil(new DateOnly(2026, 10, 20)));
    }
}
