using StudyTracker.Core.Status;

namespace StudyTracker.Core.Folders;

public sealed record FolderEntry(string Name, bool IsDirectory);

public sealed record AssignmentFolder(bool Exists, IReadOnlyList<FolderEntry> Entries)
{
    public static readonly AssignmentFolder Missing = new(false, []);
}

// Identifies an assignment across blocks: the same course code (DR1) comes back in more than one block.
public sealed record AssignmentKey(string BlockCode, string CourseCode, string AssignmentId)
{
    public static AssignmentKey Of(Block block, Course course, Assignment assignment)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(course);
        ArgumentNullException.ThrowIfNull(assignment);
        return new AssignmentKey(block.Code, course.Code, assignment.Id);
    }

    public override string ToString() => $"{BlockCode}/{CourseCode}/{AssignmentId}";
}

public sealed record CourseKey(string BlockCode, string CourseCode)
{
    public override string ToString() => $"{BlockCode}/{CourseCode}";
}

// What is on disk for the assignments of the scanned blocks, as read at a given moment.
public sealed class FolderScan
{
    private readonly Dictionary<string, AssignmentFolder> _assignments;
    private readonly HashSet<string> _courses;

    public FolderScan(IReadOnlyDictionary<AssignmentKey, AssignmentFolder> assignments, IEnumerable<CourseKey> existingCourses)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(existingCourses);

        _assignments = assignments.ToDictionary(p => p.Key.ToString(), p => p.Value, StringComparer.OrdinalIgnoreCase);
        _courses = new HashSet<string>(existingCourses.Select(c => c.ToString()), StringComparer.OrdinalIgnoreCase);
        Signature = string.Join("\n", _assignments
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p.Key}:{p.Value.Exists}:{string.Join("|", p.Value.Entries.Select(e => e.Name + (e.IsDirectory ? "/" : "")))}"))
            + "\n" + string.Join(",", _courses.Order(StringComparer.OrdinalIgnoreCase));
    }

    public static FolderScan Empty { get; } = new(new Dictionary<AssignmentKey, AssignmentFolder>(), []);

    // Changes whenever a folder appears, disappears or its list of entries changes.
    public string Signature { get; }

    public bool CourseExists(Block block, Course course) => _courses.Contains(new CourseKey(block.Code, course.Code).ToString());

    public bool CourseExists(Block block, CompletedCourse course) => _courses.Contains(new CourseKey(block.Code, course.Code).ToString());

    public AssignmentFolder For(Block block, Course course, Assignment assignment) =>
        _assignments.GetValueOrDefault(AssignmentKey.Of(block, course, assignment).ToString(), AssignmentFolder.Missing);
}
