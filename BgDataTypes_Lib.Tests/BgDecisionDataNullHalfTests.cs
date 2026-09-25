using System.Text.Json;
using System.Text.Json.Nodes;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// <see cref="BgDecisionData.Position"/> and
/// <see cref="BgDecisionData.Decision"/> are declared non-nullable and keep
/// it (halheinrich/backgammon#221): an explicit null — from an initializer
/// or from a JSON document — throws <see cref="ArgumentNullException"/>
/// naming the member at init, through the reflection path and the
/// source-generated context alike. An <em>absent</em> half is a different
/// failure: both halves are required (halheinrich/backgammon#222), so a
/// document without one is a <see cref="JsonException"/> on both paths —
/// where the reflection path once kept the half's default and the context
/// once passed it as <c>default</c>, a null. Before #221 the halves accepted
/// null and <see cref="ProblemKey.TryDerive"/> carried a no-key rung for the
/// resulting record; that rung is gone, since the record cannot exist.
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
            Xgid = "",
            Position = null!,
            Decision = TestRecords.Decision(),
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        });

        Assert.Equal("Position", ex.ParamName);
    }

    [Fact]
    public void Decision_Null_ThrowsNamingDecision()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Xgid = "",
            Position = TestRecords.Position(),
            Decision = null!,
            Descriptive = TestRecords.Descriptive(),
            Outcome = TestRecords.Outcome(),
        });

        Assert.Equal("Decision", ex.ParamName);
    }

    // ---------------------------------------------------------------------
    //  The wire — an explicit null is a malformed document
    // ---------------------------------------------------------------------

    /// <summary>
    /// A full record document with <paramref name="member"/> set to JSON
    /// <c>null</c>, or removed when <paramref name="remove"/> is set.
    /// Rewritten from literal JSON: every other member is required, so the
    /// rest of a full record travels with the half under test.
    /// </summary>
    private static string Document(string member, bool remove = false)
    {
        var document = JsonNode.Parse(JsonSerializer.Serialize(TestRecords.Record()))!.AsObject();
        if (remove)
            document.Remove(member);
        else
            document[member] = null;
        return document.ToJsonString();
    }

    [Theory]
    [InlineData("Position")]
    [InlineData("Decision")]
    public void Deserialize_NullHalf_Throws(string member)
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(Document(member)));

        Assert.Equal(member, ex.ParamName);
    }

    [Theory]
    [InlineData("Position")]
    [InlineData("Decision")]
    public void Deserialize_NullHalf_ThroughContext_Throws(string member)
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(Document(member), ContextOptions));

        Assert.Equal(member, ex.ParamName);
    }

    // ---------------------------------------------------------------------
    //  The wire — an absent half is not a null half: it is refused as
    //  absent, on both paths alike (halheinrich/backgammon#222)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("Position")]
    [InlineData("Decision")]
    public void Deserialize_AbsentHalf_IsRefused_Reflection(string member)
    {
        // Rewritten from Deserialize_AbsentHalves_StayAtDefaults_Reflection:
        // the reflection path used to keep the half's `new()` default.
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(Document(member, remove: true)));
    }

    [Theory]
    [InlineData("Position")]
    [InlineData("Decision")]
    public void Deserialize_AbsentHalf_IsRefused_ThroughContext(string member)
    {
        // Rewritten from Deserialize_AbsentHalf_ThroughContext_ArrivesAsNull_AndThrows:
        // the generated creator used to pass the absent half as default —
        // null — which the null guard then caught. The required check now
        // refuses the document before the creator runs, as absent, not null.
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(Document(member, remove: true), ContextOptions));
    }
}
