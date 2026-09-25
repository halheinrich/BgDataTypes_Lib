using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Stored boards on the wire (halheinrich/backgammon#15): a board is a
/// <see cref="BoardPosition"/> written as its 26 counts, a malformed board is
/// a malformed document, and an absent after-board is <c>null</c> — written
/// as null, read from null or from a missing member, and read from the empty
/// array older documents wrote. Every case runs on the reflection path and
/// through <see cref="BgDataTypesJsonContext"/>.
/// </summary>
public class BoardWireTests
{
    public static TheoryData<string> Paths => ["reflection", "context"];

    private static JsonSerializerOptions OptionsFor(string path) => path switch
    {
        "reflection" => new JsonSerializerOptions(),
        "context" => new JsonSerializerOptions { TypeInfoResolver = BgDataTypesJsonContext.Default },
        _ => throw new ArgumentOutOfRangeException(nameof(path)),
    };

    private const string StandardJson = "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0]";

    // ── The board value ───────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Paths))]
    public void Board_WritesItsCounts_AndReadsThemBack(string path)
    {
        var options = OptionsFor(path);

        Assert.Equal(StandardJson, JsonSerializer.Serialize(BoardPosition.Standard, options));
        Assert.Equal(BoardPosition.Standard, JsonSerializer.Deserialize<BoardPosition>(StandardJson, options));
    }

    public static TheoryData<string, string> MalformedBoards => new()
    {
        { "no counts", "[]" },
        { "25 counts", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2]" },
        { "27 counts", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0,0]" },
        { "a fraction", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,1.5,0]" },
        { "beyond int", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,10000000000,0]" },
        { "a string count", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,\"2\",0]" },
        { "a null count", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,null,0]" },
        { "a nested array", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,[2],0]" },
        { "sixteen on-roll checkers", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,3,0]" },
        { "an on-roll checker on the opponent's bar", "[1,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,1,0]" },
        { "an opponent's checker on the on-roll bar", "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-4,0,0,0,0,2,-1]" },
        { "an object", "{\"Mop\":[]}" },
        { "a string", "\"standard\"" },
        { "a number", "26" },
        { "null", "null" },
    };

    [Theory]
    [MemberData(nameof(MalformedBoards))]
    public void Board_Malformed_IsAJsonException_OnBothPaths(string name, string json)
    {
        foreach (var path in new[] { "reflection", "context" })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize<BoardPosition>(json, OptionsFor(path)));
            Assert.True(ex is JsonException, $"{name} on the {path} path: {ex?.GetType().Name ?? "no exception"}");
        }
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void RequiredBoards_Null_OrEmpty_AreRefused(string path)
    {
        // A board that must be present is never absent in disguise: neither
        // null nor the empty array reads as a board.
        var options = OptionsFor(path);
        var record = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Record(), options))!.AsObject();
        var row = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Row(roll: 31), options))!.AsObject();

        foreach (JsonNode? value in new JsonNode?[] { null, new JsonArray() })
        {
            var r = record.DeepClone().AsObject();
            r["Position"]!["Mop"] = value?.DeepClone();
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BgDecisionData>(r.ToJsonString(), options));

            var w = row.DeepClone().AsObject();
            w["Board"] = value?.DeepClone();
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionRow>(w.ToJsonString(), options));
        }
    }

    // ── Optional after-boards ─────────────────────────────────────

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_Null_RoundTrip(string path)
    {
        var options = OptionsFor(path);
        var outcome = TestRecords.Outcome(afterBestBoard: BoardPosition.Standard, afterPlayerBoard: null);
        var row = TestRecords.Row(roll: 31, afterBestBoard: null, afterPlayerBoard: BoardPosition.Standard);

        var outcomeJson = JsonSerializer.Serialize(outcome, options);
        var rowJson = JsonSerializer.Serialize(row, options);
        var outcomeBack = JsonSerializer.Deserialize<PlayOutcomeData>(outcomeJson, options)!;
        var rowBack = JsonSerializer.Deserialize<DecisionRow>(rowJson, options)!;

        Assert.Equal($"{{\"AfterBestBoard\":{StandardJson},\"AfterPlayerBoard\":null}}", outcomeJson);
        Assert.Contains($"\"AfterBestBoard\":null,\"AfterPlayerBoard\":{StandardJson}", rowJson);
        Assert.Equal(BoardPosition.Standard, outcomeBack.AfterBestBoard);
        Assert.Null(outcomeBack.AfterPlayerBoard);
        Assert.Null(rowBack.AfterBestBoard);
        Assert.Equal(BoardPosition.Standard, rowBack.AfterPlayerBoard);
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_OldEmptyArray_ReadsAsNull(string path)
    {
        // Documents written before the boards were typed spelled an absent
        // after-board as []; they keep loading, as null.
        var options = OptionsFor(path);

        var outcome = JsonSerializer.Deserialize<PlayOutcomeData>(
            "{\"AfterBestBoard\":[],\"AfterPlayerBoard\":[ ]}", options)!;
        var row = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Row(roll: 0), options))!.AsObject();
        row["AfterBestBoard"] = new JsonArray();
        row["AfterPlayerBoard"] = new JsonArray();
        var rowBack = JsonSerializer.Deserialize<DecisionRow>(row.ToJsonString(), options)!;

        Assert.Null(outcome.AfterBestBoard);
        Assert.Null(outcome.AfterPlayerBoard);
        Assert.Null(rowBack.AfterBestBoard);
        Assert.Null(rowBack.AfterPlayerBoard);

        // …and it is never written: a read-back writes null.
        Assert.Equal("{\"AfterBestBoard\":null,\"AfterPlayerBoard\":null}",
            JsonSerializer.Serialize(outcome, options));
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_OldEmptyArray_InsideARecord_ReadsAsNull(string path)
    {
        // The shape an old cube record carried, inside the whole document.
        var options = OptionsFor(path);
        var record = JsonNode.Parse(JsonSerializer.Serialize(
            TestRecords.Record(decision: TestRecords.Decision(isCube: true)), options))!.AsObject();
        record["Outcome"] = JsonNode.Parse("{\"AfterBestBoard\":[],\"AfterPlayerBoard\":[]}");

        var restored = JsonSerializer.Deserialize<BgDecisionData>(record.ToJsonString(), options)!;

        Assert.Null(restored.AfterBestBoard);
        Assert.Null(restored.AfterPlayerBoard);
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_CubeRecord_WrittenAsNull(string path)
    {
        // The one intended byte change of the leg: a cube record's absent
        // after-boards were written [] and are written null.
        var json = JsonSerializer.Serialize(
            TestRecords.Record(decision: TestRecords.Decision(isCube: true)), OptionsFor(path));

        Assert.EndsWith("\"Outcome\":{\"AfterBestBoard\":null,\"AfterPlayerBoard\":null}}", json);
    }

    [Theory]
    [MemberData(nameof(MalformedBoards))]
    public void AfterBoards_Malformed_IsAJsonException_OnBothPaths(string name, string json)
    {
        // Only null and the empty array mean absent; every other malformed
        // board is refused as it is for a required board.
        if (json is "null" or "[]")
            return;

        foreach (var path in new[] { "reflection", "context" })
        {
            var ex = Record.Exception(() => JsonSerializer.Deserialize<PlayOutcomeData>(
                $"{{\"AfterBestBoard\":{json}}}", OptionsFor(path)));
            Assert.True(ex is JsonException, $"{name} on the {path} path: {ex?.GetType().Name ?? "no exception"}");
        }
    }
}
