using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The persistence-trio contract, exercised through the interface's static
/// abstract members on the synthetic <see cref="SamplePayload"/> — the way
/// a generic container will call them. The interface has no implementers
/// in this library yet (halheinrich/backgammon#190 legs (B) and (C) add
/// them), so these pins are the contract's first executable statement.
/// </summary>
public class IJsonDocumentTests
{
    // Generic dispatch: the only way a caller that knows nothing but the
    // interface reaches the trio.
    private static T Restore<T>(string json) where T : IJsonDocument<T> => T.FromJson(json);

    private static bool TryRestore<T>(string? json, out T document) where T : IJsonDocument<T> =>
        T.TryFromJson(json, out document);

    private static string Write<T>(T document) where T : IJsonDocument<T> => document.ToJson();

    private static SamplePayload Rich() => new()
    {
        Label = "blitz",
        Weight = 7,
        Tags = ["a", "b"],
    };

    // -----------------------------------------------------------------------
    //  ToJson / FromJson are inverses, reached through the interface
    // -----------------------------------------------------------------------

    [Fact]
    public void RoundTrip_ThroughTheInterface_PreservesTheValue()
    {
        var original = Rich();

        var restored = Restore<SamplePayload>(Write(original));

        Assert.Equal(original.ToJson(), restored.ToJson());
        Assert.Equal("blitz", restored.Label);
        Assert.Equal(7, restored.Weight);
        Assert.Equal(["a", "b"], restored.Tags);
    }

    [Fact]
    public void ToJson_PinsTheSamplePayloadShape()
    {
        // The synthetic payload's own wire form, pinned so the collection
        // tests' hand-written envelopes have a stable body to embed.
        Assert.Equal("""{"Label":"blitz","Weight":7,"Tags":["a","b"]}""", Rich().ToJson());
        Assert.Equal("""{"Label":"","Weight":0,"Tags":[]}""", new SamplePayload().ToJson());
    }

    [Fact]
    public void FromJson_ToleratesUnknownMembers()
    {
        // The tolerant-payload half of the envelope split: a retired member
        // is ignored, exactly as the real payloads ignore one.
        var restored = Restore<SamplePayload>("""{"Label":"x","Retired":true}""");

        Assert.Equal("x", restored.Label);
    }

    // -----------------------------------------------------------------------
    //  FromJson — the three rejections
    // -----------------------------------------------------------------------

    [Fact]
    public void FromJson_NullString_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Restore<SamplePayload>(null!));
    }

    [Fact]
    public void FromJson_NullToken_ThrowsArgument()
    {
        var ex = Assert.Throws<ArgumentException>(() => Restore<SamplePayload>("null"));

        Assert.Equal("json", ex.ParamName);
        Assert.Contains("SamplePayload", ex.Message);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("5")]
    [InlineData("[]")]
    [InlineData("""{"Weight":"seven"}""")]
    [InlineData("""{"Label":"x"} trailing""")]
    public void FromJson_MalformedOrContractViolating_ThrowsJson(string json)
    {
        Assert.Throws<JsonException>(() => Restore<SamplePayload>(json));
    }

    // -----------------------------------------------------------------------
    //  TryFromJson — success, and the three absorbed failures
    // -----------------------------------------------------------------------

    [Fact]
    public void TryFromJson_Valid_RestoresAndReportsTrue()
    {
        var found = TryRestore<SamplePayload>(Rich().ToJson(), out var restored);

        Assert.True(found);
        Assert.Equal(Rich().ToJson(), restored.ToJson());
    }

    [Theory]
    [InlineData(null)]                      // store never written
    [InlineData("null")]                    // literal null token
    [InlineData("not json")]                // malformed
    [InlineData("""{"Weight":"seven"}""")]  // contract violation
    public void TryFromJson_FailureShapes_YieldTheInertDefault(string? json)
    {
        var found = TryRestore<SamplePayload>(json, out var restored);

        Assert.False(found);
        Assert.Equal(new SamplePayload().ToJson(), restored.ToJson());
    }

    [Fact]
    public void TryFromJson_YieldsAFreshDefaultPerCall_ForAMutablePayload()
    {
        // The mutable payload's inert default must not be a shared instance,
        // or one caller's edit would leak into every later failed restore.
        TryRestore<SamplePayload>(null, out var first);
        TryRestore<SamplePayload>(null, out var second);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void TryFromJson_PropagatesANonJsonException()
    {
        // Only JsonException is absorbed; a converter bug surfaces.
        Assert.Throws<InvalidOperationException>(
            () => TryRestore<ThrowingDocument>("{}", out _));
    }
}
