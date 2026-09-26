using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Stored boards on the wire (halheinrich/backgammon#15): a board is a
/// <see cref="BoardPosition"/> written as its 26 counts, a malformed board is
/// a malformed document, and an absent after-board is <c>null</c> — written
/// as null and read from null or from a missing member. The after-boards are
/// the row's columns now; a record derives its own and writes none. Every
/// case runs on the reflection path and through
/// <see cref="BgDataTypesJsonContext"/>.
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
        var record = JsonNode.Parse(JsonSerializer.Serialize<BgDecisionData>(TestRecords.Cube(), options))!.AsObject();
        var row = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Row(), options))!.AsObject();

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
    //
    // A record's after-boards are derived and never on its wire
    // (AfterBoardDerivationTests); the row carries them as optional columns,
    // taken from the record. The empty array older documents wrote for an
    // absent board belonged to the retired shape, which is refused whole
    // (WireGoldenTests), so it is refused here as the malformed board it is.

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_Null_RoundTrip(string path)
    {
        // Rewritten: a checker row with no user play among the candidates
        // writes its player board as null and reads it back as null; its best
        // board is present. (The retired PlayOutcomeData half is gone.)
        var options = OptionsFor(path);
        var row = TestRecords.Row(TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null)));

        var rowJson = JsonSerializer.Serialize(row, options);
        var rowBack = JsonSerializer.Deserialize<DecisionRow>(rowJson, options)!;

        Assert.Contains("\"AfterPlayerBoard\":null", rowJson);
        Assert.Null(rowBack.AfterPlayerBoard);
        Assert.Equal(row.AfterBestBoard, rowBack.AfterBestBoard);
        Assert.NotNull(rowBack.AfterBestBoard);
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_OldEmptyArray_IsRefused(string path)
    {
        // Rewritten from AfterBoards_OldEmptyArray_ReadsAsNull: the retired
        // NullableBoardPositionJsonConverter read [] as an absent board for
        // documents written before the boards were typed; every such document
        // is of the retired shape and refused whole, so the tolerance went
        // with it and [] is a malformed board.
        var options = OptionsFor(path);
        var row = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Row(TestRecords.Cube()), options))!.AsObject();
        row["AfterBestBoard"] = new JsonArray();

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionRow>(row.ToJsonString(), options));
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_OldOutcomeInsideARecord_IsRefused(string path)
    {
        // Rewritten from AfterBoards_OldEmptyArray_InsideARecord_ReadsAsNull:
        // the shape an old cube record carried — an Outcome with empty boards
        // — is a member no decision has now, and a decision refuses a member
        // it does not have.
        var options = OptionsFor(path);
        var record = JsonNode.Parse(JsonSerializer.Serialize<BgDecisionData>(TestRecords.Cube(), options))!.AsObject();
        record["Outcome"] = JsonNode.Parse("{\"AfterBestBoard\":[],\"AfterPlayerBoard\":[]}");

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BgDecisionData>(record.ToJsonString(), options));
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void AfterBoards_CubeDecision_NoneOnTheRecord_NullOnTheRow(string path)
    {
        // Rewritten from AfterBoards_CubeRecord_WrittenAsNull: a cube record
        // writes no after-board at all — no member of it — and a cube row
        // writes its two empty columns as null.
        var options = OptionsFor(path);
        var recordJson = JsonSerializer.Serialize<BgDecisionData>(TestRecords.Cube(), options);
        var rowJson = JsonSerializer.Serialize(TestRecords.Row(TestRecords.Cube()), options);

        Assert.DoesNotContain("AfterBestBoard", recordJson);
        Assert.DoesNotContain("Outcome", recordJson);
        Assert.Contains("\"AfterBestBoard\":null,\"AfterPlayerBoard\":null", rowJson);
    }

    [Theory]
    [MemberData(nameof(MalformedBoards))]
    public void AfterBoards_Malformed_IsAJsonException_OnBothPaths(string name, string json)
    {
        // Rewritten onto the row's optional player board: only null means
        // absent now; every other malformed board — the empty array included —
        // is refused as it is for a required board.
        if (json is "null")
            return;

        foreach (var path in new[] { "reflection", "context" })
        {
            var options = OptionsFor(path);
            var row = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Row(), options))!.AsObject();
            row["AfterPlayerBoard"] = JsonNode.Parse(json);

            var ex = Record.Exception(() => JsonSerializer.Deserialize<DecisionRow>(row.ToJsonString(), options));
            Assert.True(ex is JsonException, $"{name} on the {path} path: {ex?.GetType().Name ?? "no exception"}");
        }
    }
}
