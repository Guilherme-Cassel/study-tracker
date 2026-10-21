using System.Globalization;
using StudyTracker.Core.Editing;
using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;

namespace StudyTracker.UI.Services;

public static class Display
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static readonly AssignmentStatus[] StatusesByPriority =
        Enum.GetValues<AssignmentStatus>().OrderBy(s => s.Priority()).ToArray();

    public static string Label(this AssignmentStatus status) => status switch
    {
        AssignmentStatus.MissingContext => "Falta contexto",
        AssignmentStatus.Ready => "Pronto p/ começar",
        AssignmentStatus.InProgress => "Em andamento",
        AssignmentStatus.ReadyToSubmit => "Pronto p/ entregar",
        AssignmentStatus.Submitted => "Entregue",
        _ => status.ToString()
    };

    // Same keys as status-dados.js; used in CSS classes and query strings.
    public static string Key(this AssignmentStatus status) => status switch
    {
        AssignmentStatus.MissingContext => "sem-contexto",
        AssignmentStatus.Ready => "pronto",
        AssignmentStatus.InProgress => "andamento",
        AssignmentStatus.ReadyToSubmit => "para-entregar",
        AssignmentStatus.Submitted => "entregue",
        _ => status.ToString()
    };

    public static AssignmentStatus? ParseKey(string? key) =>
        Enum.GetValues<AssignmentStatus>().Cast<AssignmentStatus?>().FirstOrDefault(s => s!.Value.Key() == key);

    public static DateOnly Today(this TimeProvider time) => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", PtBr);

    public static string ShortDate(DateOnly date) => date.ToString("dd/MM", PtBr);

    public static string MonthShort(DateOnly date) => date.ToString("MMM", PtBr).TrimEnd('.');

    public static string WeekdayDate(DateOnly date) => date.ToString("ddd, dd/MM", PtBr);

    public static string Plural(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";

    public static string DueText(DateOnly due, DateOnly today)
    {
        var days = due.DaysUntil(today);
        return days switch
        {
            < -1 => $"venceu há {-days} dias",
            -1 => "venceu ontem",
            0 => "vence hoje",
            1 => "vence amanhã",
            _ => $"faltam {days} dias"
        };
    }

    public static string SinceText(DateOnly date, DateOnly today)
    {
        var days = today.DayNumber - date.DayNumber;
        return days switch
        {
            <= 0 => "hoje",
            1 => "ontem",
            _ => $"há {days} dias"
        };
    }

    public static string BlockUrl(Block block) => $"/blocos/{Uri.EscapeDataString(block.Code)}";

    public static string BlockAssignmentsUrl(Block block, AssignmentStatus? status = null) =>
        $"{BlockUrl(block)}/trabalhos{(status is { } s ? "?status=" + s.Key() : "")}";

    public static string CourseUrl(Block block, Course course) => $"{BlockUrl(block)}/{Uri.EscapeDataString(course.Code)}";

    public static string AssignmentUrl(Block block, Course course, Assignment assignment) =>
        $"{CourseUrl(block, course)}/{Uri.EscapeDataString(assignment.Id)}";

    public static string Label(this Stage stage) => stage switch
    {
        Stage.Brief => "Enunciado",
        Stage.Rubrics => "Rubricas",
        Stage.Definition => "Definição",
        Stage.Guide => "Guia",
        Stage.Artifacts => "Artefatos",
        _ => stage.ToString()
    };

    public static string Label(this EditField field) => field switch
    {
        EditField.Status => "status",
        EditField.DueOn => "prazo",
        EditField.SubmittedOn => "data de entrega",
        EditField.Stages => "etapas",
        EditField.NextStep => "próximo passo",
        EditField.Feedback => "correção",
        EditField.Notes => "observação",
        _ => field.ToString()
    };

    public static string Message(this EditProblem problem) => problem switch
    {
        EditProblem.SubmittedOnRequired => "Informe a data em que foi entregue.",
        EditProblem.SubmittedOnWithoutSubmission => "Data de entrega só vale para trabalho entregue.",
        EditProblem.SubmittedOnInFuture => "A data de entrega não pode estar no futuro.",
        EditProblem.TooLong => $"Texto longo demais (máximo de {AssignmentEdit.MaxTextLength} caracteres).",
        _ => problem.ToString()
    };

    // Same wording the static panel used for its history lines.
    public static string HistoryText(Course course, Assignment assignment, AssignmentEdit original, AssignmentEdit edit)
    {
        var a = original.Normalized();
        var b = edit.Normalized();
        var parts = new List<string>();
        if (a.Status != b.Status) parts.Add($"{a.Status.Label()} para {b.Status.Label()}");
        if (a.DueOn != b.DueOn) parts.Add(b.DueOn is { } due ? $"prazo {Date(due)}" : "prazo removido");
        if (a.SubmittedOn != b.SubmittedOn) parts.Add(b.SubmittedOn is { } on ? $"entregue em {Date(on)}" : "data de entrega removida");

        var stages = Enum.GetValues<Stage>();
        var done = stages.Where(s => b.HasStage(s) && !a.HasStage(s)).Select(Label).ToList();
        var reopened = stages.Where(s => !b.HasStage(s) && a.HasStage(s)).Select(Label).ToList();
        if (done.Count > 0) parts.Add("etapas concluídas: " + string.Join(", ", done));
        if (reopened.Count > 0) parts.Add("etapas reabertas: " + string.Join(", ", reopened));

        if (a.Feedback != b.Feedback) parts.Add(b.Feedback is null ? "correção removida" : a.Feedback is null ? "correção registrada" : "correção atualizada");
        if (a.NextStep != b.NextStep) parts.Add(b.NextStep is null ? "próximo passo removido" : "próximo passo atualizado");
        if (a.Notes != b.Notes) parts.Add(b.Notes is null ? "observação removida" : "observação atualizada");

        return parts.Count == 0 ? "" : $"{course.Code} {assignment.Id}: {string.Join(", ", parts)}.";
    }

    public static string Describe(FolderSuggestion suggestion)
    {
        var parts = new List<string>();
        if (suggestion.FolderNowExists) parts.Add("a pasta já existe");
        if (suggestion.Mark.Count > 0) parts.Add("marcar " + string.Join(", ", suggestion.Mark.Select(Label)));
        if (suggestion.Unmark.Count > 0) parts.Add("desmarcar " + string.Join(", ", suggestion.Unmark.Select(Label)) + " (não achei na pasta)");
        if (suggestion.MoveToReady) parts.Add("mudar para " + AssignmentStatus.Ready.Label());
        return string.Join("; ", parts);
    }
}
