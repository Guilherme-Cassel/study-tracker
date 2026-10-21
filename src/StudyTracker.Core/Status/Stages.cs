using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public enum Stage
{
    Brief,
    Rubrics,
    Definition,
    Guide,
    Artifacts
}

public sealed class Stages
{
    [JsonPropertyName("enunciado")] public bool Brief { get; set; }
    [JsonPropertyName("rubricas")] public bool Rubrics { get; set; }
    [JsonPropertyName("definicao")] public bool Definition { get; set; }
    [JsonPropertyName("guia")] public bool Guide { get; set; }
    [JsonPropertyName("artefatos")] public bool Artifacts { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }

    public bool Get(Stage stage) => stage switch
    {
        Stage.Brief => Brief,
        Stage.Rubrics => Rubrics,
        Stage.Definition => Definition,
        Stage.Guide => Guide,
        Stage.Artifacts => Artifacts,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
    };

    public void Set(Stage stage, bool done)
    {
        switch (stage)
        {
            case Stage.Brief: Brief = done; break;
            case Stage.Rubrics: Rubrics = done; break;
            case Stage.Definition: Definition = done; break;
            case Stage.Guide: Guide = done; break;
            case Stage.Artifacts: Artifacts = done; break;
            default: throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
        }
    }
}
