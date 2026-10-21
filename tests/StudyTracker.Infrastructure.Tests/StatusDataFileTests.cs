using StudyTracker.Core.Status;
using StudyTracker.Infrastructure.Status;
using StudyTracker.Infrastructure.Tests.Fixtures;

namespace StudyTracker.Infrastructure.Tests;

public class StatusDataFileTests
{
    [Fact]
    public void Format_AfterParse_ReproducesTheFileExactly()
    {
        var text = Fixture.SampleText;
        var file = StatusDataFile.Parse(text);

        Assert.Equal(text, StatusDataFile.Format(file.Data, file));
    }

    [Fact]
    public void Parse_MapsFieldsToTheModel()
    {
        var data = StatusDataFile.Parse(Fixture.SampleText).Data;

        Assert.Equal(new DateOnly(2026, 3, 2), data.UpdatedOn);
        Assert.Equal(3, data.History.Count);
        var current = data.CurrentBlock;
        Assert.Equal("Bloco Exemplo", current.Name);
        var course = current.Groups![0].Courses[0];
        Assert.Equal("XY1", course.Code);
        Assert.Equal("Professor Exemplo", course.Professor);
        var tp2 = course.Assignments[1];
        Assert.Equal(AssignmentStatus.Submitted, tp2.Status);
        Assert.Equal(new DateOnly(2026, 3, 18), tp2.SubmittedOn);
        Assert.True(tp2.Stages!.Artifacts);
        Assert.True(course.Assignments[2].FolderMissing);
        Assert.Equal(["TP1", "TP2", "TP3", "AT"], data.Blocks[1].CompletedCourses![0].AssignmentIds);
    }

    [Fact]
    public void Format_KeepsUnknownFields()
    {
        var file = StatusDataFile.Parse(Fixture.SampleText);
        var at = file.Data.CurrentBlock.Groups![0].Courses[0].Assignments[3];

        at.NextStep = "Outro passo.";
        var text = StatusDataFile.Format(file.Data, file);

        Assert.Contains("\"campoNovo\": {", text);
        Assert.Contains("\"lista\": [1, 2]", text);
    }

    [Fact]
    public void Format_KeepsExistingKeyOrderAndAppendsNewKeys()
    {
        var file = StatusDataFile.Parse(Fixture.SampleText);
        var tp3 = file.Data.CurrentBlock.Groups![0].Courses[0].Assignments[2];

        tp3.Feedback = "Recebida.";
        var reparsed = StatusDataFile.Format(file.Data, file);

        var block = reparsed[reparsed.IndexOf("\"id\": \"TP3\"")..];
        block = block[..block.IndexOf('}')];
        var order = new[] { "\"id\"", "\"status\"", "\"semPasta\"", "\"proximo\"", "\"obs\"", "\"correcao\"" }
            .Select(k => block.IndexOf(k)).ToList();
        Assert.DoesNotContain(-1, order);
        Assert.Equal(order.Order(), order);
    }

    [Fact]
    public void Format_DropsFalseFlagsAndNullFields()
    {
        var file = StatusDataFile.Parse(Fixture.SampleText);
        var tp3 = file.Data.CurrentBlock.Groups![0].Courses[0].Assignments[2];

        tp3.FolderMissing = false;
        tp3.Notes = null;
        var text = StatusDataFile.Format(file.Data, file);

        Assert.DoesNotContain("semPasta", text);
        Assert.DoesNotContain("Campo semPasta antes de proximo.", text);
    }

    [Fact]
    public void Format_WithoutOriginal_UsesDefaultHeader()
    {
        var data = StatusDataFile.Parse(Fixture.SampleText).Data;

        var text = StatusDataFile.Format(data);

        Assert.StartsWith(StatusDataFile.DefaultHeader + "window.DADOS = {\n", text);
        Assert.EndsWith("};\n", text);
        Assert.Equal(data.Blocks.Count, StatusDataFile.Parse(text).Data.Blocks.Count);
    }

    [Theory]
    [InlineData("const x = 1;")]
    [InlineData("window.DADOS = { nao: json };")]
    [InlineData("window.DADOS = null;")]
    [InlineData("""window.DADOS = { "atualizadoEm": "2026-01-01", "historico": [], "blocos": [] , "x": };""")]
    public void Parse_RejectsInvalidContent(string text)
    {
        Assert.Throws<StatusDataFormatException>(() => StatusDataFile.Parse(text));
    }

    [Fact]
    public void Parse_RejectsUnknownStatus()
    {
        var text = Fixture.SampleText.Replace("\"status\": \"para-entregar\"", "\"status\": \"talvez\"");

        Assert.NotEqual(Fixture.SampleText, text);
        Assert.Throws<StatusDataFormatException>(() => StatusDataFile.Parse(text));
    }

    [Fact]
    public void Parse_RejectsMissingRequiredField()
    {
        var text = Fixture.SampleText.Replace("\"id\": \"TP1\",\n                  \"status\": \"para-entregar\"", "\"status\": \"para-entregar\"");

        Assert.NotEqual(Fixture.SampleText, text);
        Assert.Throws<StatusDataFormatException>(() => StatusDataFile.Parse(text));
    }

    [Fact]
    public void Parse_RejectsBlankRequiredField()
    {
        var text = Fixture.SampleText.Replace("\"sigla\": \"XY1\"", "\"sigla\": \" \"");

        Assert.Throws<StatusDataFormatException>(() => StatusDataFile.Parse(text));
    }

    [Fact]
    public void Parse_AcceptsTrailingCommasAndMissingSemicolon()
    {
        var text = """
            window.DADOS = {
              "atualizadoEm": "2026-01-01",
              "historico": [],
              "blocos": [],
            }
            """;

        Assert.Empty(StatusDataFile.Parse(text).Data.Blocks);
    }
}
