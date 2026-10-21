using System.Globalization;
using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;

namespace StudyTracker.Infrastructure.Folders;

public sealed class FolderScanner(StudyFolder folder)
{
    private static readonly StringComparer NameOrder = StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), ignoreCase: true);

    public Task<FolderScan> ScanAsync(IEnumerable<Block> blocks, CancellationToken cancellationToken = default) =>
        Task.Run(() => Scan(blocks, cancellationToken), cancellationToken);

    public FolderScan Scan(Block block, CancellationToken cancellationToken = default) => Scan([block], cancellationToken);

    // Reads every assignment folder of the detailed blocks. For finished blocks only the course folders are checked.
    public FolderScan Scan(IEnumerable<Block> blocks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var assignments = new Dictionary<AssignmentKey, AssignmentFolder>();
        var courses = new List<CourseKey>();

        foreach (var block in blocks)
        {
            ArgumentNullException.ThrowIfNull(block);
            foreach (var (_, course) in block.Courses())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryResolve(out var coursePath, block.Folder, course.Folder) || !Directory.Exists(coursePath))
                    continue;
                courses.Add(new CourseKey(block.Code, course.Code));

                foreach (var assignment in course.Assignments)
                {
                    assignments[AssignmentKey.Of(block, course, assignment)] =
                        TryResolve(out var path, block.Folder, course.Folder, assignment.Id) ? Read(path) : AssignmentFolder.Missing;
                }
            }

            foreach (var course in block.CompletedCourses ?? [])
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryResolve(out var coursePath, block.Folder, course.Folder) && Directory.Exists(coursePath))
                    courses.Add(new CourseKey(block.Code, course.Code));
            }
        }

        return new FolderScan(assignments, courses);
    }

    private bool TryResolve(out string path, params string[] segments)
    {
        try
        {
            path = folder.Resolve(segments);
            return true;
        }
        catch (ArgumentException)
        {
            path = "";
            return false;
        }
    }

    private static AssignmentFolder Read(string path)
    {
        try
        {
            var info = new DirectoryInfo(path);
            if (!info.Exists) return AssignmentFolder.Missing;

            var entries = info.EnumerateFileSystemInfos()
                .Where(e => (e.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                .Select(e => new FolderEntry(e.Name, e is DirectoryInfo))
                .OrderByDescending(e => e.IsDirectory)
                .ThenBy(e => e.Name, NameOrder)
                .ToList();
            return new AssignmentFolder(true, entries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return AssignmentFolder.Missing;
        }
    }
}
