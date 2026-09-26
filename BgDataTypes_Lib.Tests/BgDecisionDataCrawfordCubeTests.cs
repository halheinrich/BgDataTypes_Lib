using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The Crawford rule on the record (halheinrich/backgammon#201): doubling is
/// prohibited in the Crawford game, so a cube decision in a Crawford position
/// describes a decision that cannot exist, and the record refuses to be
/// constructed. The kind is the record's type now, fixed before any member
/// is set, so the rule is one guard on <see cref="BgDecisionData.Position"/>:
/// it fires whatever order the members are set in, from an object
/// initializer (an <see cref="ArgumentException"/> naming Position) and from
/// a document in any property order (a <see cref="JsonException"/> on both
/// paths, carrying it).
/// </summary>
public class BgDecisionDataCrawfordCubeTests
{
    private static PositionData Crawford() =>
        TestRecords.Position(onRollNeeds: 1, opponentNeeds: 3, isCrawford: true);

    private static PositionData NotCrawford() =>
        TestRecords.Position(onRollNeeds: 1, opponentNeeds: 3);

    // ---------------------------------------------------------------------
    //  Object initializers
    // ---------------------------------------------------------------------

    [Fact]
    public void CrawfordCube_PositionSetAfterDecision_ThrowsNamingPosition()
    {
        // Rewritten from CrawfordCube_PositionSetSecond_ThrowsNamingPosition.
        var ex = Assert.Throws<ArgumentException>(() => new CubeDecision
        {
            Id = new XgDecisionId("m.xg", 1, 2, IsCube: true),
            Xgid = "XGID=x",
            Decision = TestRecords.CubeData(),
            Position = Crawford(),
            Descriptive = TestRecords.Descriptive(),
        });

        Assert.Equal("Position", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordCube_PositionSetBeforeDecision_ThrowsNamingPosition()
    {
        // Rewritten from CrawfordCube_DecisionSetSecond_ThrowsNamingDecision:
        // the other half no longer completes the contradiction — the kind is
        // the type, known before Position is set — so Position is named in
        // this order too.
        var ex = Assert.Throws<ArgumentException>(() => new CubeDecision
        {
            Id = new XgDecisionId("m.xg", 1, 2, IsCube: true),
            Xgid = "XGID=x",
            Position = Crawford(),
            Decision = TestRecords.CubeData(),
            Descriptive = TestRecords.Descriptive(),
        });

        Assert.Equal("Position", ex.ParamName);
    }

    [Fact]
    public void CrawfordPlay_Constructs()
    {
        // Rewritten from CrawfordPlay_Constructs_InEitherOrder.
        var play = TestRecords.CheckerPlay(position: Crawford());

        Assert.True(play.IsCrawford);
        Assert.Equal(DecisionKind.CheckerPlay, play.Kind);
    }

    [Fact]
    public void NonCrawfordCube_Constructs()
    {
        // Rewritten from NonCrawfordCube_Constructs_InEitherOrder.
        var cube = TestRecords.Cube(position: NotCrawford());

        Assert.False(cube.IsCrawford);
        Assert.Equal(DecisionKind.Cube, cube.Kind);
    }

    // ---------------------------------------------------------------------
    //  The wire — the same guard runs during deserialization, in any
    //  property order, and the refusal is a JsonException on both paths
    // ---------------------------------------------------------------------

    /// <summary>
    /// A cube document with a Crawford position, its <c>Position</c> member
    /// placed first or last. Rewritten from the halves-in-either-order
    /// documents: only Position can complete the contradiction now.
    /// </summary>
    public static TheoryData<bool> PositionFirst => [true, false];

    private static string CrawfordCubeDocument(bool positionFirst)
    {
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube(position: NotCrawford()));
        document.Remove("Position");
        var crawford = JsonSerializer.SerializeToNode(Crawford(), WirePaths.Context)!;
        if (positionFirst)
            document.Insert(0, "Position", crawford);
        else
            document.Add("Position", crawford);
        return document.ToJsonString();
    }

    [Theory]
    [MemberData(nameof(PositionFirst))]
    public void Deserialize_CrawfordCube_IsRefused_BothPaths(bool positionFirst)
    {
        // Rewritten from Deserialize_CrawfordCube_Throws and
        // Deserialize_CrawfordCube_ThroughContext_Throws: a malformed
        // document is a JsonException now, the guard's refusal inside it.
        var ex = WirePaths.AssertRefused<BgDecisionData>(CrawfordCubeDocument(positionFirst));

        var guard = Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Equal("Position", guard.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void Deserialize_CrawfordPlayAndNonCrawfordCube_Load_BothPaths()
    {
        // Rewritten from Deserialize_OneFactOnly_Loads.
        foreach (var (_, options) in WirePaths.Both)
        {
            var play = WirePaths.RoundTrip<BgDecisionData>(TestRecords.CheckerPlay(position: Crawford()), options);
            var cube = WirePaths.RoundTrip<BgDecisionData>(TestRecords.Cube(position: NotCrawford()), options);

            Assert.True(play.IsCrawford);
            Assert.IsType<CheckerPlayDecision>(play);
            Assert.False(cube.IsCrawford);
            Assert.IsType<CubeDecision>(cube);
        }
    }

    [Fact]
    public void Deserialize_CubeWithoutPosition_IsRefused_BothPaths()
    {
        // Rewritten from HalfSetRecord_IsRefused_OnBothPaths: a member absent
        // is refused as absent, never read as a default that could dodge the
        // rule.
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document.Remove("Position");

        WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
    }
}
