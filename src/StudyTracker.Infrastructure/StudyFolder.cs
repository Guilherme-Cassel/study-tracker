namespace StudyTracker.Infrastructure;

public sealed class StudyFolder
{
    public StudyFolder(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException(
                "The study folder is not configured. Set StudyFolder:RootPath in appsettings.Local.json.", nameof(rootPath));
        if (!Path.IsPathFullyQualified(rootPath))
            throw new ArgumentException($"The study folder path must be absolute: '{rootPath}'.", nameof(rootPath));

        RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
    }

    public string RootPath { get; }

    public bool IsAvailable => Directory.Exists(RootPath);
}
