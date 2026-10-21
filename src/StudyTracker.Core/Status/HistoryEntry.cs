using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class HistoryEntry
{
    private string _text = "";

    [JsonPropertyName("data"), JsonRequired]
    public DateOnly Date { get; set; }

    [JsonPropertyName("texto"), JsonRequired]
    public string Text { get => _text; set => _text = Guard.NotBlank(value, nameof(Text)); }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
