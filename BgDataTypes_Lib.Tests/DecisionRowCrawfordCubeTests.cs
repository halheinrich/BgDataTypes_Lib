using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The Crawford-cube invariant on the flat row
/// (halheinrich/backgammon#201): a row whose <see cref="DecisionRow.Roll"/>
/// is 0 (a cube decision) and whose <see cref="DecisionRow.IsCrawford"/> is
/// true describes a decision that cannot exist, and the row refuses to be
/// constructed. The row's trap is that cube is its <em>default</em> kind, so
/// the guard needs two things the composite record does not:
/// <see cref="DecisionRow.Roll"/> is <c>required</c>, so every construction
/// and every document states the kind, and the guard distinguishes a
/// not-yet-stated roll from a stated 0, so the legal initializer order
/// <c>{ IsCrawford = true, Roll = 31 }</c> constructs. The
/// <c>UserDoublerAction</c> rejection tests are the pattern.
/// </summary>
public class DecisionRowCrawfordCubeTests
{
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    // ---------------------------------------------------------------------
    //  Object initializers — whichever member completes the Crawford cube
    //  is the one named
    // ---------------------------------------------------------------------

    [Fact]
    public void CrawfordCube_IsCrawfordSetSecond_ThrowsNamingIsCrawford()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 0,
            IsCrawford = true,
        });

        Assert.Equal("IsCrawford", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordCube_RollSetSecond_ThrowsNamingRoll()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            IsCrawford = true,
            Roll = 0,
        });

        Assert.Equal("Roll", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordPlay_Constructs_InEitherOrder()
    {
        // The trap order first: IsCrawford is set while Roll is still
        // unstated, and an unstated roll must not read as a cube.
        var crawfordFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            IsCrawford = true,
            Roll = 31,
        };
        var rollFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 31,
            IsCrawford = true,
        };

        Assert.True(crawfordFirst.IsCrawford);
        Assert.False(crawfordFirst.IsCube);
        Assert.Equal(31, crawfordFirst.Roll);
        Assert.True(rollFirst.IsCrawford);
        Assert.False(rollFirst.IsCube);
        Assert.Equal(31, rollFirst.Roll);
    }

    [Fact]
    public void NonCrawfordCube_Constructs_InEitherOrder()
    {
        var rollFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            Roll = 0,
            IsCrawford = false,
        };
        var crawfordFirst = new DecisionRow
        {
            Id = new XgpDecisionId("test.xgp"),
            IsCrawford = false,
            Roll = 0,
        };

        Assert.True(rollFirst.IsCube);
        Assert.False(rollFirst.IsCrawford);
        Assert.True(crawfordFirst.IsCube);
        Assert.False(crawfordFirst.IsCrawford);
    }

    // ---------------------------------------------------------------------
    //  The wire — the same guards run during deserialization, in either
    //  property order, through the reflection path and the source-generated
    //  context alike; and a document that never states Roll is refused
    //  rather than defaulting to a cube
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("{\"Id\":\"test.xgp\",\"Roll\":0,\"IsCrawford\":true}")]
    [InlineData("{\"Id\":\"test.xgp\",\"IsCrawford\":true,\"Roll\":0}")]
    public void Deserialize_CrawfordCube_Throws(string json)
    {
        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json));
    }

    [Fact]
    public void Deserialize_CrawfordCube_ThroughContext_Throws()
    {
        const string json = "{\"Id\":\"test.xgp\",\"IsCrawford\":true,\"Roll\":0}";

        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json, ContextOptions));
    }

    [Theory]
    [InlineData("{\"Id\":\"test.xgp\"}")]
    [InlineData("{\"Id\":\"test.xgp\",\"IsCrawford\":true}")]
    public void Deserialize_RollAbsent_Throws(string json)
    {
        // Roll is required on the wire too: the second document is exactly
        // the shape that used to read as a Crawford cube by default.
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json));
    }

    [Fact]
    public void Deserialize_RollAbsent_ThroughContext_Throws()
    {
        const string json = "{\"Id\":\"test.xgp\",\"IsCrawford\":true}";

        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<DecisionRow>(json, ContextOptions));
    }

    [Theory]
    [InlineData("{\"Id\":\"test.xgp\",\"IsCrawford\":true,\"Roll\":31}")]
    [InlineData("{\"Id\":\"test.xgp\",\"Roll\":31,\"IsCrawford\":true}")]
    public void Deserialize_CrawfordPlay_Loads(string json)
    {
        var restored = JsonSerializer.Deserialize<DecisionRow>(json)!;

        Assert.True(restored.IsCrawford);
        Assert.False(restored.IsCube);
        Assert.Equal(31, restored.Roll);
    }

    [Fact]
    public void Deserialize_NonCrawfordCube_Loads()
    {
        var restored = JsonSerializer.Deserialize<DecisionRow>(
            "{\"Id\":\"test.xgp\",\"Roll\":0,\"IsCrawford\":false}")!;

        Assert.True(restored.IsCube);
        Assert.False(restored.IsCrawford);
    }
}
