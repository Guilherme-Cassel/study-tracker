using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

public sealed class CompletedCourse
{
    private string _code = "";
    private string _name = "";
    private string _folder = "";
    private List<string> _assignmentIds = [];

    [JsonPropertyName("sigla"), JsonRequired]
    public string Code { get => _code; set => _code = Guard.NotBlank(value, nameof(Code)); }

    [JsonPropertyName("nome"), JsonRequired]
    public string Name { get => _name; set => _name = Guard.NotBlank(value, nameof(Name)); }

    [JsonPropertyName("pasta"), JsonRequired]
    public string Folder { get => _folder; set => _folder = Guard.NotBlank(value, nameof(Folder)); }

    [JsonPropertyName("trabalhos"), JsonRequired]
    public List<string> AssignmentIds { get => _assignmentIds; set => _assignmentIds = Guard.NotNull(value, nameof(AssignmentIds)); }

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}
