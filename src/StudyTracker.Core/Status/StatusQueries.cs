namespace StudyTracker.Core.Status;

public sealed record AssignmentItem(Block Block, CourseGroup Group, Course Course, Assignment Assignment);

public sealed record CourseRef(CourseGroup Group, Course Course);

public sealed record CourseGap(Course Course, IReadOnlyList<Assignment> Assignments);

public sealed record Progress(int Submitted, int Total)
{
    public double Ratio => Total == 0 ? 0 : (double)Submitted / Total;
}

public static class StatusQueries
{
    // Lower comes first in "what to do now".
    public static int Priority(this AssignmentStatus status) => status switch
    {
        AssignmentStatus.ReadyToSubmit => 1,
        AssignmentStatus.InProgress => 2,
        AssignmentStatus.Ready => 3,
        AssignmentStatus.MissingContext => 4,
        AssignmentStatus.Submitted => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static int DaysUntil(this DateOnly date, DateOnly today) => date.DayNumber - today.DayNumber;

    public static IEnumerable<AssignmentItem> Items(this Block block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return (block.Groups ?? [])
            .SelectMany(g => g.Courses.SelectMany(c => c.Assignments.Select(a => new AssignmentItem(block, g, c, a))));
    }

    public static IReadOnlyDictionary<AssignmentStatus, int> CountByStatus(this Block block)
    {
        var counts = Enum.GetValues<AssignmentStatus>().ToDictionary(s => s, _ => 0);
        foreach (var item in block.Items()) counts[item.Assignment.Status]++;
        return counts;
    }

    public static IReadOnlyList<AssignmentItem> NextSteps(this Block block) =>
        block.Items()
            .Where(i => i.Assignment.Status is not (AssignmentStatus.Submitted or AssignmentStatus.MissingContext))
            .OrderBy(i => i.Assignment.Status.Priority())
            .ThenBy(i => i.Assignment.DueOn ?? DateOnly.MaxValue)
            .ToList();

    public static IReadOnlyList<AssignmentItem> UpcomingDeadlines(this Block block) =>
        block.Items()
            .Where(i => i.Assignment.Status != AssignmentStatus.Submitted && i.Assignment.DueOn is not null)
            .OrderBy(i => i.Assignment.DueOn)
            .ToList();

    public static IReadOnlyList<CourseGap> MissingContext(this Block block) =>
        (block.Groups ?? [])
            .SelectMany(g => g.Courses)
            .Select(c => new CourseGap(c, c.Assignments.Where(a => a.Status == AssignmentStatus.MissingContext).ToList()))
            .Where(gap => gap.Assignments.Count > 0)
            .ToList();

    public static Progress Progress(this Course course)
    {
        ArgumentNullException.ThrowIfNull(course);
        return new Progress(course.Assignments.Count(a => a.Status == AssignmentStatus.Submitted), course.Assignments.Count);
    }

    // A completed block only lists submitted work.
    public static Progress Progress(this Block block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (block.CompletedCourses is { } completed)
        {
            var total = completed.Sum(c => c.AssignmentIds.Count);
            return new Progress(total, total);
        }
        var items = block.Items().ToList();
        return new Progress(items.Count(i => i.Assignment.Status == AssignmentStatus.Submitted), items.Count);
    }

    public static Progress Progress(this StatusData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var parts = data.Blocks.Select(b => b.Progress()).ToList();
        return new Progress(parts.Sum(p => p.Submitted), parts.Sum(p => p.Total));
    }

    public static IEnumerable<CourseRef> Courses(this Block block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return (block.Groups ?? []).SelectMany(g => g.Courses.Select(c => new CourseRef(g, c)));
    }

    public static CourseRef? FindCourse(this Block block, string code) =>
        block.Courses().FirstOrDefault(r => string.Equals(r.Course.Code, code, StringComparison.OrdinalIgnoreCase));
}
