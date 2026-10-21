using StudyTracker.Core.Folders;
using StudyTracker.Core.Status;
using StudyTracker.UI.Services;

namespace StudyTracker.UI.Tests;

public class ClaudePromptTests
{
    private const string Root = @"G:\Meu Drive\Infnet";

    private static readonly Block Block = new() { Name = "Desenvolvimento Backend", Code = "26E3-26E4", Folder = "Desenvolvimento Backend [26E3-26E4]" };
    private static readonly Course Course = new() { Code = "DR3", Name = "Serviços Web", Folder = "DR3 - Serviços Web [26E4]" };

    private static AssignmentFolderInfo Folder(bool exists, params string[] files)
    {
        var entries = files.Select(f => new FolderEntry(f, false)).ToList();
        var folder = exists ? new AssignmentFolder(true, entries) : AssignmentFolder.Missing;
        return new AssignmentFolderInfo(folder, FolderRules.Detect(Course.Code, "TP2", entries, "guilherme_cassel"), null);
    }

    [Fact]
    public void NewWork_WithoutFolder_AsksToCreateIt()
    {
        var work = new Assignment { Id = "TP2", Status = AssignmentStatus.MissingContext, NextStep = "Mandar enunciado e rubricas do TP2." };

        var text = ClaudePrompt.Build(Root, Block, Course, work, Folder(exists: false));

        Assert.Equal(
            "Novo trabalho: Desenvolvimento Backend [26E3-26E4], DR3 - Serviços Web [26E4], TP2.\r\n" +
            @"Pasta: G:\Meu Drive\Infnet\Desenvolvimento Backend [26E3-26E4]\DR3 - Serviços Web [26E4]\TP2 (ainda não existe; criar seguindo a seção 3 do guia)." + "\r\n" +
            "Status no painel: Falta contexto.\r\n" +
            "Etapas feitas: nenhuma.\r\n" +
            "Próximo passo registrado: Mandar enunciado e rubricas do TP2.\r\n" +
            "Siga o protocolo de início do guia (seção 4) e atualize o status-dados.js a cada etapa.\r\n",
            text.ReplaceLineEndings("\r\n"));
    }

    [Fact]
    public void Resume_ListsWhatIsOnDiskAndWhatIsMissing()
    {
        var work = new Assignment
        {
            Id = "TP2", Status = AssignmentStatus.InProgress, DueOn = new DateOnly(2026, 11, 9),
            Stages = new Stages { Brief = true, Definition = true }, NextStep = "Escrever o guia."
        };

        var text = ClaudePrompt.Build(Root, Block, Course, work, Folder(exists: true, "Enunciado TP2.docx", "Guia_TP2.md"));

        Assert.StartsWith("Retomar trabalho: ", text);
        Assert.Contains("Status no painel: Em andamento; prazo 09/11/2026.", text);
        Assert.Contains("Etapas feitas: enunciado, definição.", text);
        Assert.Contains("Na pasta: enunciado, guia.", text);
        Assert.Contains("Falta na pasta: rubricas.", text);
        Assert.Contains("Próximo passo registrado: Escrever o guia.", text);
        Assert.EndsWith("Continue de onde parou seguindo o guia e atualize o status-dados.js." + Environment.NewLine, text);
        Assert.DoesNotContain("ainda não existe", text);
    }

    [Fact]
    public void Submitted_AsksToRecordTheFeedback()
    {
        var work = new Assignment { Id = "TP2", Status = AssignmentStatus.Submitted, SubmittedOn = new DateOnly(2026, 10, 20), NextStep = "ignorado" };

        var text = ClaudePrompt.Build(Root, Block, Course, work, Folder(exists: true, "guilherme_cassel_DR3_TP2.pdf"));

        Assert.StartsWith("Correção recebida: ", text);
        Assert.Contains("Status no painel: Entregue; entregue em 20/10/2026.", text);
        Assert.Contains("Na pasta: artefatos.", text);
        Assert.DoesNotContain("Falta na pasta", text);
        Assert.DoesNotContain("Próximo passo", text);
        Assert.EndsWith("Registre a correção abaixo na seção 11 do guia e no status-dados.js:" + Environment.NewLine, text);
    }
}
