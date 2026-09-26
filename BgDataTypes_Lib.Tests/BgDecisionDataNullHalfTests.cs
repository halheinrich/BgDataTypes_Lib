using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A record's categories and identifier are declared non-nullable and keep it
/// (halheinrich/backgammon#221, extended to every member a guard reads): an
/// explicit null — from an initializer or from a JSON document — is refused
/// at init with an <see cref="ArgumentNullException"/> naming the member; read
/// as a <see cref="BgDecisionData"/>, the document is a
/// <see cref="JsonException"/> carrying it, on both paths. An <em>absent</em>
/// member is a different failure: every one is required
/// (halheinrich/backgammon#222), so a document without it is refused as
/// absent, on both paths.
/// </summary>
public class BgDecisionDataNullHalfTests
{
    // ---------------------------------------------------------------------
    //  Object initializers
    // ---------------------------------------------------------------------

    [Fact]
    public void Position_Null_ThrowsNamingPosition()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new CubeDecision
        {
            Id = new XgDecisionId("m.xg", 1, 2, IsCube: true),
            Xgid = "XGID=x",
            Position = null!,
            Decision = TestRecords.CubeData(),
            Descriptive = TestRecords.Descriptive(),
        });

        Assert.Equal("Position", ex.ParamName);
    }

    [Fact]
    public void Decision_Null_ThrowsNamingDecision_EachKind()
    {
        // Rewritten from Decision_Null_ThrowsNamingDecision: each kind's own
        // Decision category keeps the guard.
        var cube = Assert.Throws<ArgumentNullException>(() => new CubeDecision
        {
            Id = new XgDecisionId("m.xg", 1, 2, IsCube: true),
            Xgid = "XGID=x",
            Position = TestRecords.Position(),
            Decision = null!,
            Descriptive = TestRecords.Descriptive(),
        });
        var play = Assert.Throws<ArgumentNullException>(() => new CheckerPlayDecision
        {
            Id = new XgDecisionId("m.xg", 1, 1, IsCube: false),
            Xgid = "XGID=x",
            Position = TestRecords.Position(),
            Decision = null!,
            Descriptive = TestRecords.Descriptive(),
        });

        Assert.Equal("Decision", cube.ParamName);
        Assert.Equal("Decision", play.ParamName);
    }

    [Fact]
    public void IdAndDescriptive_Null_ThrowNamingThemselves()
    {
        // Added: the guards on Id and Descriptive read them, so an explicit
        // null is refused there too.
        var id = Assert.Throws<ArgumentNullException>(() => new CubeDecision
        {
            Id = null!,
            Xgid = "XGID=x",
            Position = TestRecords.Position(),
            Decision = TestRecords.CubeData(),
            Descriptive = TestRecords.Descriptive(),
        });
        var descriptive = Assert.Throws<ArgumentNullException>(() => new CubeDecision
        {
            Id = new XgDecisionId("m.xg", 1, 2, IsCube: true),
            Xgid = "XGID=x",
            Position = TestRecords.Position(),
            Decision = TestRecords.CubeData(),
            Descriptive = null!,
        });

        Assert.Equal("Id", id.ParamName);
        Assert.Equal("Descriptive", descriptive.ParamName);
    }

    // ---------------------------------------------------------------------
    //  The wire — an explicit null is a malformed document
    // ---------------------------------------------------------------------

    /// <summary>
    /// A full record document of each kind with <paramref name="member"/> set
    /// to JSON <c>null</c>, or removed when <paramref name="remove"/> is set.
    /// </summary>
    private static IEnumerable<string> Documents(string member, bool remove = false)
    {
        foreach (BgDecisionData record in new BgDecisionData[] { TestRecords.CheckerPlay(), TestRecords.Cube() })
        {
            var document = WirePaths.Document(record);
            if (remove)
                document.Remove(member);
            else
                document[member] = null;
            yield return document.ToJsonString();
        }
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Position")]
    [InlineData("Decision")]
    [InlineData("Descriptive")]
    public void Deserialize_NullMember_IsRefused_BothPaths(string member)
    {
        // Rewritten from Deserialize_NullHalf_Throws and
        // Deserialize_NullHalf_ThroughContext_Throws: the guard's
        // ArgumentNullException now rides inside a JsonException.
        foreach (var json in Documents(member))
        {
            var ex = WirePaths.AssertRefused<BgDecisionData>(json);

            var guard = Assert.IsType<ArgumentNullException>(ex.InnerException);
            Assert.Equal(member, guard.ParamName);
        }
    }

    // ---------------------------------------------------------------------
    //  The wire — an absent member is not a null one: it is refused as
    //  absent, on both paths alike (halheinrich/backgammon#222)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("Position")]
    [InlineData("Decision")]
    public void Deserialize_AbsentHalf_IsRefused_BothPaths(string member)
    {
        // Rewritten from Deserialize_AbsentHalf_IsRefused_Reflection and
        // Deserialize_AbsentHalf_IsRefused_ThroughContext: refused as absent,
        // before any setter runs — no inner guard exception.
        foreach (var json in Documents(member, remove: true))
        {
            var ex = WirePaths.AssertRefused<BgDecisionData>(json);

            Assert.Null(ex.InnerException);
            Assert.Contains(member, ex.Message);
        }
    }
}
