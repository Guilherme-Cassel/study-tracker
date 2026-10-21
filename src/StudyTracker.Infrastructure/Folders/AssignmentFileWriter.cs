using System.Text;
using StudyTracker.Core.Folders;

namespace StudyTracker.Infrastructure.Folders;

public sealed class TargetExistsException(string path) : IOException($"File already exists: '{path}'.")
{
    public string Path { get; } = path;
}

// Saves and reads files in a course or assignment folder. It never creates folders: the block, course and
// assignment folders are created by the assistant following the study guide, so a missing one is reported.
public sealed class AssignmentFileWriter(StudyFolder folder)
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public bool Exists(IReadOnlyList<string> target, string fileName) =>
        File.Exists(PathFor(target, fileName));

    public bool FolderExists(IReadOnlyList<string> target)
    {
        try
        {
            return Directory.Exists(folder.Resolve([.. target]));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public string FolderPath(IReadOnlyList<string> target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return folder.Resolve([.. target]);
    }

    public async Task<string> SaveAsync(IReadOnlyList<string> target, string fileName, Stream content, bool overwrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var destination = PathFor(target, fileName);
        var directory = Path.GetDirectoryName(destination)!;
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Folder not found: '{directory}'.");

        var temp = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                File.SetAttributes(temp, FileAttributes.Hidden);
                await content.CopyToAsync(output, cancellationToken);
            }

            try
            {
                File.Move(temp, destination, overwrite);
            }
            catch (IOException) when (!overwrite && File.Exists(destination))
            {
                throw new TargetExistsException(destination);
            }
            // the move keeps the temp file's hidden flag
            File.SetAttributes(destination, File.GetAttributes(destination) & ~FileAttributes.Hidden);
            return destination;
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public Task<string> SaveTextAsync(IReadOnlyList<string> target, string fileName, string text, bool overwrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var normalized = text.Replace("\r\n", "\n").TrimEnd() + "\n";
        return SaveAsync(target, fileName, new MemoryStream(Utf8NoBom.GetBytes(normalized)), overwrite, cancellationToken);
    }

    // Reads a text file (brief, rubrics) to show it on screen; null when it is missing or larger than maxBytes.
    public async Task<string?> ReadTextAsync(IReadOnlyList<string> target, string fileName, int maxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        var path = PathFor(target, fileName);
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > maxBytes) return null;
            return await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string PathFor(IReadOnlyList<string> target, string fileName)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.Count == 0)
            throw new ArgumentException("Files are only saved inside course or assignment folders.", nameof(target));
        if (!FileNaming.IsValid(fileName))
            throw new ArgumentException($"Invalid file name: '{fileName}'.", nameof(fileName));
        return folder.Resolve([.. target, fileName]);
    }
}
