using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The Crawford rule on the flat row (halheinrich/backgammon#201): a cube row
/// flagged Crawford describes a decision that cannot exist. The row is a
/// projection of a record now (<see cref="DecisionRow.From"/>), and a Crawford
/// cube record cannot be built, so no row can be projected from one; a
/// document holding one is refused on read with a
/// <see cref="JsonException"/>, on both paths, in any property order. The row
/// states its kind (<see cref="DecisionRow.Kind"/>), so cube is no longer its
/// default kind, and the not-yet-stated roll sentinel the old guards needed
/// is gone.
/// </summary>
public class DecisionRowCrawfordCubeTests
{
    private static PositionData Crawford() =>
        TestRecords.Position(session: TestRecords.MatchSession(onRollNeeds: 1, opponentNeeds: 3, isCrawford: true));

    // The same standing outside the Crawford game: a cube may be made there.
    private static PositionData PostCrawford() =>
        TestRecords.Position(session: TestRecords.MatchSession(onRollNeeds: 1, opponentNeeds: 3));

    private static bool? IsCrawford(DecisionRow row) => (row.Session as MatchSession)?.IsCrawford;

    // ---------------------------------------------------------------------
    //  Construction — through the one door, the record
    // ---------------------------------------------------------------------

    [Fact]
    public void CrawfordCube_CannotBeProjected_BecauseItsRecordCannotBeBuilt()
    {
        // Rewritten from CrawfordCube_IsCrawfordSetSecond_ThrowsNamingIsCrawford
        // and CrawfordCube_RollSetSecond_ThrowsNamingRoll: a row is built from
        // a record, and the record refuses the Crawford cube (naming Position),
        // so there is no row-side initializer order left to guard.
        var ex = Assert.Throws<ArgumentException>(() => TestRecords.Cube(position: Crawford()));

        Assert.Equal("Position", ex.ParamName);
    }

    [Fact]
    public void CrawfordPlay_Projects()
    {
        // Rewritten from CrawfordPlay_Constructs_InEitherOrder.
        var row = TestRecords.Row(TestRecords.CheckerPlay(position: Crawford()));

        Assert.True(IsCrawford(row));
        Assert.Equal(DecisionKind.CheckerPlay, row.Kind);
    }

    [Fact]
    public void NonCrawfordCube_Projects()
    {
        // Rewritten from NonCrawfordCube_Constructs_InEitherOrder.
        var row = TestRecords.Row(TestRecords.Cube(
            position: PostCrawford()));

        Assert.False(IsCrawford(row));
        Assert.Equal(DecisionKind.Cube, row.Kind);
    }

    // ---------------------------------------------------------------------
    //  The wire — read back whole, in any property order, on both paths
    // ---------------------------------------------------------------------

    /// <summary>
    /// A cube row with its Crawford flag set, the two members first in the
    /// order given. Rewritten from the Roll-and-IsCrawford-first documents:
    /// the kind is its own column now.
    /// </summary>
    private static string CrawfordCubeDocument(bool kindFirst)
    {
        var document = WirePaths.Document(TestRecords.Row(TestRecords.Cube(position: PostCrawford())));
        var kind = document["Kind"]!.DeepClone();
        document.Remove("Kind");
        document.Remove("IsCrawford");
        JsonNode crawford = true;
        if (kindFirst)
        {
            document.Insert(0, "Kind", kind);
            document.Insert(1, "IsCrawford", crawford);
        }
        else
        {
            document.Insert(0, "IsCrawford", crawford);
            document.Insert(1, "Kind", kind);
        }
        return document.ToJsonString();
    }

    public static TheoryData<bool> KindFirst => [true, false];

    [Theory]
    [MemberData(nameof(KindFirst))]
    public void Deserialize_CrawfordCube_IsRefused_BothPaths(bool kindFirst)
    {
        // Rewritten from Deserialize_CrawfordCube_Throws and
        // Deserialize_CrawfordCube_ThroughContext_Throws: the refusal is a
        // JsonException naming the rule, whatever order the members come in.
        // The cube sits at 1-away to 3-away, where a Crawford game is
        // possible, so what refuses it is the cube rule, not the standing's.
        var ex = WirePaths.AssertRefused<DecisionRow>(CrawfordCubeDocument(kindFirst));

        Assert.Equal(DecisionRules.CrawfordMessage, ex.Message);
    }

    [Fact]
    public void Deserialize_KindAbsent_IsRefused_BothPaths()
    {
        // Rewritten from Deserialize_RollAbsent_Throws and
        // Deserialize_RollAbsent_ThroughContext_Throws: the kind is the
        // stated column now, required, so a row without it is refused rather
        // than read as either kind. (A checker row without its roll is
        // refused too: DecisionRowSerializationTests.)
        var document = WirePaths.Document(TestRecords.Row(TestRecords.CheckerPlay(position: Crawford())));
        document.Remove("Kind");

        WirePaths.AssertRefused<DecisionRow>(document.ToJsonString());
    }

    [Fact]
    public void Deserialize_CrawfordPlay_Loads_BothPaths()
    {
        // Rewritten from Deserialize_CrawfordPlay_Loads.
        var row = TestRecords.Row(TestRecords.CheckerPlay(position: Crawford()));

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = JsonSerializer.Deserialize<DecisionRow>(JsonSerializer.Serialize(row, options), options)!;
            Assert.True(IsCrawford(restored));
            Assert.Equal(DecisionKind.CheckerPlay, restored.Kind);
        }
    }

    [Fact]
    public void Deserialize_NonCrawfordCube_Loads_BothPaths()
    {
        // Rewritten from Deserialize_NonCrawfordCube_Loads.
        var row = TestRecords.Row(TestRecords.Cube(position: TestRecords.Position(session: TestRecords.MatchSession(onRollNeeds: 1, opponentNeeds: 1))));

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = JsonSerializer.Deserialize<DecisionRow>(JsonSerializer.Serialize(row, options), options)!;
            Assert.False(IsCrawford(restored));
            Assert.Equal(DecisionKind.Cube, restored.Kind);
        }
    }
}
