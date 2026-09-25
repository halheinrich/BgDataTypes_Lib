using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The Crawford-cube invariant on the flat row
/// (halheinrich/backgammon#201): a row whose <see cref="DecisionRow.Roll"/>
/// is 0 (a cube decision) and whose <see cref="DecisionRow.IsCrawford"/> is
/// true describes a decision that cannot exist, and the row refuses to be
/// constructed. The row's trap is that cube is its <em>default</em> kind, so
/// the guard needs two things the composite record does not:
/// <see cref="DecisionRow.Roll"/> is <c>required</c>, so every construction
/// and every document states the kind, and the guard distinguishes a
/// not-yet-stated roll from a stated 0, so the legal initializer order
/// <c>{ IsCrawford = true, Roll = 31 }</c> constructs. The
/// <c>UserDoublerAction</c> rejection tests are the pattern.
/// </summary>
/// <remarks>
/// Every stored member of the row is required or nullable
/// (halheinrich/backgammon#222), so each initializer here states the whole
/// row; the two members under test come first, in the order under test.
/// </remarks>
public class DecisionRowCrawfordCubeTests
{
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    // ---------------------------------------------------------------------
    //  Object initializers — whichever member completes the Crawford cube
    //  is the one named
    // ---------------------------------------------------------------------

    [Fact]
    public void CrawfordCube_IsCrawfordSetSecond_ThrowsNamingIsCrawford()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 0,
            IsCrawford = true,
            Xgid = "", Error = 0, MatchLength = 5, Player = "", Game = 1, MoveNumber = 1,
            IsStandardStart = false, AnalysisDepth = "", AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown, Equity = 0, OnRollNeeds = 1, OpponentNeeds = 3,
            Board = BoardPosition.Standard,
        });

        Assert.Equal("IsCrawford", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordCube_RollSetSecond_ThrowsNamingRoll()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            IsCrawford = true,
            Roll = 0,
            Xgid = "", Error = 0, MatchLength = 5, Player = "", Game = 1, MoveNumber = 1,
            IsStandardStart = false, AnalysisDepth = "", AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown, Equity = 0, OnRollNeeds = 1, OpponentNeeds = 3,
            Board = BoardPosition.Standard,
        });

        Assert.Equal("Roll", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordPlay_Constructs_InEitherOrder()
    {
        // The trap order first: IsCrawford is set while Roll is still
        // unstated, and an unstated roll must not read as a cube.
        var crawfordFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            IsCrawford = true,
            Roll = 31,
            Xgid = "", Error = 0, MatchLength = 5, Player = "", Game = 1, MoveNumber = 1,
            IsStandardStart = false, AnalysisDepth = "", AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown, Equity = 0, OnRollNeeds = 1, OpponentNeeds = 3,
            Board = BoardPosition.Standard,
        };
        var rollFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 31,
            IsCrawford = true,
            Xgid = "", Error = 0, MatchLength = 5, Player = "", Game = 1, MoveNumber = 1,
            IsStandardStart = false, AnalysisDepth = "", AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown, Equity = 0, OnRollNeeds = 1, OpponentNeeds = 3,
            Board = BoardPosition.Standard,
        };

        Assert.True(crawfordFirst.IsCrawford);
        Assert.False(crawfordFirst.IsCube);
        Assert.Equal(31, crawfordFirst.Roll);
        Assert.True(rollFirst.IsCrawford);
        Assert.False(rollFirst.IsCube);
        Assert.Equal(31, rollFirst.Roll);
    }

    [Fact]
    public void NonCrawfordCube_Constructs_InEitherOrder()
    {
        var rollFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 0,
            IsCrawford = false,
            Xgid = "", Error = 0, MatchLength = 5, Player = "", Game = 1, MoveNumber = 1,
            IsStandardStart = false, AnalysisDepth = "", AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown, Equity = 0, OnRollNeeds = 1, OpponentNeeds = 3,
            Board = BoardPosition.Standard,
        };
        var crawfordFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            IsCrawford = false,
            Roll = 0,
            Xgid = "", Error = 0, MatchLength = 5, Player = "", Game = 1, MoveNumber = 1,
            IsStandardStart = false, AnalysisDepth = "", AnalysisMode = AnalysisMode.Unknown,
            AnalysisLevel = AnalysisLevel.Unknown, Equity = 0, OnRollNeeds = 1, OpponentNeeds = 3,
            Board = BoardPosition.Standard,
        };

        Assert.True(rollFirst.IsCube);
        Assert.False(rollFirst.IsCrawford);
        Assert.True(crawfordFirst.IsCube);
        Assert.False(crawfordFirst.IsCrawford);
    }

    // ---------------------------------------------------------------------
    //  The wire — the same guards run during deserialization, in either
    //  property order, through the reflection path and the source-generated
    //  context alike; and a document that never states Roll is refused
    //  rather than defaulting to a cube
    // ---------------------------------------------------------------------

    /// <summary>
    /// A full row document with <paramref name="leading"/> first, in the
    /// order given, and the row's other members after it; a member given
    /// <see langword="null"/> is left out. Rewritten from literal JSON: every
    /// other member of the row is required, so the rest of a full row
    /// travels with the members under test.
    /// </summary>
    private static string RowDocument(params (string Name, JsonNode? Value)[] leading)
    {
        var full = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Row(roll: 31)))!.AsObject();
        var document = new JsonObject { ["Id"] = "test.xgp" };
        foreach (var (name, value) in leading)
            if (value is not null)
                document[name] = value;
        foreach (var (name, value) in full)
            if (name != "Id" && !leading.Any(l => l.Name == name))
                document[name] = value?.DeepClone();
        return document.ToJsonString();
    }

    public static TheoryData<string> CrawfordCubeDocuments =>
    [
        RowDocument(("Roll", 0), ("IsCrawford", true)),
        RowDocument(("IsCrawford", true), ("Roll", 0)),
    ];

    [Theory]
    [MemberData(nameof(CrawfordCubeDocuments))]
    public void Deserialize_CrawfordCube_Throws(string json)
    {
        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json));
    }

    [Theory]
    [MemberData(nameof(CrawfordCubeDocuments))]
    public void Deserialize_CrawfordCube_ThroughContext_Throws(string json)
    {
        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json, ContextOptions));
    }

    public static TheoryData<string> RollAbsentDocuments =>
    [
        RowDocument(("Roll", null)),
        RowDocument(("IsCrawford", true), ("Roll", null)),
    ];

    [Theory]
    [MemberData(nameof(RollAbsentDocuments))]
    public void Deserialize_RollAbsent_Throws(string json)
    {
        // Roll is required on the wire too: the second document is exactly
        // the shape that used to read as a Crawford cube by default.
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json));
    }

    [Theory]
    [MemberData(nameof(RollAbsentDocuments))]
    public void Deserialize_RollAbsent_ThroughContext_Throws(string json)
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json, ContextOptions));
    }

    public static TheoryData<string> CrawfordPlayDocuments =>
    [
        RowDocument(("IsCrawford", true), ("Roll", 31)),
        RowDocument(("Roll", 31), ("IsCrawford", true)),
    ];

    [Theory]
    [MemberData(nameof(CrawfordPlayDocuments))]
    public void Deserialize_CrawfordPlay_Loads(string json)
    {
        var restored = JsonSerializer.Deserialize<DecisionRow>(json)!;

        Assert.True(restored.IsCrawford);
        Assert.False(restored.IsCube);
        Assert.Equal(31, restored.Roll);
    }

    [Fact]
    public void Deserialize_NonCrawfordCube_Loads()
    {
        var restored = JsonSerializer.Deserialize<DecisionRow>(
            RowDocument(("Roll", 0), ("IsCrawford", false)))!;

        Assert.True(restored.IsCube);
        Assert.False(restored.IsCrawford);
    }
}
