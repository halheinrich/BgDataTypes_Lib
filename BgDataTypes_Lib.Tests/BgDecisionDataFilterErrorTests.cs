using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins <see cref="BgDecisionData.FilterError"/> for cube decisions, where the
/// value is <c>UserDoubleError ?? UserTakeError</c> — the doubling error if one
/// was recorded, otherwise the take/drop error. (Checker decisions read
/// <c>UserPlayError</c>; the last case guards that branch.)
///
/// Reconstructed from two cases dropped when XgFilter_Lib's filter suite
/// consolidated onto DecisionFilterAsserts; the logic they covered now lives on
/// <see cref="BgDecisionData"/>, so the tests belong here.
/// </summary>
public class BgDecisionDataFilterErrorTests
{
    // Id is required on BgDecisionData — an unset identifier is meant to surface
    // at construction, so every fixture supplies one even though FilterError
    // never reads it.
    private static readonly DecisionId AnyId = new XgpDecisionId("x.xgp");

    [Fact]
    public void FilterError_Cube_UsesDoubleError()
    {
        var d = new BgDecisionData
        {
            Id = AnyId,
            Decision = new DecisionData
            {
                IsCube = true,
                UserDoubleError = 0.042,
                UserTakeError = 0.017,
            },
        };

        // Doubling error present → it wins over the take error.
        Assert.Equal(0.042, d.FilterError);
    }

    [Fact]
    public void FilterError_Cube_FallsBackToTakeError()
    {
        var d = new BgDecisionData
        {
            Id = AnyId,
            Decision = new DecisionData
            {
                IsCube = true,
                UserDoubleError = null,
                UserTakeError = 0.017,
            },
        };

        // No doubling error → fall back to the take/drop error.
        Assert.Equal(0.017, d.FilterError);
    }

    [Fact]
    public void FilterError_Checker_UsesPlayError()
    {
        var d = new BgDecisionData
        {
            Id = AnyId,
            Decision = new DecisionData
            {
                IsCube = false,
                UserPlayError = 0.031,
            },
        };

        // Checker decision → the cube-error fields are irrelevant; read the play error.
        Assert.Equal(0.031, d.FilterError);
    }
}
