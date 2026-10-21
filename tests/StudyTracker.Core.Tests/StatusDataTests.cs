using System.Text.Json;
using StudyTracker.Core.Status;

namespace StudyTracker.Core.Tests;

public class StatusDataTests
{
    private static Block NewBlock(string name, bool current) =>
        new() { Name = name, Code = "26E1-26E2", Folder = name, IsCurrent = current };

    [Fact]
    public void AddHistory_PutsNewestFirstAndKeepsTheLimit()
    {
        var data = new StatusData();
        for (var day = 1; day <= 12; day++)
            data.AddHistory(new DateOnly(2026, 1, day), $"Dia {day}.");

        Assert.Equal(StatusData.MaxHistoryEntries, data.History.Count);
        Assert.Equal("Dia 12.", data.History[0].Text);
        Assert.Equal("Dia 3.", data.History[^1].Text);
    }

    [Fact]
    public void AddHistory_RejectsBlankText()
    {
        Assert.Throws<ArgumentException>(() => new StatusData().AddHistory(new DateOnly(2026, 1, 1), " "));
    }

    [Fact]
    public void DefaultBlock_PrefersTheBlockMarkedAsCurrent()
    {
        var data = new StatusData { Blocks = [NewBlock("Antigo", false), NewBlock("Atual", true)] };

        Assert.Equal("Atual", data.DefaultBlock!.Name);
    }

    [Fact]
    public void DefaultBlock_FallsBackToTheFirstDetailedBlock_ThenTheFirstOfAll()
    {
        var summary = NewBlock("Resumido", false);
        var detailed = NewBlock("Detalhado", false);
        detailed.Groups = [];

        Assert.Equal("Detalhado", new StatusData { Blocks = [summary, detailed] }.DefaultBlock!.Name);
        Assert.Equal("Resumido", new StatusData { Blocks = [summary] }.DefaultBlock!.Name);
        Assert.Null(new StatusData().DefaultBlock);
    }

    [Fact]
    public void DefaultBlock_TakesTheFirstWhenMoreThanOneIsMarked()
    {
        var data = new StatusData { Blocks = [NewBlock("B0", true), NewBlock("B1", true)] };

        Assert.Equal("B0", data.DefaultBlock!.Name);
    }

    [Fact]
    public void FindBlock_IgnoresCase()
    {
        var block = NewBlock("Atual", true);
        var data = new StatusData { Blocks = [block] };

        Assert.Same(block, data.FindBlock("26e1-26E2"));
        Assert.Null(data.FindBlock("27E1-27E2"));
    }

    [Theory]
    [InlineData(AssignmentStatus.MissingContext, "sem-contexto")]
    [InlineData(AssignmentStatus.Ready, "pronto")]
    [InlineData(AssignmentStatus.InProgress, "andamento")]
    [InlineData(AssignmentStatus.ReadyToSubmit, "para-entregar")]
    [InlineData(AssignmentStatus.Submitted, "entregue")]
    public void AssignmentStatus_UsesTheFileNames(AssignmentStatus status, string name)
    {
        Assert.Equal($"\"{name}\"", JsonSerializer.Serialize(status));
        Assert.Equal(status, JsonSerializer.Deserialize<AssignmentStatus>($"\"{name}\""));
    }

    [Fact]
    public void Setters_RejectBlankRequiredText()
    {
        Assert.Throws<ArgumentException>(() => new Assignment { Id = "" });
        Assert.Throws<ArgumentException>(() => new Course { Code = " " });
        Assert.Throws<ArgumentException>(() => new Block { Folder = "" });
        Assert.Throws<ArgumentNullException>(() => new Course { Assignments = null! });
    }
}
