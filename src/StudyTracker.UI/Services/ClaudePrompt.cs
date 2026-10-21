using System.Text;
using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;

namespace StudyTracker.UI.Services;

// The message pasted into Claude Code to start, resume or close an assignment. It names the folders the way
// the study guide does (block and course folder names) and says what is on disk, so Claude can skip the
// discovery steps of the guide's protocol.
public static class ClaudePrompt
{
    public static string Build(string rootPath, Block block, Course course, Assignment assignment, AssignmentFolderInfo folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(course);
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(folder);

        var submitted = assignment.Status == AssignmentStatus.Submitted;
        var starting = assignment.Status is AssignmentStatus.MissingContext or AssignmentStatus.Ready;
        var kind = submitted ? "Correção recebida" : starting ? "Novo trabalho" : "Retomar trabalho";
        var path = Path.Combine(rootPath, block.Folder, course.Folder, assignment.Id);

        var sb = new StringBuilder();
        sb.Append(kind).Append(": ").Append(block.Folder).Append(", ").Append(course.Folder).Append(", ").Append(assignment.Id).AppendLine(".");

        sb.Append("Pasta: ").Append(path);
        if (!folder.Folder.Exists) sb.Append(" (ainda não existe; criar seguindo a seção 3 do guia)");
        sb.AppendLine(".");

        sb.Append("Status no painel: ").Append(assignment.Status.Label());
        if (!submitted && assignment.DueOn is { } due) sb.Append("; prazo ").Append(Display.Date(due));
        if (submitted && assignment.SubmittedOn is { } on) sb.Append("; entregue em ").Append(Display.Date(on));
        sb.AppendLine(".");

        var stages = Enum.GetValues<Stage>().Where(s => assignment.Stages?.Get(s) == true).Select(s => s.Label().ToLowerInvariant()).ToList();
        sb.Append("Etapas feitas: ").Append(stages.Count > 0 ? string.Join(", ", stages) : "nenhuma").AppendLine(".");

        if (folder.Folder.Exists)
        {
            var present = new List<string>();
            if (folder.Files.Brief) present.Add("enunciado");
            if (folder.Files.Rubrics) present.Add("rubricas");
            if (folder.Files.Guide) present.Add("guia");
            if (folder.Files.Artifacts) present.Add("artefatos");
            sb.Append("Na pasta: ").Append(present.Count > 0 ? string.Join(", ", present) : "nada").AppendLine(".");

            if (!submitted)
            {
                var missing = new List<string>();
                if (!folder.Files.Brief) missing.Add("enunciado");
                if (!folder.Files.Rubrics) missing.Add("rubricas");
                if (missing.Count > 0) sb.Append("Falta na pasta: ").Append(string.Join(", ", missing)).AppendLine(".");
            }
        }

        if (!submitted && !string.IsNullOrWhiteSpace(assignment.NextStep))
            sb.Append("Próximo passo registrado: ").AppendLine(assignment.NextStep.Trim());
        if (submitted && !string.IsNullOrWhiteSpace(assignment.Feedback))
            sb.Append("Correção registrada: ").AppendLine(assignment.Feedback.Trim());
        if (!string.IsNullOrWhiteSpace(assignment.Notes))
            sb.Append("Observação: ").AppendLine(assignment.Notes.Trim());

        sb.AppendLine(submitted
            ? "Registre a correção abaixo na seção 11 do guia e no status-dados.js:"
            : starting
                ? "Siga o protocolo de início do guia (seção 4) e atualize o status-dados.js a cada etapa."
                : "Continue de onde parou seguindo o guia e atualize o status-dados.js.");
        return sb.ToString();
    }
}
