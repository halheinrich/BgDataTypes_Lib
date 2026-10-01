using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

// The claim vocabulary of halheinrich/backgammon#86 (SPEC-scoring §1/§3),
// a reading of the cube answer at a decision since halheinrich/backgammon#326:
// the reading itself is pinned in CubeDecisionClaimOfTests.
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

    // Declaration order is SPEC-scoring §1's claim axis {No Double, Double,
    // Too Good}.
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

    // The claim-to-action collapse is retired with the six-value pair
    // (halheinrich/backgammon#326): an answer's doubling action is its own
    // projection, CubeAnswerExtensions.DoublerAction, and nothing maps a
    // claim to an action or back.
    [Fact]
    public void NoMember_MapsAClaimToAnAction()
    {
        Assert.Null(typeof(CubeClaim).Assembly.GetType("BgDataTypes_Lib.CubeClaimExtensions"));
        Assert.DoesNotContain(
            typeof(CubeClaim).Assembly.GetExportedTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.IsStatic && m.GetParameters() is [{ ParameterType: var p }, ..] && p == typeof(CubeClaim)),
            m => m.ReturnType == typeof(CubeAction));
    }
}
