using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the filter view's <see cref="IDecisionFilterData.FilterError"/>
/// (<see cref="BgDecisionData.ViewFor"/>) for each kind, under each ranking: a
/// cube decision's is <c>UserDoubleError ?? UserTakeError</c> — the doubling
/// error if there is one, otherwise the take/pass error — whatever the
/// ranking; a checker play's is the ranking's
/// <see cref="RankedPlays.UserPlayError"/>. Each of those errors is derived
/// where the record determines it (the error of a stated action, of a
/// candidate the user played), and stored only where it does not. The
/// records here rank alike under both rankings; where the rankings differ is
/// pinned in <see cref="PlayRankingTests"/>.
///
/// Reconstructed from two cases dropped when XgFilter_Lib's filter suite
/// consolidated onto DecisionFilterAsserts; the logic they covered now lives on
/// <see cref="BgDecisionData"/>, so the tests belong here.
/// </summary>
public class BgDecisionDataFilterErrorTests
{
    /// <summary>The record's filter view under each ranking.</summary>
    private static IEnumerable<IDecisionFilterData> Views(BgDecisionData record) =>
        Enum.GetValues<PlayRanking>().Select(record.ViewFor);

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
    public void FilterError_Cube_UsesDoubleError()
    {
        // Rewritten: the errors are no longer stated beside the actions they
        // score; they are derived from the stated actions and the equities.
        var d = TestRecords.Cube(decision: TooGoodDoubledAndTaken());

        // Doubling error present → it wins over the take error.
        Assert.All(Views(d), view => Assert.Equal(0.042, view.FilterError!.Value, 12));
        Assert.Equal(0.017, d.Decision.UserTakeError!.Value, 12);
    }

    [Fact]
    public void FilterError_Cube_FallsBackToTakeError()
    {
        // Rewritten: no doubler action stated and no unstated-action error →
        // no doubling error.
        var d = TestRecords.Cube(decision: TooGoodDoubledAndTaken(doubled: null));

        // No doubling error → fall back to the take/pass error.
        Assert.All(Views(d), view => Assert.Equal(0.017, view.FilterError!.Value, 12));
    }

    [Fact]
    public void FilterError_Cube_UnstatedActionsError_IsTheAnalysersNumber()
    {
        // Added: where the record states no action, the analyser's error is
        // the only statement of it, and the filter reads it.
        var d = TestRecords.Cube(decision: TestRecords.CubeData(
            userDoublerAction: null, userTakerAction: null, unstatedDoublerActionError: 0.042));

        Assert.All(Views(d), view => Assert.Equal(0.042, view.FilterError));
    }

    [Fact]
    public void FilterError_Checker_UsesPlayError()
    {
        // Rewritten: the user played the default's second candidate, 13/10
        // 6/5, whose equity loss against the best is 0.1604 − (−0.0127).
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 1));

        Assert.All(Views(d), view => Assert.Equal(0.1731, view.FilterError!.Value, 12));
    }

    [Fact]
    public void FilterError_Checker_UnlistedPlaysError_IsTheAnalysersNumber()
    {
        // Added: a user play outside the candidates has no equity here to
        // derive from; the analyser's number is its one statement.
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(
            userPlayIndex: null, unlistedPlayError: 0.031));

        Assert.All(Views(d), view => Assert.Equal(0.031, view.FilterError));
    }

    [Fact]
    public void FilterError_NoUserDecisionRecorded_IsNull_EachKind()
    {
        // Added: "none recorded" is null on both kinds, never a zero.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null));
        var cube = TestRecords.Cube(decision: TestRecords.CubeData(userDoublerAction: null, userTakerAction: null));

        Assert.All(Views(play), view => Assert.Null(view.FilterError));
        Assert.All(Views(cube), view => Assert.Null(view.FilterError));
    }
}
