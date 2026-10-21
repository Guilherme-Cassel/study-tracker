using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using StudyTracker.Core.Status;

namespace StudyTracker.Infrastructure.Status;

// status-dados.js is a script for the static panel: an optional comment header, then "window.DADOS = <json>;".
public sealed partial class StatusDataFile
{
    public const string DefaultHeader =
        "// Dados do painel status.html. Regras de manutenção: seção 0.2 do \"GUIA - Trabalhos Infnet.md\".\n" +
        "// Editado pelo Claude e pelo próprio painel. Depois de \"window.DADOS =\" o conteúdo tem que ser JSON válido.\n";

    private const string DataPrefix = "window.DADOS = ";

    private readonly string _header;
    private readonly JsonNode _tree;

    private StatusDataFile(string header, JsonNode tree, StatusData data)
    {
        _header = header;
        _tree = tree;
        Data = data;
    }

    public StatusData Data { get; }

    [GeneratedRegex(@"^window\.DADOS\s*=", RegexOptions.Multiline)]
    private static partial Regex DataLine();

    public static StatusDataFile Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var match = DataLine().Match(text);
        if (!match.Success)
            throw new StatusDataFormatException("The file has no \"window.DADOS =\" line.");

        var body = text[(match.Index + match.Length)..].Trim();
        if (body.EndsWith(';')) body = body[..^1];

        try
        {
            var tree = JsonNode.Parse(body, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            }) ?? throw new StatusDataFormatException("The data after \"window.DADOS =\" is null.");
            var data = tree.Deserialize(StatusJsonContext.Default.StatusData)
                ?? throw new StatusDataFormatException("The data after \"window.DADOS =\" is null.");
            return new StatusDataFile(text[..match.Index], tree, data);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            throw new StatusDataFormatException($"The data after \"window.DADOS =\" is not valid: {ex.Message}", ex);
        }
    }

    public static string Format(StatusData data, StatusDataFile? original = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var tree = JsonSerializer.SerializeToNode(data, StatusJsonContext.Default.StatusData)!;
        if (original is not null) KeepKeyOrder(tree, original._tree);
        return (original?._header ?? DefaultHeader) + DataPrefix + CompactJson.Format(tree) + ";\n";
    }

    // Keys already in the file keep their position; keys the file did not have go to the end of their object.
    private static void KeepKeyOrder(JsonNode? current, JsonNode? previous)
    {
        if (current is JsonObject obj && previous is JsonObject old)
        {
            var ordered = old.Select(p => p.Key).Where(obj.ContainsKey)
                .Concat(obj.Select(p => p.Key).Where(k => !old.ContainsKey(k)))
                .ToList();
            var values = ordered.Select(k => (k, obj[k])).ToList();
            obj.Clear();
            foreach (var (key, value) in values)
            {
                obj[key] = value;
                KeepKeyOrder(value, old[key]);
            }
        }
        else if (current is JsonArray array && previous is JsonArray oldArray)
        {
            for (var i = 0; i < array.Count && i < oldArray.Count; i++)
                KeepKeyOrder(array[i], oldArray[i]);
        }
    }
}
