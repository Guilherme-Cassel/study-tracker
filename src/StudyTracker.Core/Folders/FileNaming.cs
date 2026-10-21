namespace StudyTracker.Core.Folders;

public enum FileKind
{
    Brief,
    Rubrics,
    KeepName
}

// Names for files added to an assignment folder, following the study guide: "Enunciado TP2.docx", "Rubricas TP2.txt".
public static class FileNaming
{
    public const int MaxNameLength = 200;

    private static readonly char[] InvalidChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string RubricsFileName(string assignmentId) => $"Rubricas {assignmentId}.txt";

    // A brief pasted as text (the Moodle one is usually a .docx dropped on the assignment).
    public static string BriefTextFileName(string assignmentId) => $"Enunciado {assignmentId}.md";

    public static FileKind Guess(string originalName)
    {
        ArgumentNullException.ThrowIfNull(originalName);
        if (originalName.Contains("enunciado", StringComparison.OrdinalIgnoreCase)) return FileKind.Brief;
        if (originalName.Contains("rubrica", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetExtension(originalName), ".txt", StringComparison.OrdinalIgnoreCase))
            return FileKind.Rubrics;
        return FileKind.KeepName;
    }

    public static string NameFor(FileKind kind, string originalName, string assignmentId)
    {
        ArgumentNullException.ThrowIfNull(originalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(assignmentId);

        var extension = Path.GetExtension(originalName);
        return kind switch
        {
            FileKind.Brief => $"Enunciado {assignmentId}{extension}",
            FileKind.Rubrics => $"Rubricas {assignmentId}{(string.IsNullOrEmpty(extension) ? ".txt" : extension)}",
            FileKind.KeepName => originalName,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public static bool IsValid(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength) return false;
        if (name is "." or ".." || name.EndsWith('.') || name.EndsWith(' ') || name.StartsWith(' ')) return false;
        if (name.IndexOfAny(InvalidChars) >= 0 || name.Any(char.IsControl)) return false;
        return !ReservedNames.Contains(Path.GetFileNameWithoutExtension(name));
    }
}
