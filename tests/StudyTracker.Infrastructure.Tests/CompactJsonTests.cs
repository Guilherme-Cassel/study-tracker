using System.Text.Json.Nodes;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.Infrastructure.Tests;

public class CompactJsonTests
{
    [Theory]
    [InlineData("simples", "\"simples\"")]
    [InlineData("ção é ã", "\"ção é ã\"")]
    [InlineData("aspas \"x\"", "\"aspas \\\"x\\\"\"")]
    [InlineData("barra \\", "\"barra \\\\\"")]
    [InlineData("a\nb\tc\rd", "\"a\\nb\\tc\\rd\"")]
    [InlineData("\b\f", "\"\\b\\f\"")]
    [InlineData("\u0001\u001f", "\"\\u0001\\u001f\"")]
    [InlineData("<tag> & '", "\"<tag> & '\"")]
    [InlineData("😀", "\"😀\"")]
    public void Quote_MatchesJavaScriptStringify(string text, string expected)
    {
        Assert.Equal(expected, CompactJson.Quote(text));
    }

    [Fact]
    public void Quote_EscapesLoneSurrogate()
    {
        Assert.Equal("\"\\ud83d\"", CompactJson.Quote("\ud83d"));
    }

    [Fact]
    public void Format_KeepsShortPrimitiveObjectOnOneLine()
    {
        var node = JsonNode.Parse("""{"a":1,"b":"x","c":true}""");

        Assert.Equal("""{ "a": 1, "b": "x", "c": true }""", CompactJson.Format(node));
    }

    [Fact]
    public void Format_BreaksLongPrimitiveObjectWithMoreThanTwoKeys()
    {
        var node = new JsonObject { ["a"] = new string('x', 50), ["b"] = new string('y', 50), ["c"] = "z" };

        Assert.StartsWith("{\n  \"a\": ", CompactJson.Format(node));
    }

    [Fact]
    public void Format_KeepsTwoKeyObjectOnOneLineEvenWhenLong()
    {
        var node = new JsonObject { ["data"] = "2026-01-01", ["texto"] = new string('x', 200) };

        Assert.DoesNotContain("\n", CompactJson.Format(node));
    }

    [Theory]
    [InlineData(0, true)]   // 100 chars
    [InlineData(5, true)]   // indent 10, 110 chars
    [InlineData(6, false)]  // indent 12, 112 chars
    public void Format_CountsIndentTowardTheLineWidth(int depth, bool expectedInline)
    {
        // { "a": "<69 x>", "b": "y", "c": "z" } is exactly 100 characters
        JsonNode node = new JsonObject { ["a"] = new string('x', 69), ["b"] = "y", ["c"] = "z" };
        for (var i = 0; i < depth; i++) node = new JsonObject { ["k"] = node };

        var lines = CompactJson.Format(node).Split('\n');

        Assert.Equal(expectedInline, lines.Any(l => l.Contains("\"a\"") && l.Contains("\"c\"")));
    }

    [Fact]
    public void Format_WritesPrimitiveArraysInline()
    {
        var node = JsonNode.Parse("""{"trabalhos":["TP1","TP2"]}""");

        Assert.Equal("{\n  \"trabalhos\": [\"TP1\", \"TP2\"]\n}", CompactJson.Format(node));
    }

    [Fact]
    public void Format_WritesEmptyContainersLikeJavaScript()
    {
        Assert.Equal("[]", CompactJson.Format(new JsonArray()));
        Assert.Equal("{  }", CompactJson.Format(new JsonObject()));
    }

    [Fact]
    public void Format_WritesNullAndNumbers()
    {
        Assert.Equal("""{ "n": null, "x": 1.5 }""", CompactJson.Format(JsonNode.Parse("""{"n":null,"x":1.5}""")));
    }
}
