using StudyTracker.Core.Status;
using StudyTracker.Infrastructure.Status;
using StudyTracker.Infrastructure.Tests.Fixtures;

namespace StudyTracker.Infrastructure.Tests;

public sealed class StatusDataStoreTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("study-tracker-").FullName;
    private readonly StatusDataStore _store;

    public StatusDataStoreTests()
    {
        File.WriteAllText(DataPath, Fixture.SampleText);
        _store = new StatusDataStore(new StudyFolder(_root));
    }

    private string DataPath => Path.Combine(_root, StatusDataStore.FileName);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public async Task Save_WithoutChanges_WritesTheSameBytes()
    {
        var before = await File.ReadAllBytesAsync(DataPath);
        var snapshot = await _store.LoadAsync();

        var saved = await _store.SaveAsync(snapshot, snapshot.Data);

        Assert.Equal(before, await File.ReadAllBytesAsync(DataPath));
        Assert.Equal(snapshot.Version, saved.Version);
    }

    [Fact]
    public async Task Save_WritesChangesAndReturnsTheNewVersion()
    {
        var snapshot = await _store.LoadAsync();
        snapshot.Data.AddHistory(new DateOnly(2026, 3, 3), "Linha nova.");

        var saved = await _store.SaveAsync(snapshot, snapshot.Data);

        Assert.NotEqual(snapshot.Version, saved.Version);
        var reloaded = await _store.LoadAsync();
        Assert.Equal(saved.Version, reloaded.Version);
        Assert.Equal("Linha nova.", reloaded.Data.History[0].Text);
        Assert.False(File.Exists(DataPath + ".tmp"));
    }

    [Fact]
    public async Task Save_RefusesWhenTheFileChangedOnDisk()
    {
        var snapshot = await _store.LoadAsync();
        var external = Fixture.SampleText.Replace("2026-03-02", "2026-03-05");
        await File.WriteAllTextAsync(DataPath, external);
        snapshot.Data.AddHistory(new DateOnly(2026, 3, 3), "Não pode gravar.");

        await Assert.ThrowsAsync<StatusDataConflictException>(() => _store.SaveAsync(snapshot, snapshot.Data));

        Assert.Equal(external, await File.ReadAllTextAsync(DataPath));
        Assert.True(await _store.HasChangedAsync(snapshot));
    }

    [Fact]
    public async Task Save_AfterAnotherSave_UsesTheNewSnapshot()
    {
        var first = await _store.LoadAsync();
        first.Data.AddHistory(new DateOnly(2026, 3, 3), "Primeira.");
        var second = await _store.SaveAsync(first, first.Data);

        second.Data.AddHistory(new DateOnly(2026, 3, 4), "Segunda.");
        await _store.SaveAsync(second, second.Data);

        await Assert.ThrowsAsync<StatusDataConflictException>(() => _store.SaveAsync(first, first.Data));
        Assert.Equal("Segunda.", (await _store.LoadAsync()).Data.History[0].Text);
    }

    [Fact]
    public async Task Load_ReadsFileWithByteOrderMark()
    {
        await File.WriteAllTextAsync(DataPath, Fixture.SampleText, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var snapshot = await _store.LoadAsync();

        Assert.Equal("Bloco Exemplo", snapshot.Data.DefaultBlock!.Name);
    }

    [Fact]
    public async Task Load_ReportsMissingFile()
    {
        File.Delete(DataPath);

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => _store.LoadAsync());
        Assert.Equal(DataPath, ex.FileName);
    }

    [Fact]
    public async Task Load_ReportsInvalidContent()
    {
        await File.WriteAllTextAsync(DataPath, "window.DADOS = {;");

        await Assert.ThrowsAsync<StatusDataFormatException>(() => _store.LoadAsync());
    }
}
