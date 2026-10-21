using StudyTracker.Core.Folders;

namespace StudyTracker.Core.Tests;

public class FileNamingTests
{
    [Theory]
    [InlineData("TP2 - Enunciado (1).docx", FileKind.Brief)]
    [InlineData("ENUNCIADO.pdf", FileKind.Brief)]
    [InlineData("rubricas moodle.png", FileKind.Rubrics)]
    [InlineData("anotacoes.txt", FileKind.Rubrics)]
    [InlineData("dataset.csv", FileKind.KeepName)]
    [InlineData("Mini Mundo SCA.docx", FileKind.KeepName)]
    public void Guess_UsesTheOriginalName(string name, FileKind expected)
    {
        Assert.Equal(expected, FileNaming.Guess(name));
    }

    [Theory]
    [InlineData(FileKind.Brief, "TP2 - Enunciado (1).docx", "Enunciado TP2.docx")]
    [InlineData(FileKind.Brief, "enunciado", "Enunciado TP2")]
    [InlineData(FileKind.Rubrics, "print rubricas.png", "Rubricas TP2.png")]
    [InlineData(FileKind.Rubrics, "rubricas", "Rubricas TP2.txt")]
    [InlineData(FileKind.KeepName, "dataset.csv", "dataset.csv")]
    public void NameFor_FollowsTheGuide(FileKind kind, string original, string expected)
    {
        Assert.Equal(expected, FileNaming.NameFor(kind, original, "TP2"));
    }

    [Fact]
    public void RubricsFileName_IsTheOneTheFolderRulesDetect()
    {
        var name = FileNaming.RubricsFileName("AT");

        Assert.Equal("Rubricas AT.txt", name);
        Assert.True(FolderRules.Detect("DR3", "AT", [new FolderEntry(name, false)], null).Rubrics);
    }

    [Theory]
    [InlineData("Enunciado TP2.docx", true)]
    [InlineData("Mini Mundo SCA.docx", true)]
    [InlineData("ação é ã.pdf", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("..", false)]
    [InlineData("a/b.txt", false)]
    [InlineData(@"..\fora.txt", false)]
    [InlineData("nome?.txt", false)]
    [InlineData("termina com ponto.", false)]
    [InlineData(" espaço.txt", false)]
    [InlineData("CON.txt", false)]
    [InlineData("lpt1", false)]
    [InlineData("tab\tdentro.txt", false)]
    public void IsValid_RejectsNamesWindowsWouldRefuseOrMisread(string name, bool expected)
    {
        Assert.Equal(expected, FileNaming.IsValid(name));
    }

    [Fact]
    public void IsValid_LimitsLength()
    {
        Assert.True(FileNaming.IsValid(new string('a', FileNaming.MaxNameLength)));
        Assert.False(FileNaming.IsValid(new string('a', FileNaming.MaxNameLength + 1)));
    }
}
