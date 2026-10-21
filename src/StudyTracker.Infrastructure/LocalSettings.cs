using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudyTracker.Core.Folders;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.Infrastructure;

public enum StudyFolderProblem
{
    Missing,
    NotAbsolute,
    NotFound,
    NoDataFile
}

public enum PrefixProblem
{
    Invalid
}

// The machine settings file (appsettings.Local.json): where the study folder is and the deliverable prefix.
public static class LocalSettings
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static StudyFolderProblem? CheckStudyFolder(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) return StudyFolderProblem.Missing;
        if (!Path.IsPathFullyQualified(rootPath)) return StudyFolderProblem.NotAbsolute;
        if (!Directory.Exists(rootPath)) return StudyFolderProblem.NotFound;
        if (!File.Exists(Path.Combine(rootPath, StatusDataStore.FileName))) return StudyFolderProblem.NoDataFile;
        return null;
    }

    public static PrefixProblem? CheckPrefix(string? prefix) =>
        string.IsNullOrWhiteSpace(prefix) || (FileNaming.IsValid(prefix.Trim()) && !prefix.Trim().Contains(' '))
            ? null
            : PrefixProblem.Invalid;

    // Keeps any other setting already in the file; a blank prefix removes it.
    public static void Save(string settingsPath, string rootPath, string? deliverablePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        if (CheckStudyFolder(rootPath) is { } folderProblem)
            throw new ArgumentException($"Invalid study folder ({folderProblem}): '{rootPath}'.", nameof(rootPath));
        if (CheckPrefix(deliverablePrefix) is not null)
            throw new ArgumentException($"Invalid deliverable prefix: '{deliverablePrefix}'.", nameof(deliverablePrefix));

        var root = ReadObject(settingsPath);
        if (root["StudyFolder"] is not JsonObject studyFolder)
        {
            studyFolder = new JsonObject();
            root["StudyFolder"] = studyFolder;
        }

        studyFolder["RootPath"] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        if (string.IsNullOrWhiteSpace(deliverablePrefix)) studyFolder.Remove("DeliverablePrefix");
        else studyFolder["DeliverablePrefix"] = deliverablePrefix.Trim();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settingsPath))!);
        File.WriteAllText(settingsPath, root.ToJsonString(WriteOptions) + Environment.NewLine);
    }

    private static JsonObject ReadObject(string path)
    {
        if (!File.Exists(path)) return [];
        try
        {
            return JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            }) as JsonObject ?? [];
        }
        catch (JsonException)
        {
            // A broken file cannot be kept anyway; it is replaced by the new settings.
            return [];
        }
    }
}
