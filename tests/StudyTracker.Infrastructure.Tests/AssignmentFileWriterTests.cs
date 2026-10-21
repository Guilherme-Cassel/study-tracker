using System.Text;
using StudyTracker.Infrastructure.Folders;

namespace StudyTracker.Infrastructure.Tests;

public sealed class AssignmentFileWriterTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("study-tracker-").FullName;
    private readonly AssignmentFileWriter _writer;
    private readonly string[] _course = ["Bloco", "DR3"];
    private readonly string[] _tp2 = ["Bloco", "DR3", "TP2"];

    public AssignmentFileWriterTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Bloco", "DR3", "TP2"));
        _writer = new AssignmentFileWriter(new StudyFolder(_root));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static MemoryStream Content(string text) => new(Encoding.UTF8.GetBytes(text));

    private string PathOf(params string[] parts) => Path.Combine([_root, .. parts]);

    [Fact]
    public async Task Save_WritesTheFileIntoTheAssignmentFolder()
    {
        Assert.True(_writer.FolderExists(_tp2));

        var path = await _writer.SaveAsync(_tp2, "Enunciado TP2.docx", Content("conteúdo"), overwrite: false);

        Assert.Equal(PathOf("Bloco", "DR3", "TP2", "Enunciado TP2.docx"), path);
        Assert.Equal("conteúdo", await File.ReadAllTextAsync(path));
        Assert.True(_writer.Exists(_tp2, "Enunciado TP2.docx"));
        Assert.Single(Directory.GetFiles(PathOf("Bloco", "DR3", "TP2")));
    }

    [Fact]
    public async Task Save_NeverCreatesTheAssignmentFolder()
    {
        string[] tp3 = ["Bloco", "DR3", "TP3"];
        Assert.False(_writer.FolderExists(tp3));

        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _writer.SaveAsync(tp3, "a.txt", Content("x"), overwrite: false));
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => _writer.SaveTextAsync(tp3, "Rubricas TP3.txt", "x", overwrite: false));

        Assert.False(Directory.Exists(PathOf("Bloco", "DR3", "TP3")));
        Assert.Equal(PathOf("Bloco", "DR3", "TP3"), _writer.FolderPath(tp3));
    }

    [Fact]
    public async Task ReadText_ReturnsTheFileOrNull()
    {
        await _writer.SaveTextAsync(_tp2, "Rubricas TP2.txt", "1. Uma\r\n2. Duas", overwrite: false);

        Assert.Equal("1. Uma\n2. Duas\n", await _writer.ReadTextAsync(_tp2, "Rubricas TP2.txt", 1024));
        Assert.Null(await _writer.ReadTextAsync(_tp2, "Rubricas TP2.txt", 4));
        Assert.Null(await _writer.ReadTextAsync(_tp2, "Rubricas TP9.txt", 1024));
        Assert.Null(await _writer.ReadTextAsync(["Bloco", "DR3", "TP3"], "Rubricas TP3.txt", 1024));
    }

    [Fact]
    public async Task Save_LeavesTheFileVisibleToTheFolderScanner()
    {
        var path = await _writer.SaveAsync(_tp2, "Enunciado TP2.docx", Content("x"), overwrite: false);
        await _writer.SaveAsync(_tp2, "Enunciado TP2.docx", Content("y"), overwrite: true);

        Assert.Equal((FileAttributes)0, File.GetAttributes(path) & (FileAttributes.Hidden | FileAttributes.System));
    }

    [Fact]
    public async Task Save_IntoCourseFolder()
    {
        await _writer.SaveAsync(_course, "Mini Mundo.docx", Content("x"), overwrite: false);

        Assert.True(File.Exists(PathOf("Bloco", "DR3", "Mini Mundo.docx")));
    }

    [Fact]
    public async Task Save_DoesNotOverwriteUnlessAsked()
    {
        await _writer.SaveAsync(_tp2, "a.txt", Content("primeiro"), overwrite: false);

        var ex = await Assert.ThrowsAsync<TargetExistsException>(() => _writer.SaveAsync(_tp2, "a.txt", Content("segundo"), overwrite: false));
        Assert.Equal(PathOf("Bloco", "DR3", "TP2", "a.txt"), ex.Path);
        Assert.Equal("primeiro", await File.ReadAllTextAsync(PathOf("Bloco", "DR3", "TP2", "a.txt")));

        await _writer.SaveAsync(_tp2, "a.txt", Content("segundo"), overwrite: true);
        Assert.Equal("segundo", await File.ReadAllTextAsync(PathOf("Bloco", "DR3", "TP2", "a.txt")));
        Assert.Single(Directory.GetFiles(PathOf("Bloco", "DR3", "TP2")));
    }

    [Fact]
    public async Task Save_RequiresTheCourseFolder()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _writer.SaveAsync(["Bloco", "DR9", "TP1"], "a.txt", Content("x"), overwrite: false));
        Assert.False(Directory.Exists(PathOf("Bloco", "DR9")));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("a/b.txt")]
    [InlineData(@"..\..\fora.txt")]
    [InlineData("CON")]
    public async Task Save_RejectsBadFileNames(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _writer.SaveAsync(_tp2, name, Content("x"), overwrite: false));
    }

    [Fact]
    public async Task Save_RejectsTargetsOutsideTheRoot()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _writer.SaveAsync(["Bloco", ".."], "a.txt", Content("x"), overwrite: false));
        await Assert.ThrowsAsync<ArgumentException>(() => _writer.SaveAsync([], "a.txt", Content("x"), overwrite: false));
    }

    [Fact]
    public async Task SaveText_WritesUtf8WithoutBomAndLineFeeds()
    {
        var path = await _writer.SaveTextAsync(_tp2, "Rubricas TP2.txt", "1. Rubrica ação\r\n2. Outra\r\n\r\n", overwrite: false);

        var bytes = await File.ReadAllBytesAsync(path);
        Assert.NotEqual(0xEF, bytes[0]);
        Assert.Equal("1. Rubrica ação\n2. Outra\n", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task Save_LeavesNoTempFileWhenTheCopyFails()
    {
        await Assert.ThrowsAsync<IOException>(() => _writer.SaveAsync(_tp2, "a.txt", new FailingStream(), overwrite: false));

        Assert.Empty(Directory.GetFiles(PathOf("Bloco", "DR3", "TP2")));
    }

    private sealed class FailingStream : MemoryStream
    {
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
            throw new IOException("falhou no meio");
    }
}
