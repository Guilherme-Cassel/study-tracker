using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class Course
{
    private string _code = "";
    private string _name = "";
    private string _folder = "";
    private List<Assignment> _assignments = [];

    [JsonPropertyName("sigla"), JsonRequired]
    public string Code { get => _code; set => _code = Guard.NotBlank(value, nameof(Code)); }

    [JsonPropertyName("nome"), JsonRequired]
    public string Name { get => _name; set => _name = Guard.NotBlank(value, nameof(Name)); }

    [JsonPropertyName("pasta"), JsonRequired]
    public string Folder { get => _folder; set => _folder = Guard.NotBlank(value, nameof(Folder)); }

    [JsonPropertyName("professor")] public string? Professor { get; set; }

    [JsonPropertyName("trabalhos"), JsonRequired]
    public List<Assignment> Assignments { get => _assignments; set => _assignments = Guard.NotNull(value, nameof(Assignments)); }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
