using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class Assignment
{
    private string _id = "";

    [JsonPropertyName("id"), JsonRequired]
    public string Id { get => _id; set => _id = Guard.NotBlank(value, nameof(Id)); }

    [JsonPropertyName("status"), JsonRequired]
    public AssignmentStatus Status { get; set; }

    [JsonPropertyName("semPasta"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool FolderMissing { get; set; }

    [JsonPropertyName("prazo")] public DateOnly? DueOn { get; set; }
    [JsonPropertyName("entrega")] public DateOnly? SubmittedOn { get; set; }
    [JsonPropertyName("resumo")] public string? Summary { get; set; }
    [JsonPropertyName("etapas")] public Stages? Stages { get; set; }
    [JsonPropertyName("proximo")] public string? NextStep { get; set; }
    [JsonPropertyName("correcao")] public string? Feedback { get; set; }
    [JsonPropertyName("obs")] public string? Notes { get; set; }
    [JsonPropertyName("guia")] public string? GuideFile { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
