using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace StudyTracker.Infrastructure.Status;

// Same layout as fmtJson in status.html, so the panel, the app and manual edits produce identical text.
public static class CompactJson
{
    public const int MaxInlineWidth = 110;
    private const string IndentStep = "  ";

    public static string Format(JsonNode? node)
    {
        var sb = new StringBuilder();
        Write(sb, node, "");
        return sb.ToString();
    }

    private static bool IsPrimitive(JsonNode? node) => node is null or JsonValue;

    private static void Write(StringBuilder sb, JsonNode? node, string indent)
    {
        switch (node)
        {
            case JsonArray array:
                WriteArray(sb, array, indent);
                break;
            case JsonObject obj:
                WriteObject(sb, obj, indent);
                break;
            default:
                sb.Append(Scalar(node));
                break;
        }
    }

    private static void WriteArray(StringBuilder sb, JsonArray array, string indent)
    {
        if (array.Count == 0)
        {
            sb.Append("[]");
            return;
        }
        if (array.All(IsPrimitive))
        {
            sb.Append('[').AppendJoin(", ", array.Select(Scalar)).Append(']');
            return;
        }
        var inner = indent + IndentStep;
        sb.Append("[\n");
        for (var i = 0; i < array.Count; i++)
        {
            if (i > 0) sb.Append(",\n");
            sb.Append(inner);
            Write(sb, array[i], inner);
        }
        sb.Append('\n').Append(indent).Append(']');
    }

    private static void WriteObject(StringBuilder sb, JsonObject obj, string indent)
    {
        if (obj.All(p => IsPrimitive(p.Value)))
        {
            var line = "{ " + string.Join(", ", obj.Select(p => Quote(p.Key) + ": " + Scalar(p.Value))) + " }";
            if (obj.Count <= 2 || line.Length + indent.Length <= MaxInlineWidth)
            {
                sb.Append(line);
                return;
            }
        }
        var inner = indent + IndentStep;
        sb.Append("{\n");
        var first = true;
        foreach (var (key, value) in obj)
        {
            if (!first) sb.Append(",\n");
            first = false;
            sb.Append(inner).Append(Quote(key)).Append(": ");
            Write(sb, value, inner);
        }
        sb.Append('\n').Append(indent).Append('}');
    }

    private static string Scalar(JsonNode? node)
    {
        if (node is null) return "null";
        var value = (JsonValue)node;
        return value.GetValueKind() switch
        {
            JsonValueKind.String => Quote(value.GetValue<string>()),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => value.ToJsonString()
        };
    }

    // Escapes exactly like JavaScript's JSON.stringify.
    public static string Quote(string text)
    {
        var sb = new StringBuilder(text.Length + 2).Append('"');
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    var loneSurrogate =
                        (char.IsHighSurrogate(c) && (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))) ||
                        (char.IsLowSurrogate(c) && (i == 0 || !char.IsHighSurrogate(text[i - 1])));
                    if (c < 0x20 || loneSurrogate)
                        sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}
