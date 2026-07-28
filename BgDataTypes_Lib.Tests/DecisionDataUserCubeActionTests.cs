using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class DecisionDataUserCubeActionTests
{
    // The played-cube-action halves are guarded to their own action domains,
    // mirroring CubeDecisionPair's half-guards: UserDoublerAction admits
    // NoDouble / Double (or null), UserTakerAction admits Take / Pass (or
    // null). Cross-half consistency between the two is a producer contract
    // and deliberately not guarded — see the section comment in DecisionData.

    // ---------------------------------------------------------------------
    //  Valid domains — every in-domain value (including null) is accepted
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    public void UserDoublerAction_DoublerHalfOrNull_Accepted(CubeAction? action)
    {
        var d = new DecisionData { IsCube = true, UserDoublerAction = action };

        Assert.Equal(action, d.UserDoublerAction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void UserTakerAction_TakerHalfOrNull_Accepted(CubeAction? action)
    {
        var d = new DecisionData { IsCube = true, UserTakerAction = action };

        Assert.Equal(action, d.UserTakerAction);
    }

    // ---------------------------------------------------------------------
    //  Half-guards — a cross-half value throws on init
    // ---------------------------------------------------------------------

    [Theory]
    // The doubler half rejects taker-only actions.
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void UserDoublerAction_NonDoublerAction_Throws(CubeAction takerAction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DecisionData { IsCube = true, UserDoublerAction = takerAction });
    }

    [Theory]
    // The taker half rejects doubler-only actions.
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    public void UserTakerAction_NonTakerAction_Throws(CubeAction doublerAction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DecisionData { IsCube = true, UserTakerAction = doublerAction });
    }

    // ---------------------------------------------------------------------
    //  Half-guards on the wire — a cross-half JSON value fails loud
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("{\"IsCube\":true,\"UserDoublerAction\":\"Take\"}")]
    [InlineData("{\"IsCube\":true,\"UserTakerAction\":\"Double\"}")]
    public void Deserialize_CrossHalfValue_Throws(string json)
    {
        // The init guards run during deserialization too: corrupt wire data
        // surfaces at read time rather than as a silently-carried invalid
        // action.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => JsonSerializer.Deserialize<DecisionData>(json));
    }
}
