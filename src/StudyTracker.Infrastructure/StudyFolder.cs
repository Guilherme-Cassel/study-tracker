namespace StudyTracker.Infrastructure;

public sealed class StudyFolder
{
    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    public StudyFolder(string? rootPath, string? deliverablePrefix = null)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException(
                "The study folder is not configured. Set StudyFolder:RootPath in appsettings.Local.json.", nameof(rootPath));
        if (!Path.IsPathFullyQualified(rootPath))
            throw new ArgumentException($"The study folder path must be absolute: '{rootPath}'.", nameof(rootPath));

        RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        DeliverablePrefix = string.IsNullOrWhiteSpace(deliverablePrefix) ? null : deliverablePrefix.Trim();
    }

    public string RootPath { get; }

    // File name prefix of submitted work, e.g. "guilherme_cassel" in "guilherme_cassel_DR3_TP1.pdf".
    public string? DeliverablePrefix { get; }

    public bool IsAvailable => Directory.Exists(RootPath);

    // Builds a path from folder names taken from the data file, refusing anything that would leave the study folder.
    public string Resolve(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment) || segment is "." or ".." || segment.IndexOfAny(InvalidNameChars) >= 0)
                throw new ArgumentException($"Invalid folder or file name: '{segment}'.", nameof(segments));
        }

        var full = Path.GetFullPath(Path.Combine([RootPath, .. segments]));
        if (!Contains(full))
            throw new ArgumentException($"The path leaves the study folder: '{full}'.", nameof(segments));
        return full;
    }

    public bool Contains(string fullPath) =>
        string.Equals(fullPath, RootPath, StringComparison.OrdinalIgnoreCase) ||
        fullPath.StartsWith(RootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
