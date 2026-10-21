using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class Block
{
    private string _name = "";
    private string _code = "";
    private string _folder = "";

    [JsonPropertyName("nome"), JsonRequired]
    public string Name { get => _name; set => _name = Guard.NotBlank(value, nameof(Name)); }

    [JsonPropertyName("codigo"), JsonRequired]
    public string Code { get => _code; set => _code = Guard.NotBlank(value, nameof(Code)); }

    [JsonPropertyName("pasta"), JsonRequired]
    public string Folder { get => _folder; set => _folder = Guard.NotBlank(value, nameof(Folder)); }

    [JsonPropertyName("atual"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsCurrent { get; set; }

    [JsonPropertyName("grupos")] public List<CourseGroup>? Groups { get; set; }

    [JsonPropertyName("concluido")] public List<CompletedCourse>? CompletedCourses { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
