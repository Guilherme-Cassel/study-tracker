using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class StatusData
{
    public const int MaxHistoryEntries = 10;

    private List<HistoryEntry> _history = [];
    private List<Block> _blocks = [];

    [JsonPropertyName("atualizadoEm"), JsonRequired]
    public DateOnly UpdatedOn { get; set; }

    [JsonPropertyName("historico"), JsonRequired]
    public List<HistoryEntry> History { get => _history; set => _history = Guard.NotNull(value, nameof(History)); }

    [JsonPropertyName("blocos"), JsonRequired]
    public List<Block> Blocks { get => _blocks; set => _blocks = Guard.NotNull(value, nameof(Blocks)); }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }

    // The block the app opens first: the one marked "atual", else the first one with course data, else the first.
    [JsonIgnore]
    public Block? DefaultBlock =>
        Blocks.FirstOrDefault(b => b.IsCurrent)
        ?? Blocks.FirstOrDefault(b => b.IsDetailed)
        ?? Blocks.FirstOrDefault();

    public Block? FindBlock(string code) =>
        Blocks.FirstOrDefault(b => string.Equals(b.Code, code, StringComparison.OrdinalIgnoreCase));

    public void AddHistory(DateOnly date, string text)
    {
        History.Insert(0, new HistoryEntry { Date = date, Text = text });
        if (History.Count > MaxHistoryEntries)
            History.RemoveRange(MaxHistoryEntries, History.Count - MaxHistoryEntries);
    }
}
