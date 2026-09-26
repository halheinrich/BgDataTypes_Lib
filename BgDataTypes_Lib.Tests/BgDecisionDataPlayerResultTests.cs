using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the filter view's <see cref="IDecisionFilterData.PlayerResult"/>
/// (<see cref="BgDecisionData.ViewFor"/>) for each kind, under each ranking,
/// each case by name. A cube decision's is the doubler's result or, when the
/// record holds none, the taker's — as <c>UserDoubleError ?? UserTakeError</c>
/// reads — whatever the ranking: a stated action scored with its derived
/// error, an unstated one unlisted with the analyser's. A checker play's is
/// the ranking's <see cref="RankedPlays.PlayerResult"/>. The records here rank
/// alike under both rankings; the case only depth first has, not scored, is
/// pinned in <see cref="PlayRankingTests"/>.
///
/// Renamed from BgDecisionDataFilterErrorTests when the view's error became
/// a result naming its case (the umbrella's third-round ruling). Originally
/// reconstructed from two cases dropped when XgFilter_Lib's filter suite
/// consolidated onto DecisionFilterAsserts; the logic they covered lives on
/// <see cref="BgDecisionData"/>, so the tests belong here.
/// </summary>
public class BgDecisionDataPlayerResultTests
{
    /// <summary>The record's filter view under each ranking.</summary>
    private static IEnumerable<IDecisionFilterData> Views(BgDecisionData record) =>
        Enum.GetValues<PlayRanking>().Select(record.ViewFor);

    /// <summary>The result's error, which the test has established it has.</summary>
    private static double ErrorOf(PlayerResult result) =>
        result.TryGetError(out double error) ? error : throw new InvalidOperationException($"{result} has no error.");

    /// <summary>
    /// A too-good position the user doubled and the opponent took: no double
    /// +1.042 against a cash of 1 is a doubling error of 0.042, and
    /// double/take +1.017 is a take error of 0.017.
    /// </summary>
    private static CubeDecisionData TooGoodDoubledAndTaken(CubeAction? doubled = CubeAction.Double) =>
        TestRecords.CubeData(
            noDoubleEquity: 1.042, doubleTakeEquity: 1.017,
            userDoublerAction: doubled, userTakerAction: CubeAction.Take);

    [Fact]
    public void Cube_AStatedDouble_IsScoredWithTheDoublingError()
    {
        // Rewritten: the errors are no longer stated beside the actions they
        // score; they are derived from the stated actions and the equities.
        var d = TestRecords.Cube(decision: TooGoodDoubledAndTaken());

        // Doubling error present → it wins over the take error.
        Assert.All(Views(d), view =>
        {
            Assert.Equal(PlayerResultKind.Scored, view.PlayerResult.Kind);
            Assert.Equal(0.042, ErrorOf(view.PlayerResult), 12);
        });
        Assert.Equal(0.017, d.Decision.UserTakeError!.Value, 12);
    }

    [Fact]
    public void Cube_NoDoublerResult_FallsBackToTheTakersScoredError()
    {
        // Rewritten: no doubler action stated and no unstated-action error →
        // no doubler result; the taker's stated take is scored.
        var d = TestRecords.Cube(decision: TooGoodDoubledAndTaken(doubled: null));

        Assert.All(Views(d), view =>
        {
            Assert.Equal(PlayerResultKind.Scored, view.PlayerResult.Kind);
            Assert.Equal(0.017, ErrorOf(view.PlayerResult), 12);
        });
    }

    [Theory]
    [InlineData(0.042, null)]
    [InlineData(null, 0.013)]
    public void Cube_AnUnstatedActionsError_IsUnlisted_TheAnalysersNumber(double? doubler, double? taker)
    {
        // Added: where the record states no action, the analyser's error is
        // the only statement of it — the doubler's, or failing that the
        // taker's — and the view names it unlisted.
        var d = TestRecords.Cube(decision: TestRecords.CubeData(
            userDoublerAction: null, userTakerAction: null,
            unstatedDoublerActionError: doubler, unstatedTakerActionError: taker));

        Assert.All(Views(d), view => Assert.Equal(PlayerResult.Unlisted((doubler ?? taker)!.Value), view.PlayerResult));
    }

    [Fact]
    public void Checker_AScoredPlay_IsScoredWithItsError()
    {
        // Rewritten: the user played the default's second candidate, 13/10
        // 6/5, whose equity loss against the best is 0.1604 − (−0.0127).
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 1));

        Assert.All(Views(d), view =>
        {
            Assert.Equal(PlayerResultKind.Scored, view.PlayerResult.Kind);
            Assert.Equal(0.1731, ErrorOf(view.PlayerResult), 12);
        });
    }

    [Fact]
    public void Checker_AnUnlistedPlay_IsUnlisted_TheAnalysersNumber()
    {
        // Added: a user play outside the candidates has no equity here to
        // derive from; the analyser's number is its one statement.
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            userPlayIndex: null, unlistedPlayError: 0.031));

        Assert.All(Views(d), view => Assert.Equal(PlayerResult.Unlisted(0.031), view.PlayerResult));
    }

    [Fact]
    public void NoUserDecisionRecorded_IsNotRecorded_EachKind()
    {
        // Added: "none recorded" is its own case on both kinds, with no error
        // — never a zero, and never confused with a move that is not scored.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null));
        var cube = TestRecords.Cube(decision: TestRecords.CubeData(userDoublerAction: null, userTakerAction: null));

        Assert.All(Views(play).Concat(Views(cube)), view =>
        {
            Assert.Equal(PlayerResult.NotRecorded, view.PlayerResult);
            Assert.False(view.PlayerResult.TryGetError(out _));
        });
    }

    [Fact]
    public void ACubeDecision_IsNeverNotScored()
    {
        // No ranking scores a cube decision; every case it has is one of the
        // other three, over each shape of what the record states.
        CubeDecisionData[] shapes =
        [
            TestRecords.CubeData(),
            TestRecords.CubeData(userDoublerAction: null),
            TestRecords.CubeData(userDoublerAction: null, userTakerAction: null),
            TestRecords.CubeData(userDoublerAction: null, userTakerAction: null, unstatedDoublerActionError: 0.1),
            TestRecords.CubeData(userDoublerAction: CubeAction.NoDouble, userTakerAction: null),
        ];

        foreach (var shape in shapes)
            Assert.All(Views(TestRecords.Cube(decision: shape)),
                view => Assert.NotEqual(PlayerResultKind.NotScored, view.PlayerResult.Kind));
    }
}
