using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;

namespace StudyTracker.Core.Tests;

public class FolderRulesTests
{
    private const string Prefix = "aluno_teste";

    private static FolderEntry File(string name) => new(name, false);
    private static FolderEntry Dir(string name) => new(name, true);

    private static AssignmentFolder Folder(params FolderEntry[] entries) => new(true, entries);

    [Fact]
    public void Detect_RecognizesTheGuideFileNames()
    {
        var files = FolderRules.Detect("DR3", "TP2",
            [File("Enunciado TP2.docx"), File("Rubricas TP2.txt"), File("Guia_TP2.md"), File("aluno_teste_DR3_TP2.pdf"), Dir("src")],
            Prefix);

        Assert.True(files.Brief);
        Assert.True(files.Rubrics);
        Assert.True(files.Guide);
        Assert.True(files.Artifacts);
        Assert.Empty(files.MisnamedDeliverables);
    }

    [Fact]
    public void Detect_IgnoresCaseButNotOtherAssignments()
    {
        var files = FolderRules.Detect("DR3", "TP2",
            [File("enunciado tp2.PDF"), File("Rubricas TP1.txt"), File("guia_tp2.MD"), File("aluno_teste_DR3_TP1.pdf")],
            Prefix);

        Assert.True(files.Brief);
        Assert.False(files.Rubrics);
        Assert.True(files.Guide);
        Assert.False(files.Artifacts);
    }

    [Fact]
    public void Detect_RequiresTxtRubricsAndIgnoresFolders()
    {
        var files = FolderRules.Detect("DR3", "TP2", [File("Rubricas TP2.png"), Dir("Enunciado TP2.docx")], Prefix);

        Assert.False(files.Rubrics);
        Assert.False(files.Brief);
    }

    [Fact]
    public void Detect_FlagsDeliverablesWithWrongCourseOrAssignment()
    {
        var files = FolderRules.Detect("DR5", "TP1",
            [File("aluno_teste_TP1.pdf"), File("aluno_teste_DR2_TP1.zip"), File("aluno_teste_DR5_TP1.pdf")],
            Prefix);

        Assert.True(files.Artifacts);
        Assert.Equal(["aluno_teste_TP1.pdf", "aluno_teste_DR2_TP1.zip"], files.MisnamedDeliverables);
    }

    [Fact]
    public void Detect_WithoutPrefix_MatchesBySuffixAndSkipsNameCheck()
    {
        var files = FolderRules.Detect("DR3", "TP2", [File("qualquer_DR3_TP2.zip"), File("aluno_teste_TP2.pdf")], null);

        Assert.True(files.Artifacts);
        Assert.Empty(files.MisnamedDeliverables);
    }

    [Fact]
    public void Suggest_MarksStagesFoundAndMovesToReady()
    {
        var work = new Assignment { Id = "TP2", Status = AssignmentStatus.MissingContext, NextStep = "Mandar enunciado e rubricas do TP2." };
        var folder = Folder(File("Enunciado TP2.docx"), File("Rubricas TP2.txt"));
        var files = FolderRules.Detect("DR3", "TP2", folder.Entries, Prefix);

        var suggestion = FolderRules.Suggest(work, folder, files)!;

        Assert.Equal([Stage.Brief, Stage.Rubrics], suggestion.Mark);
        Assert.Empty(suggestion.Unmark);
        Assert.True(suggestion.MoveToReady);

        suggestion.ApplyTo(work);
        Assert.Equal(AssignmentStatus.Ready, work.Status);
        Assert.True(work.Stages!.Brief && work.Stages.Rubrics);
        Assert.False(work.Stages.Definition);
        Assert.Null(work.NextStep);
    }

    [Fact]
    public void Suggest_DoesNotMoveToReadyWithoutRubrics()
    {
        var work = new Assignment { Id = "TP2", Status = AssignmentStatus.MissingContext };
        var folder = Folder(File("Enunciado TP2.docx"));

        var suggestion = FolderRules.Suggest(work, folder, FolderRules.Detect("DR3", "TP2", folder.Entries, Prefix))!;

        Assert.False(suggestion.MoveToReady);
        Assert.Equal([Stage.Brief], suggestion.Mark);
    }

    [Fact]
    public void Suggest_UnmarksStagesNotFound_AndKeepsDefinition()
    {
        var work = new Assignment
        {
            Id = "TP1", Status = AssignmentStatus.ReadyToSubmit, NextStep = "Postar.",
            Stages = new Stages { Brief = true, Rubrics = true, Definition = true, Guide = true, Artifacts = true }
        };
        var folder = Folder(File("Enunciado TP1.docx"), File("Rubricas TP1.txt"), File("aluno_teste_DR3_TP1.pdf"));

        var suggestion = FolderRules.Suggest(work, folder, FolderRules.Detect("DR3", "TP1", folder.Entries, Prefix))!;
        suggestion.ApplyTo(work);

        Assert.Equal([Stage.Guide], suggestion.Unmark);
        Assert.False(work.Stages.Guide);
        Assert.True(work.Stages.Definition);
        Assert.Equal("Postar.", work.NextStep);
    }

    [Fact]
    public void Suggest_ClearsFolderMissingFlagWhenFolderExists()
    {
        var work = new Assignment { Id = "TP4", Status = AssignmentStatus.MissingContext, FolderMissing = true };
        var folder = Folder();

        var suggestion = FolderRules.Suggest(work, folder, FolderRules.Detect("PB", "TP4", folder.Entries, Prefix))!;
        suggestion.ApplyTo(work);

        Assert.True(suggestion.FolderNowExists);
        Assert.False(work.FolderMissing);
        Assert.Null(work.Stages);
    }

    [Fact]
    public void Suggest_ReturnsNullWhenDataMatchesTheFolder()
    {
        var work = new Assignment
        {
            Id = "TP1", Status = AssignmentStatus.InProgress,
            Stages = new Stages { Brief = true, Rubrics = true, Definition = true }
        };
        var folder = Folder(File("Enunciado TP1.docx"), File("Rubricas TP1.txt"), Dir("src"));

        Assert.Null(FolderRules.Suggest(work, folder, FolderRules.Detect("DR3", "TP1", folder.Entries, Prefix)));
    }

    [Fact]
    public void Suggest_IgnoresStagesOfSubmittedWorkAndMissingFolders()
    {
        var submitted = new Assignment { Id = "TP1", Status = AssignmentStatus.Submitted };
        var folder = Folder(File("Enunciado TP1.docx"));

        Assert.Null(FolderRules.Suggest(submitted, folder, FolderRules.Detect("DR3", "TP1", folder.Entries, Prefix)));
        Assert.Null(FolderRules.Suggest(new Assignment { Id = "TP2", Status = AssignmentStatus.MissingContext },
            AssignmentFolder.Missing, FolderRules.Detect("DR3", "TP2", [], Prefix)));
    }

    [Fact]
    public void Scan_SignatureChangesWithFolderContents()
    {
        var course = new Course { Code = "DR3", Name = "X", Folder = "X", Assignments = [new Assignment { Id = "TP1", Status = AssignmentStatus.Ready }] };
        FolderScan Scan(params FolderEntry[] entries) =>
            new(new Dictionary<(string, string), AssignmentFolder> { [("DR3", "TP1")] = Folder(entries) }, ["DR3"]);

        Assert.Equal(Scan(File("a.txt")).Signature, Scan(File("a.txt")).Signature);
        Assert.NotEqual(Scan(File("a.txt")).Signature, Scan(File("a.txt"), File("b.txt")).Signature);
        Assert.True(Scan().CourseExists(course));
        Assert.True(Scan().For(course, course.Assignments[0]).Exists);
        Assert.False(FolderScan.Empty.For(course, course.Assignments[0]).Exists);
    }
}
