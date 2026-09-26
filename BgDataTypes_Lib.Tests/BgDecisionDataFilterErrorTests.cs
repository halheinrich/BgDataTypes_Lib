using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins <see cref="BgDecisionData.FilterError"/> for each kind: a cube
/// decision's is <c>UserDoubleError ?? UserTakeError</c> — the doubling error
/// if one was recorded, otherwise the take/drop error — and a checker play's
/// is its <c>UserPlayError</c>.
///
/// Reconstructed from two cases dropped when XgFilter_Lib's filter suite
/// consolidated onto DecisionFilterAsserts; the logic they covered now lives on
/// <see cref="BgDecisionData"/>, so the tests belong here.
/// </summary>
public class BgDecisionDataFilterErrorTests
{
    [Fact]
    public void FilterError_Cube_UsesDoubleError()
    {
        var d = TestRecords.Cube(decision: TestRecords.CubeData(
            userDoubleError: 0.042,
            userTakeError: 0.017));

        // Doubling error present → it wins over the take error.
        Assert.Equal(0.042, d.FilterError);
    }

    [Fact]
    public void FilterError_Cube_FallsBackToTakeError()
    {
        var d = TestRecords.Cube(decision: TestRecords.CubeData(
            userDoubleError: null,
            userTakeError: 0.017));

        // No doubling error → fall back to the take/drop error.
        Assert.Equal(0.017, d.FilterError);
    }

    [Fact]
    public void FilterError_Checker_UsesPlayError()
    {
        var d = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayError: 0.031));

        // Rewritten: a checker play has no cube-error members at all, so there
        // is nothing irrelevant to ignore — it reads its play error.
        Assert.Equal(0.031, d.FilterError);
    }

    [Fact]
    public void FilterError_NoUserDecisionRecorded_IsNull_EachKind()
    {
        // Added: "none recorded" is null on both kinds, never a zero.
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null, userPlayError: null));
        var cube = TestRecords.Cube(decision: TestRecords.CubeData(userDoubleError: null, userTakeError: null));

        Assert.Null(play.FilterError);
        Assert.Null(cube.FilterError);
    }
}
