using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class CourseGroup
{
    private string _title = "";
    private List<Course> _courses = [];

    [JsonPropertyName("titulo"), JsonRequired]
    public string Title { get => _title; set => _title = Guard.NotBlank(value, nameof(Title)); }

    [JsonPropertyName("detalhe")] public string? Detail { get; set; }

    [JsonPropertyName("disciplinas"), JsonRequired]
    public List<Course> Courses { get => _courses; set => _courses = Guard.NotNull(value, nameof(Courses)); }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
