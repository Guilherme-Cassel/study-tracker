using StudyTracker.Core.Status;
using StudyTracker.Infrastructure.Folders;

namespace StudyTracker.Infrastructure.Tests;

public sealed class FolderTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("study-tracker-").FullName;
    private readonly StudyFolder _folder;

    public FolderTests()
    {
        _folder = new StudyFolder(_root, "aluno_teste");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static Block NewBlock() => new()
    {
        Name = "Bloco", Code = "26E3-26E4", Folder = "Bloco [26E3-26E4]", IsCurrent = true,
        Groups =
        [
            new CourseGroup
            {
                Title = "26E4",
                Courses =
                [
                    new Course
                    {
                        Code = "DR3", Name = "Disciplina", Folder = "DR3 - Disciplina [26E4]",
                        Assignments = [new Assignment { Id = "TP1", Status = AssignmentStatus.Ready }, new Assignment { Id = "TP2", Status = AssignmentStatus.MissingContext }]
                    },
                    new Course { Code = "DR4", Name = "Sem pasta", Folder = "DR4 - Sem pasta [26E4]", Assignments = [new Assignment { Id = "TP1", Status = AssignmentStatus.Ready }] }
                ]
            }
        ]
    };

    private string Make(params string[] parts)
    {
        var path = Path.Combine([_root, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("a/../..")]
    [InlineData(@"..\fora")]
    [InlineData(@"C:\Windows")]
    [InlineData("")]
    [InlineData("nome:inválido")]
    public void Resolve_RejectsNamesThatLeaveOrBreakThePath(string segment)
    {
        Assert.Throws<ArgumentException>(() => _folder.Resolve("Bloco", segment));
    }

    [Fact]
    public void Resolve_BuildsPathsInsideTheRoot()
    {
        Assert.Equal(Path.Combine(_root, "Bloco [26E3-26E4]", "TP1"), _folder.Resolve("Bloco [26E3-26E4]", "TP1"));
        Assert.Equal(_folder.RootPath, _folder.Resolve());
    }

    [Fact]
    public void Contains_DoesNotAcceptSiblingWithSamePrefix()
    {
        Assert.False(_folder.Contains(_folder.RootPath + "-outra"));
        Assert.True(_folder.Contains(Path.Combine(_folder.RootPath, "x")));
    }

    [Fact]
    public void Scan_ReadsAssignmentFoldersAndSkipsMissingOnes()
    {
        var tp1 = Make("Bloco [26E3-26E4]", "DR3 - Disciplina [26E4]", "TP1");
        File.WriteAllText(Path.Combine(tp1, "Rubricas TP1.txt"), "x");
        File.WriteAllText(Path.Combine(tp1, "Enunciado TP1.docx"), "x");
        Directory.CreateDirectory(Path.Combine(tp1, "src"));
        var hidden = Path.Combine(tp1, "desktop.ini");
        File.WriteAllText(hidden, "x");
        File.SetAttributes(hidden, FileAttributes.Hidden | FileAttributes.System);

        var block = NewBlock();
        var dr3 = block.Groups![0].Courses[0];
        var scan = new FolderScanner(_folder).Scan(block);

        var folder = scan.For(block, dr3, dr3.Assignments[0]);
        Assert.True(folder.Exists);
        Assert.Equal(["src/", "Enunciado TP1.docx", "Rubricas TP1.txt"], folder.Entries.Select(e => e.Name + (e.IsDirectory ? "/" : "")));
        Assert.False(scan.For(block, dr3, dr3.Assignments[1]).Exists);
        Assert.True(scan.CourseExists(block, dr3));
        Assert.False(scan.CourseExists(block, block.Groups[0].Courses[1]));
    }

    [Fact]
    public void Scan_SkipsFolderNamesThatWouldLeaveTheRoot()
    {
        Make("fora");
        var block = NewBlock();
        block.Folder = "..";

        var scan = new FolderScanner(_folder).Scan(block);

        Assert.False(scan.CourseExists(block, block.Groups![0].Courses[0]));
    }

    [Fact]
    public void Scan_CoversEveryBlock_AndOnlyChecksCourseFoldersOfFinishedOnes()
    {
        Make("Bloco [26E3-26E4]", "DR3 - Disciplina [26E4]", "TP1");
        Make("Antigo [25E3-25E4]", "DR3 - Outra [25E4]", "TP1");
        var current = NewBlock();
        var old = new Block
        {
            Name = "Antigo", Code = "25E3-25E4", Folder = "Antigo [25E3-25E4]",
            CompletedCourses =
            [
                new CompletedCourse { Code = "DR3", Name = "Outra", Folder = "DR3 - Outra [25E4]", AssignmentIds = ["TP1"] },
                new CompletedCourse { Code = "DR4", Name = "Sem pasta", Folder = "DR4 - Sem pasta [25E4]", AssignmentIds = ["TP1"] }
            ]
        };
        var dr3 = current.Groups![0].Courses[0];

        var scan = new FolderScanner(_folder).Scan([current, old]);

        Assert.True(scan.For(current, dr3, dr3.Assignments[0]).Exists);
        Assert.False(scan.For(old, dr3, dr3.Assignments[0]).Exists);
        Assert.True(scan.CourseExists(old, old.CompletedCourses[0]));
        Assert.False(scan.CourseExists(old, old.CompletedCourses[1]));
    }

    [Fact]
    public void Explorer_OpensExistingFoldersOnly()
    {
        var opened = new List<string>();
        var launcher = new ExplorerLauncher(_folder, opened.Add);
        var tp1 = Make("Bloco", "TP1");

        launcher.OpenFolder("Bloco", "TP1");

        Assert.Equal([tp1], opened);
        Assert.Throws<DirectoryNotFoundException>(() => launcher.OpenFolder("Bloco", "TP9"));
        Assert.Throws<ArgumentException>(() => launcher.OpenFolder(".."));
        Assert.Single(opened);
    }

    [Fact]
    public async Task Watcher_RaisesOneEventPerBurst()
    {
        using var watcher = new StudyFolderWatcher(_folder, TimeSpan.FromMilliseconds(200));
        var count = 0;
        var raised = new TaskCompletionSource();
        watcher.Changed += () =>
        {
            Interlocked.Increment(ref count);
            raised.TrySetResult();
        };
        Assert.True(watcher.Start());

        var dir = Make("Bloco", "TP1");
        for (var i = 0; i < 5; i++) await File.WriteAllTextAsync(Path.Combine(dir, $"f{i}.txt"), "x");

        await raised.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(600);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Watcher_DoesNotStartWhenFolderIsMissing()
    {
        using var watcher = new StudyFolderWatcher(new StudyFolder(Path.Combine(_root, "nao-existe")), TimeSpan.Zero);

        Assert.False(watcher.Start());
        Assert.False(watcher.IsRunning);
    }
}
