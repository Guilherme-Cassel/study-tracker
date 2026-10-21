using System.Globalization;
using StudyTracker.Core.Status;

namespace StudyTracker.Web.Services;

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

    public static string CourseUrl(Course course) => $"/disciplinas/{Uri.EscapeDataString(course.Code)}";

    public static string AssignmentUrl(Course course, Assignment assignment) => $"{CourseUrl(course)}#{assignment.Id}";

    public static IEnumerable<(string Key, string Label)> StageList =>
    [
        ("enunciado", "Enunciado"), ("rubricas", "Rubricas"), ("definicao", "Definição"), ("guia", "Guia"), ("artefatos", "Artefatos")
    ];

    public static bool IsDone(this Stages stages, string key) => key switch
    {
        "enunciado" => stages.Brief,
        "rubricas" => stages.Rubrics,
        "definicao" => stages.Definition,
        "guia" => stages.Guide,
        "artefatos" => stages.Artifacts,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
    };
}
