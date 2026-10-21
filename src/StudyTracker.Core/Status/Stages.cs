using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class Stages
{
    [JsonPropertyName("enunciado")] public bool Brief { get; set; }
    [JsonPropertyName("rubricas")] public bool Rubrics { get; set; }
    [JsonPropertyName("definicao")] public bool Definition { get; set; }
    [JsonPropertyName("guia")] public bool Guide { get; set; }
    [JsonPropertyName("artefatos")] public bool Artifacts { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
