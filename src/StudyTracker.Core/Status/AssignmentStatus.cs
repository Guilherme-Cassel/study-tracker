using System.Text.Json.Serialization;

namespace StudyTracker.Core.Status;

[JsonConverter(typeof(JsonStringEnumConverter<AssignmentStatus>))]
public enum AssignmentStatus
{
    [JsonStringEnumMemberName("sem-contexto")] MissingContext,
    [JsonStringEnumMemberName("pronto")] Ready,
    [JsonStringEnumMemberName("andamento")] InProgress,
    [JsonStringEnumMemberName("para-entregar")] ReadyToSubmit,
    [JsonStringEnumMemberName("entregue")] Submitted
}
