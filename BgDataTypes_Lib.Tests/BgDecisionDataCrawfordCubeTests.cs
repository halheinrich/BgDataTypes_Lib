using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The Crawford-cube invariant on the composite record
/// (halheinrich/backgammon#201): doubling is prohibited in the Crawford
/// game, so a record whose <see cref="PositionData.IsCrawford"/> and
/// <see cref="DecisionData.IsCube"/> are both true describes a decision
/// that cannot exist, and the record refuses to be constructed. The guard
/// sits in both init setters so whichever half is set second fires — an
/// object initializer in either member order and a JSON document in either
/// property order all fail the same way. The <c>UserDoublerAction</c>
/// rejection tests are the pattern.
/// </summary>
public class BgDecisionDataCrawfordCubeTests
{
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    private static PositionData Crawford() =>
        new() { OnRollNeeds = 1, OpponentNeeds = 3, IsCrawford = true };

    private static PositionData NotCrawford() =>
        new() { OnRollNeeds = 1, OpponentNeeds = 3 };

    private static DecisionData Cube() => new() { IsCube = true };

    private static DecisionData Play() => new() { IsCube = false, Dice = [3, 1] };

    // ---------------------------------------------------------------------
    //  Object initializers — whichever half completes the Crawford cube
    //  is the one named
    // ---------------------------------------------------------------------

    [Fact]
    public void CrawfordCube_DecisionSetSecond_ThrowsNamingDecision()
    {
        var ex = Assert.Throws<ArgumentException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = Crawford(),
            Decision = Cube(),
        });

        Assert.Equal("Decision", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordCube_PositionSetSecond_ThrowsNamingPosition()
    {
        var ex = Assert.Throws<ArgumentException>(() => new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = Cube(),
            Position = Crawford(),
        });

        Assert.Equal("Position", ex.ParamName);
        Assert.Contains("Crawford", ex.Message);
    }

    [Fact]
    public void CrawfordPlay_Constructs_InEitherOrder()
    {
        var positionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = Crawford(),
            Decision = Play(),
        };
        var decisionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = Play(),
            Position = Crawford(),
        };

        Assert.True(positionFirst.IsCrawford);
        Assert.False(positionFirst.IsCube);
        Assert.True(decisionFirst.IsCrawford);
        Assert.False(decisionFirst.IsCube);
    }

    [Fact]
    public void NonCrawfordCube_Constructs_InEitherOrder()
    {
        var positionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = NotCrawford(),
            Decision = Cube(),
        };
        var decisionFirst = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = Cube(),
            Position = NotCrawford(),
        };

        Assert.False(positionFirst.IsCrawford);
        Assert.True(positionFirst.IsCube);
        Assert.False(decisionFirst.IsCrawford);
        Assert.True(decisionFirst.IsCube);
    }

    [Fact]
    public void HalfSetRecord_NeverThrows()
    {
        // Both defaults are "not cube, not Crawford", so setting only one
        // half — either half — cannot complete the forbidden pair.
        var crawfordOnly = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Position = Crawford(),
        };
        var cubeOnly = new BgDecisionData
        {
            Id = new XgpDecisionId("test.xgp"),
            Decision = Cube(),
        };

        Assert.True(crawfordOnly.IsCrawford);
        Assert.False(crawfordOnly.IsCube);
        Assert.False(cubeOnly.IsCrawford);
        Assert.True(cubeOnly.IsCube);
    }

    // ---------------------------------------------------------------------
    //  The wire — the same guards run during deserialization, in either
    //  property order, through the reflection path and the source-generated
    //  context alike
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":{\"IsCrawford\":true},\"Decision\":{\"IsCube\":true}}")]
    [InlineData("{\"Id\":\"test.xgp\",\"Decision\":{\"IsCube\":true},\"Position\":{\"IsCrawford\":true}}")]
    public void Deserialize_CrawfordCube_Throws(string json)
    {
        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json));
    }

    [Fact]
    public void Deserialize_CrawfordCube_ThroughContext_Throws()
    {
        const string json =
            "{\"Id\":\"test.xgp\",\"Position\":{\"IsCrawford\":true},\"Decision\":{\"IsCube\":true}}";

        Assert.Throws<ArgumentException>(
            () => JsonSerializer.Deserialize<BgDecisionData>(json, ContextOptions));
    }

    [Theory]
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":{\"IsCrawford\":true},\"Decision\":{\"IsCube\":false}}")]
    [InlineData("{\"Id\":\"test.xgp\",\"Position\":{\"IsCrawford\":false},\"Decision\":{\"IsCube\":true}}")]
    public void Deserialize_OneFactOnly_Loads(string json)
    {
        var restored = JsonSerializer.Deserialize<BgDecisionData>(json)!;

        Assert.NotEqual(restored.IsCrawford, restored.IsCube);
    }
}
