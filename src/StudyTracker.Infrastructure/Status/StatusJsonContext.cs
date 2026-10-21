using System.Text.Json;
using System.Text.Json.Serialization;
using StudyTracker.Core.Status;

namespace StudyTracker.Infrastructure.Status;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(StatusData))]
internal sealed partial class StatusJsonContext : JsonSerializerContext;
