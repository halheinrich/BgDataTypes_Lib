using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The Crawford-cube invariant on the composite record
/// (halheinrich/backgammon#201): doubling is prohibited in the Crawford
/// game, so a record whose <see cref="PositionData.IsCrawford"/> and
/// <see cref="DecisionData.IsCube"/> are both true describes a decision
/// that cannot exist, and the record refuses to be constructed. The guard
/// sits in both init setters so whichever half is set second fires — an
/// object initializer in either member order and a JSON document in either
/// property order all fail the same way. The <c>UserDoublerAction</c>
/// rejection tests are the pattern.
/// </summary>
public class BgDecisionDataCrawfordCubeTests
{
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    private static PositionData Crawford() =>
        TestRecords.Position(onRollNeeds: 1, opponentNeeds: 3, isCrawford: true);

    private static PositionData NotCrawford() =>
        TestRecords.Position(onRollNeeds: 1, opponentNeeds: 3);

    private static DecisionData Cube() => TestRecords.Decision(isCube: true);

    private static DecisionData Play() => TestRecords.Decision(isCube: false, dice: [3, 1]);

    // ---------------------------------------------------------------------
    //  Object initializers — whichever half completes the Crawford cube
    //  is the one named
    // ---------------------------------------------------------------------

    [Fact]
    public void CrawfordCube_DecisionSetSecond_ThrowsNamingDecision()
    {
        var ex = Assert.Throws<ArgumentException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Position = Crawford(),
            Decision = Cube(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        });

        Assert.Equal("Decision", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordCube_PositionSetSecond_ThrowsNamingPosition()
    {
        var ex = Assert.Throws<ArgumentException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Decision = Cube(),
            Position = Crawford(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        });

        Assert.Equal("Position", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordPlay_Constructs_InEitherOrder()
    {
        var positionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Position = Crawford(),
            Decision = Play(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        };
        var decisionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Decision = Play(),
            Position = Crawford(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        };

        Assert.True(positionFirst.IsCrawford);
        Assert.False(positionFirst.IsCube);
        Assert.True(decisionFirst.IsCrawford);
        Assert.False(decisionFirst.IsCube);
    }

    [Fact]
    public void NonCrawfordCube_Constructs_InEitherOrder()
    {
        var positionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Position = NotCrawford(),
            Decision = Cube(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        };
        var decisionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Decision = Cube(),
            Position = NotCrawford(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        };

        Assert.False(positionFirst.IsCrawford);
        Assert.True(positionFirst.IsCube);
        Assert.False(decisionFirst.IsCrawford);
        Assert.True(decisionFirst.IsCube);
    }

    [Fact]
    public void HalfSetRecord_IsRefused_OnBothPaths()
    {
        // Rewritten from HalfSetRecord_NeverThrows. A record with one half
        // set used to load with the other at its default ("not cube, not
        // Crawford"), so it could never complete the forbidden pair. Both
        // halves are required now (halheinrich/backgammon#222): an
        // initializer omitting one does not compile, and a document omitting
        // one is refused as absent, not read as a default half.
        string crawfordOnly = Document(("Position", Json(Crawford())));
        string cubeOnly = Document(("Decision", Json(Cube())));

        foreach (var json in new[] { crawfordOnly, cubeOnly })
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BgDecisionData>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BgDecisionData>(json, ContextOptions));
        }
    }

    // ---------------------------------------------------------------------
    //  The wire — the same guards run during deserialization, in either
    //  property order, through the reflection path and the source-generated
    //  context alike
    // ---------------------------------------------------------------------

    /// <summary>
    /// A composite document holding <paramref name="halves"/> in the order
    /// given, then the other stored members. Rewritten from literal JSON:
    /// every member of the halves is required, so each half is a full
    /// serialized category.
    /// </summary>
    private static string Document(params (string Name, string Json)[] halves)
    {
        var members = new List<string> { "\"Id\":\"test.xgp\"", "\"Xgid\":\"\"" };
        foreach (var (name, json) in halves)
            members.Add($"\"{name}\":{json}");
        members.Add($"\"Descriptive\":{Json(TestRecords.Descriptive())}");
        members.Add($"\"Outcome\":{Json(TestRecords.Outcome())}");
        return "{" + string.Join(",", members) + "}";
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    public static TheoryData<string> CrawfordCubeDocuments =>
    [
        Document(("Position", Json(Crawford())), ("Decision", Json(Cube()))),
        Document(("Decision", Json(Cube())), ("Position", Json(Crawford()))),
    ];

    [Theory]
    [MemberData(nameof(CrawfordCubeDocuments))]
    public void Deserialize_CrawfordCube_Throws(string json)
    {
        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json));
    }

    [Theory]
    [MemberData(nameof(CrawfordCubeDocuments))]
    public void Deserialize_CrawfordCube_ThroughContext_Throws(string json)
    {
        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json, ContextOptions));
    }

    public static TheoryData<string> OneFactDocuments =>
    [
        Document(("Position", Json(Crawford())), ("Decision", Json(Play()))),
        Document(("Position", Json(NotCrawford())), ("Decision", Json(Cube()))),
    ];

    [Theory]
    [MemberData(nameof(OneFactDocuments))]
    public void Deserialize_OneFactOnly_Loads(string json)
    {
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json)!;

        Assert.NotEqual(restored.IsCrawford, restored.IsCube);
    }
}
