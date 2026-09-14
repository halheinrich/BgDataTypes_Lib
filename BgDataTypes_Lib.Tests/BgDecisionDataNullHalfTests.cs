using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// <see cref="BgDecisionData.Position"/> and
/// <see cref="BgDecisionData.Decision"/> are declared non-nullable and keep
/// it (halheinrich/backgammon#221): an explicit null — from an initializer
/// or from a JSON document — throws <see cref="ArgumentNullException"/>
/// naming the member at init, through the reflection path and the
/// source-generated context alike, while an <em>absent</em> half stays at
/// its default and loads on the reflection path. "Absent" and "null" are
/// different things there; through the context they are not, because the
/// generator passes an absent init-only member as <c>default</c> — pinned
/// below as the generator's property. Before this the halves accepted null and
/// <see cref="ProblemKey.TryDerive"/> carried a no-key rung for the
/// resulting record; that rung is gone, since the record can no longer
/// exist.
/// </summary>
public class BgDecisionDataNullHalfTests
{
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    // ---------------------------------------------------------------------
    //  Object initializers
    // ---------------------------------------------------------------------

    [Fact]
    public void Position_Null_ThrowsNamingPosition()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = null!,
        });

        Assert.Equal("Position", ex.ParamName);
    }

    [Fact]
    public void Decision_Null_ThrowsNamingDecision()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = null!,
        });

        Assert.Equal("Decision", ex.ParamName);
    }

    // ---------------------------------------------------------------------
    //  The wire — an explicit null is a malformed document
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":null}", "Position")]
    [InlineData("{\"Id\":\"test.xgp\",\"Decision\":null}", "Decision")]
    public void Deserialize_NullHalf_Throws(string json, string member)
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json));

        Assert.Equal(member, ex.ParamName);
    }

    [Theory]
    // The other half is present here because, through the context, an
    // absent half arrives as null too (below) and would be named first.
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":null,\"Decision\":{}}", "Position")]
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":{},\"Decision\":null}", "Decision")]
    public void Deserialize_NullHalf_ThroughContext_Throws(string json, string member)
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json, ContextOptions));

        Assert.Equal(member, ex.ParamName);
    }

    // ---------------------------------------------------------------------
    //  The wire — an absent half is not a null half on the reflection path.
    //  Through the source-generated context it is: the generated creator is
    //  one object initializer over an argument array, and an absent
    //  init-only member arrives there as default(T) — a property of the
    //  generator for every init-only reference member in this library's
    //  wire graph, not of this guard (measured 2026-09-14; before the guard
    //  the same document loaded with a silently null half). Both are
    //  pinned so a change on either path is noticed.
    // ---------------------------------------------------------------------

    [Fact]
    public void Deserialize_AbsentHalves_StayAtDefaults_Reflection()
    {
        var restored = JsonSerializer.Deserialize<BgDecisionData>("{\"Id\":\"test.xgp\"}")!;

        Assert.NotNull(restored.Position);
        Assert.NotNull(restored.Decision);
        Assert.False(restored.IsCrawford);
        Assert.False(restored.IsCube);
    }

    [Theory]
    // Position is the first half in the generated initializer, so with both
    // absent it is the one named; with Position present, Decision is.
    [InlineData("{\"Id\":\"test.xgp\"}", "Position")]
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":{}}", "Decision")]
    public void Deserialize_AbsentHalf_ThroughContext_ArrivesAsNull_AndThrows(string json, string member)
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json, ContextOptions));

        Assert.Equal(member, ex.ParamName);
    }
}
