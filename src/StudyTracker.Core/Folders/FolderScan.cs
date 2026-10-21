using StudyTracker.Core.Status;

namespace StudyTracker.Core.Folders;

public sealed record FolderEntry(string Name, bool IsDirectory);

public sealed record AssignmentFolder(bool Exists, IReadOnlyList<FolderEntry> Entries)
{
    public static readonly AssignmentFolder Missing = new(false, []);
}

// What is on disk for the assignments of one block, as read at a given moment.
public sealed class FolderScan
{
    private readonly Dictionary<string, AssignmentFolder> _assignments;
    private readonly HashSet<string> _courses;

    public FolderScan(IReadOnlyDictionary<(string CourseCode, string AssignmentId), AssignmentFolder> assignments,
        IEnumerable<string> existingCourseCodes)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(existingCourseCodes);

        _assignments = assignments.ToDictionary(p => Key(p.Key.CourseCode, p.Key.AssignmentId), p => p.Value, StringComparer.OrdinalIgnoreCase);
        _courses = new HashSet<string>(existingCourseCodes, StringComparer.OrdinalIgnoreCase);
        Signature = string.Join("\n", _assignments
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p.Key}:{p.Value.Exists}:{string.Join("|", p.Value.Entries.Select(e => e.Name + (e.IsDirectory ? "/" : "")))}"))
            + "\n" + string.Join(",", _courses.Order(StringComparer.OrdinalIgnoreCase));
    }

    public static FolderScan Empty { get; } = new(new Dictionary<(string, string), AssignmentFolder>(), []);

    // Changes whenever a folder appears, disappears or its list of entries changes.
    public string Signature { get; }

    public bool CourseExists(Course course) => _courses.Contains(course.Code);

    public AssignmentFolder For(Course course, Assignment assignment) =>
        _assignments.GetValueOrDefault(Key(course.Code, assignment.Id), AssignmentFolder.Missing);

    private static string Key(string courseCode, string assignmentId) => $"{courseCode}/{assignmentId}";
}
