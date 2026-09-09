using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The helper's own argument guards. Its restore policy is pinned through
/// the interface in <see cref="IJsonDocumentTests"/>, where the synthetic
/// payload forwards to it — the way every implementer will.
/// </summary>
public class CanonicalJsonTests
{
    [Fact]
    public void Parse_NullTypeInfo_ThrowsArgumentNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => CanonicalJson.Parse<SamplePayload>("{}", null!));

        Assert.Equal("typeInfo", ex.ParamName);
    }

    [Fact]
    public void TryParse_NullTypeInfo_ThrowsArgumentNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => CanonicalJson.TryParse<SamplePayload>("{}", null!, new(), out _));

        Assert.Equal("typeInfo", ex.ParamName);
    }

    [Fact]
    public void TryParse_NullFallback_ThrowsArgumentNull()
    {
        // The out must always be usable; a null fallback would break that
        // promise on the failure path, so it is a caller bug up front.
        var ex = Assert.Throws<ArgumentNullException>(
            () => CanonicalJson.TryParse<SamplePayload>(null, SampleJsonContext.Default.SamplePayload, null!, out _));

        Assert.Equal("fallback", ex.ParamName);
    }

    [Fact]
    public void TryParse_Failure_YieldsTheGivenFallbackInstance()
    {
        var fallback = new SamplePayload { Label = "fallback" };

        var found = CanonicalJson.TryParse(
            "not json", SampleJsonContext.Default.SamplePayload, fallback, out var document);

        Assert.False(found);
        Assert.Same(fallback, document);
    }
}
