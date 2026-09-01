using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

// The claim vocabulary of halheinrich/backgammon#86 (SPEC-scoring §1/§3).
public class CubeClaimTests
{
    // No explicit enum-converter registration: CubeClaim bundles its own
    // [JsonConverter(typeof(StrictJsonStringEnumConverter<CubeClaim>))]
    // attribute, so removing it from the type fails this suite loudly
    // (rather than silently passing because an option-level registration
    // covered for it). Same discipline as CubeActionTests.

    [Fact]
    public void HasExactlyThreeMembers()
    {
        Assert.Equal(3, Enum.GetValues<CubeClaim>().Length);
    }

    // Declaration order is the ruled claim axis {No Double, Double, Too Good}
    // (SPEC-scoring §3) — what a UI offering the claims renders.
    [Fact]
    public void MembersAreInExpectedOrder()
    {
        Assert.Equal(0, (int)CubeClaim.NoDouble);
        Assert.Equal(1, (int)CubeClaim.Double);
        Assert.Equal(2, (int)CubeClaim.TooGood);
    }

    [Theory]
    [InlineData(CubeClaim.NoDouble, "\"NoDouble\"")]
    [InlineData(CubeClaim.Double, "\"Double\"")]
    [InlineData(CubeClaim.TooGood, "\"TooGood\"")]
    public void Serializes_AsString(CubeClaim claim, string expectedJson)
    {
        Assert.Equal(expectedJson, JsonSerializer.Serialize(claim));
    }

    [Theory]
    [InlineData(CubeClaim.NoDouble)]
    [InlineData(CubeClaim.Double)]
    [InlineData(CubeClaim.TooGood)]
    public void RoundTrips_ThroughJson(CubeClaim claim)
    {
        var json = JsonSerializer.Serialize(claim);
        Assert.Equal(claim, JsonSerializer.Deserialize<CubeClaim>(json));
    }

    // ---------------------------------------------------------------------
    //  ToCubeAction — the claim-to-action collapse, single-sourced
    // ---------------------------------------------------------------------

    // Both no-double claims perform the identical board action: the claim
    // layer's defining collapse (SPEC-scoring §3, "Too Good is claim-layer
    // only").
    [Theory]
    [InlineData(CubeClaim.NoDouble, CubeAction.NoDouble)]
    [InlineData(CubeClaim.TooGood, CubeAction.NoDouble)]
    [InlineData(CubeClaim.Double, CubeAction.Double)]
    public void ToCubeAction_CollapsesClaimToBoardAction(CubeClaim claim, CubeAction expected)
    {
        Assert.Equal(expected, claim.ToCubeAction());
    }

    [Fact]
    public void ToCubeAction_OnUndefinedClaim_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((CubeClaim)99).ToCubeAction());
    }
}
