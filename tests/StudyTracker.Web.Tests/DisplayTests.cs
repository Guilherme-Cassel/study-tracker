using StudyTracker.Core.Editing;
using StudyTracker.Core.Status;
using StudyTracker.Web.Services;

namespace StudyTracker.Web.Tests;

public class DisplayTests
{
    private static readonly Course Course = new() { Code = "DR3", Name = "X", Folder = "X" };
    private static readonly Assignment Work = new() { Id = "TP2", Status = AssignmentStatus.MissingContext };

    private static AssignmentEdit Original => AssignmentEdit.From(Work);

    [Fact]
    public void HistoryText_DescribesEachChangeLikeThePanel()
    {
        var edit = Original with
        {
            Status = AssignmentStatus.InProgress,
            DueOn = new DateOnly(2026, 11, 9),
            Brief = true,
            Rubrics = true,
            NextStep = "Fazer a definição."
        };

        Assert.Equal(
            "DR3 TP2: Falta contexto para Em andamento, prazo 09/11/2026, etapas concluídas: Enunciado, Rubricas, próximo passo atualizado.",
            Display.HistoryText(Course, Work, Original, edit));
    }

    [Fact]
    public void HistoryText_TellsRegisteredFromUpdatedAndRemoved()
    {
        var withFeedback = Original with { Feedback = "Nota 9." };

        Assert.Equal("DR3 TP2: correção registrada.", Display.HistoryText(Course, Work, Original, withFeedback));
        Assert.Equal("DR3 TP2: correção atualizada.", Display.HistoryText(Course, Work, withFeedback, withFeedback with { Feedback = "Nota 10." }));
        Assert.Equal("DR3 TP2: correção removida.", Display.HistoryText(Course, Work, withFeedback, Original));
    }

    [Fact]
    public void HistoryText_IsEmptyWhenOnlyWhitespaceChanged()
    {
        Assert.Equal("", Display.HistoryText(Course, Work, Original, Original with { Notes = "   " }));
    }

    [Theory]
    [InlineData(10, 20, "faltam 10 dias")]
    [InlineData(10, 11, "vence amanhã")]
    [InlineData(10, 10, "vence hoje")]
    [InlineData(10, 9, "venceu ontem")]
    [InlineData(10, 5, "venceu há 5 dias")]
    public void DueText_CountsDaysInPortuguese(int today, int due, string expected)
    {
        Assert.Equal(expected, Display.DueText(new DateOnly(2026, 10, due), new DateOnly(2026, 10, today)));
    }

    [Fact]
    public void Keys_RoundTripForEveryStatus()
    {
        foreach (var status in Enum.GetValues<AssignmentStatus>())
            Assert.Equal(status, Display.ParseKey(status.Key()));
        Assert.Null(Display.ParseKey("talvez"));
    }
}
