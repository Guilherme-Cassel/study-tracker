using System.Text.Json.Nodes;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.Infrastructure.Tests;

public sealed class LocalSettingsTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("study-tracker-settings-").FullName;
    private readonly string _studyFolder;
    private readonly string _settingsPath;

    public LocalSettingsTests()
    {
        _studyFolder = Directory.CreateDirectory(Path.Combine(_root, "Meu Drive", "Faculdade")).FullName;
        File.WriteAllText(Path.Combine(_studyFolder, StatusDataStore.FileName), "window.DADOS = {};");
        _settingsPath = Path.Combine(_root, "config", "StudyTracker", "appsettings.Local.json");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private JsonObject Saved() => (JsonObject)JsonNode.Parse(File.ReadAllText(_settingsPath))!;

    [Fact]
    public void CheckStudyFolder_AcceptsFolderWithDataFile()
    {
        Assert.Null(LocalSettings.CheckStudyFolder(_studyFolder));
    }

    [Fact]
    public void CheckStudyFolder_ExplainsWhatIsWrong()
    {
        Assert.Equal(StudyFolderProblem.Missing, LocalSettings.CheckStudyFolder("  "));
        Assert.Equal(StudyFolderProblem.NotAbsolute, LocalSettings.CheckStudyFolder("Faculdade"));
        Assert.Equal(StudyFolderProblem.NotFound, LocalSettings.CheckStudyFolder(Path.Combine(_root, "nao-existe")));
        Assert.Equal(StudyFolderProblem.NoDataFile, LocalSettings.CheckStudyFolder(_root));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("guilherme_cassel", true)]
    [InlineData("nome com espaco", false)]
    [InlineData("a/b", false)]
    [InlineData("CON", false)]
    public void CheckPrefix_AcceptsBlankOrValidFileNamePart(string? prefix, bool valid)
    {
        Assert.Equal(valid, LocalSettings.CheckPrefix(prefix) is null);
    }

    [Fact]
    public void Save_CreatesTheFileAndItsFolder()
    {
        LocalSettings.Save(_settingsPath, _studyFolder + Path.DirectorySeparatorChar, " guilherme_cassel ");

        var folder = Saved()["StudyFolder"]!;
        Assert.Equal(_studyFolder, (string?)folder["RootPath"]);
        Assert.Equal("guilherme_cassel", (string?)folder["DeliverablePrefix"]);
    }

    [Fact]
    public void Save_KeepsOtherSettings_AndRemovesBlankPrefix()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, """
            {
              "Logging": { "LogLevel": { "Default": "Warning" } },
              "StudyFolder": { "RootPath": "X:\\velho", "DeliverablePrefix": "antigo", "PollSeconds": 60 },
            }
            """);

        LocalSettings.Save(_settingsPath, _studyFolder, "");

        var saved = Saved();
        Assert.Equal("Warning", (string?)saved["Logging"]!["LogLevel"]!["Default"]);
        Assert.Equal(60, (int?)saved["StudyFolder"]!["PollSeconds"]);
        Assert.Equal(_studyFolder, (string?)saved["StudyFolder"]!["RootPath"]);
        Assert.False(((JsonObject)saved["StudyFolder"]!).ContainsKey("DeliverablePrefix"));
    }

    [Fact]
    public void Save_ReplacesABrokenFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, "{ isto não é json");

        LocalSettings.Save(_settingsPath, _studyFolder, null);

        Assert.Equal(_studyFolder, (string?)Saved()["StudyFolder"]!["RootPath"]);
    }

    [Fact]
    public void Save_WritesAccentsReadably()
    {
        var accented = Directory.CreateDirectory(Path.Combine(_root, "Graduação")).FullName;
        File.WriteAllText(Path.Combine(accented, StatusDataStore.FileName), "window.DADOS = {};");

        LocalSettings.Save(_settingsPath, accented, null);

        Assert.Contains("Graduação", File.ReadAllText(_settingsPath));
    }

    [Fact]
    public void Save_RefusesInvalidInput()
    {
        Assert.Throws<ArgumentException>(() => LocalSettings.Save(_settingsPath, _root, null));
        Assert.Throws<ArgumentException>(() => LocalSettings.Save(_settingsPath, _studyFolder, "a/b"));
        Assert.False(File.Exists(_settingsPath));
    }
}
