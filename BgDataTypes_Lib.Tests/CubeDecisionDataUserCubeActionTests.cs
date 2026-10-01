using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class CubeDecisionDataUserCubeActionTests
{
    // The played-cube-action halves are guarded to their own action domains,
    // the halves CubeAnswerExtensions.Of accepts: UserDoublerAction admits
    // NoDouble / Double (or null), UserTakerAction admits Take / Pass (or
    // null). Cross-half consistency between the two is a producer contract
    // and deliberately not guarded — see the section comment in CubeDecisionData.

    // ---------------------------------------------------------------------
    //  Valid domains — every in-domain value (including null) is accepted
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    public void UserDoublerAction_DoublerHalfOrNull_Accepted(CubeAction? action)
    {
        var d = TestRecords.CubeData(userDoublerAction: action);

        Assert.Equal(action, d.UserDoublerAction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void UserTakerAction_TakerHalfOrNull_Accepted(CubeAction? action)
    {
        var d = TestRecords.CubeData(userTakerAction: action);

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
            () => TestRecords.CubeData(userDoublerAction: takerAction));
    }

    [Theory]
    // The taker half rejects doubler-only actions.
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    public void UserTakerAction_NonTakerAction_Throws(CubeAction doublerAction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TestRecords.CubeData(userTakerAction: doublerAction));
    }

    // ---------------------------------------------------------------------
    //  Half-guards on the wire — a cross-half JSON value fails loud
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("UserDoublerAction", "Take")]
    [InlineData("UserTakerAction", "Double")]
    public void Deserialize_CrossHalfValue_IsRefused_BothPaths(string member, string action)
    {
        // Rewritten from Deserialize_CrossHalfValue_Throws: the init guards
        // run during deserialization, so corrupt wire data surfaces at read
        // time rather than as a silently-carried invalid action — and read
        // from a document, the category refuses it as a JsonException
        // carrying the guard's exception, not as the exception itself. The
        // document is now a whole category with one half corrupted, rather
        // than a fragment, so the half-guard is what refuses it.
        var document = WirePaths.Document(TestRecords.CubeData());
        document[member] = action;

        var ex = WirePaths.AssertRefused<CubeDecisionData>(document.ToJsonString());
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    [Theory]
    [InlineData("UserDoublerAction", "Take")]
    [InlineData("UserTakerAction", "Double")]
    public void Deserialize_CrossHalfValueInARecord_IsRefused_BothPaths(string member, string action)
    {
        // Added: read as the wire unit, the guard's refusal is a JsonException.
        var document = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        document["Decision"]![member] = action;

        var ex = WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }
}
