using Microsoft.Extensions.Logging.Abstractions;
using StudyTracker.Core.Editing;
using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;
using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Folders;
using StudyTracker.Infrastructure.Status;
using StudyTracker.UI.Services;

namespace StudyTracker.UI.Tests;

public sealed class StatusStateEditingTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 3, 15);
    private const string Block = "26E1-26E2";
    private const string Course = "XY1";
    private const string CourseFolder = "XY1 - Disciplina Exemplo [26E2]";
    private const string BlockFolder = "Bloco Exemplo [26E1-26E2]";

    private readonly string _root = Directory.CreateTempSubdirectory("study-tracker-web-").FullName;
    private readonly StatusDataStore _store;
    private readonly StatusState _state;

    public StatusStateEditingTests()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "status-dados.sample.js"), Path.Combine(_root, StatusDataStore.FileName));
        var folder = new StudyFolder(_root, "aluno_teste");
        _store = new StatusDataStore(folder);
        _state = new StatusState(folder, _store, new FolderScanner(folder), new FixedTime(Today), NullLogger<StatusState>.Instance);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private sealed class FixedTime(DateOnly day) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(day.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private static AssignmentKey Key(string id) => new(Block, Course, id);

    private async Task<Assignment> AssignmentAsync(string id) =>
        (await _store.LoadAsync()).Data.FindBlock(Block)!.FindAssignment(Course, id)!.Assignment;

    private async Task EditOnDiskAsync(string id, Action<Assignment> change)
    {
        var snapshot = await _store.LoadAsync();
        change(snapshot.Data.FindBlock(Block)!.FindAssignment(Course, id)!.Assignment);
        await _store.SaveAsync(snapshot, snapshot.Data);
    }

    private void MakeFolder(string id, params string[] files)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_root, BlockFolder, CourseFolder, id)).FullName;
        foreach (var file in files) File.WriteAllText(Path.Combine(dir, file), "x");
    }

    [Fact]
    public async Task SaveEdit_WritesTheFieldsTheHistoryAndTheDate()
    {
        await _state.GetAsync();
        var original = AssignmentEdit.From(await AssignmentAsync("TP1"));
        var edit = original with { DueOn = new DateOnly(2026, 3, 20), NextStep = "Revisar o PDF." };

        var outcome = await _state.SaveEditAsync(Key("TP1"), original, edit, "XY1 TP1: prazo 20/03/2026.");

        Assert.Equal(SaveStatus.Saved, outcome.Status);
        var data = (await _store.LoadAsync()).Data;
        var tp1 = data.FindBlock(Block)!.FindAssignment(Course, "TP1")!.Assignment;
        Assert.Equal(new DateOnly(2026, 3, 20), tp1.DueOn);
        Assert.Equal("Revisar o PDF.", tp1.NextStep);
        Assert.Equal(Today, data.UpdatedOn);
        Assert.Equal("XY1 TP1: prazo 20/03/2026.", data.History[0].Text);
        Assert.Equal(Today, data.History[0].Date);
    }

    [Fact]
    public async Task SaveEdit_KeepsChangesMadeElsewhereInTheFile()
    {
        await _state.GetAsync();
        var original = AssignmentEdit.From(await AssignmentAsync("TP1"));
        await EditOnDiskAsync("AT", a => a.Notes = "Escrito pelo Claude.");

        var outcome = await _state.SaveEditAsync(Key("TP1"), original, original with { Feedback = "Nota 10." }, "XY1 TP1: correção registrada.");

        Assert.Equal(SaveStatus.Saved, outcome.Status);
        Assert.Equal("Escrito pelo Claude.", (await AssignmentAsync("AT")).Notes);
        Assert.Equal("Nota 10.", (await AssignmentAsync("TP1")).Feedback);
    }

    [Fact]
    public async Task SaveEdit_RefusesWhenTheSameAssignmentChanged_AndReportsWhat()
    {
        await _state.GetAsync();
        var original = AssignmentEdit.From(await AssignmentAsync("TP1"));
        await EditOnDiskAsync("TP1", a => a.NextStep = "Passo novo do Claude.");
        var before = await File.ReadAllTextAsync(_store.FilePath);

        var outcome = await _state.SaveEditAsync(Key("TP1"), original, original with { Notes = "Minha nota." }, "XY1 TP1: observação atualizada.");

        Assert.Equal(SaveStatus.Conflict, outcome.Status);
        Assert.Equal([EditField.NextStep], outcome.ChangedElsewhere);
        Assert.Equal("Passo novo do Claude.", outcome.Current!.NextStep);
        Assert.Equal(before, await File.ReadAllTextAsync(_store.FilePath));
    }

    [Fact]
    public async Task SaveEdit_AfterConflict_SavesAgainstTheNewVersion()
    {
        await _state.GetAsync();
        var original = AssignmentEdit.From(await AssignmentAsync("TP1"));
        await EditOnDiskAsync("TP1", a => a.NextStep = "Passo novo do Claude.");
        var first = await _state.SaveEditAsync(Key("TP1"), original, original with { Notes = "Minha nota." }, "x");

        var retry = await _state.SaveEditAsync(Key("TP1"), first.Current!, first.Current! with { Notes = "Minha nota." }, "XY1 TP1: observação atualizada.");

        Assert.Equal(SaveStatus.Saved, retry.Status);
        var tp1 = await AssignmentAsync("TP1");
        Assert.Equal("Passo novo do Claude.", tp1.NextStep);
        Assert.Equal("Minha nota.", tp1.Notes);
    }

    [Fact]
    public async Task SaveEdit_ReportsUnknownAssignment()
    {
        var edit = AssignmentEdit.From(new Assignment { Id = "TP9", Status = AssignmentStatus.Ready });

        var outcome = await _state.SaveEditAsync(Key("TP9"), edit, edit with { Notes = "x" }, "x");

        Assert.Equal(SaveStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task SaveEdit_RaisesChanged()
    {
        await _state.GetAsync();
        var raised = 0;
        _state.Changed += () => raised++;
        var original = AssignmentEdit.From(await AssignmentAsync("TP1"));

        await _state.SaveEditAsync(Key("TP1"), original, original with { Notes = "x" }, "x");

        Assert.Equal(1, raised);
        Assert.Equal("x", (await _state.GetAsync()).Data!.FindBlock(Block)!.FindAssignment(Course, "TP1")!.Assignment.Notes);
    }

    [Fact]
    public async Task ApplyFolderSuggestion_MovesToReadyAndClearsTheFolderFlag()
    {
        MakeFolder("TP3", "Enunciado TP3.docx", "Rubricas TP3.txt");

        var outcome = await _state.ApplyFolderSuggestionAsync(Key("TP3"));

        Assert.Equal(SaveStatus.Saved, outcome.Status);
        var data = (await _store.LoadAsync()).Data;
        var tp3 = data.FindBlock(Block)!.FindAssignment(Course, "TP3")!.Assignment;
        Assert.Equal(AssignmentStatus.Ready, tp3.Status);
        Assert.False(tp3.FolderMissing);
        Assert.True(tp3.Stages!.Brief && tp3.Stages.Rubrics);
        Assert.Equal("XY1 TP3: painel acertado pela pasta (a pasta já existe; marcar Enunciado, Rubricas; mudar para Pronto p/ começar).", data.History[0].Text);
    }

    [Fact]
    public async Task ApplyFolderSuggestion_DoesNothingWhenFolderMatches()
    {
        MakeFolder("TP1", "Enunciado TP1.docx", "Rubricas TP1.txt", "Guia_TP1.md", "aluno_teste_XY1_TP1.pdf");
        var before = await File.ReadAllTextAsync(_store.FilePath);

        var outcome = await _state.ApplyFolderSuggestionAsync(Key("TP1"));

        Assert.Equal(SaveStatus.NothingToDo, outcome.Status);
        Assert.Equal(before, await File.ReadAllTextAsync(_store.FilePath));
    }
}
