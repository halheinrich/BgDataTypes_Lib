using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class DecisionIdTests
{
    // -----------------------------------------------------------------------
    //  Equality + hash code (record-default semantics)
    // -----------------------------------------------------------------------

    [Fact]
    public void XgpDecisionId_Equality_SameFilename()
    {
        var a = new XgpDecisionId("match.xgp");
        var b = new XgpDecisionId("match.xgp");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void XgpDecisionId_Equality_DifferentFilename_NotEqual()
    {
        var a = new XgpDecisionId("alpha.xgp");
        var b = new XgpDecisionId("beta.xgp");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void XgpDecisionId_Equality_CaseSensitive()
    {
        // Pinned contract: filename equality is case-sensitive (record default
        // string equality, no custom comparator).
        var a = new XgpDecisionId("Match.xgp");
        var b = new XgpDecisionId("match.xgp");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void XgDecisionId_Equality_SameTuple()
    {
        var a = new XgDecisionId("m.xg", 2, 17, IsCube: false);
        var b = new XgDecisionId("m.xg", 2, 17, IsCube: false);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void XgDecisionId_Equality_IsCubeDiscriminates()
    {
        var play = new XgDecisionId("m.xg", 2, 17, IsCube: false);
        var cube = new XgDecisionId("m.xg", 2, 17, IsCube: true);

        Assert.NotEqual(play, cube);
    }

    [Fact]
    public void XgDecisionId_Equality_DifferentGameOrMove_NotEqual()
    {
        var a = new XgDecisionId("m.xg", 1, 17, IsCube: false);
        var b = new XgDecisionId("m.xg", 2, 17, IsCube: false);
        var c = new XgDecisionId("m.xg", 1, 18, IsCube: false);

        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void XgpDecisionId_AndXgDecisionId_AreNeverEqual()
    {
        // Different concrete record types — record equality requires same EqualityContract.
        DecisionId xgp = new XgpDecisionId("m.xg");
        DecisionId xg = new XgDecisionId("m.xg", 1, 1, IsCube: false);

        Assert.NotEqual(xgp, xg);
    }

    // -----------------------------------------------------------------------
    //  Canonical string form (ToString)
    // -----------------------------------------------------------------------

    [Fact]
    public void XgpDecisionId_ToString_IsBareFilename()
    {
        var id = new XgpDecisionId("match.xgp");
        Assert.Equal("match.xgp", id.ToString());
    }

    [Theory]
    [InlineData(1, 1, false, "match.xg:g1:m1:play")]
    [InlineData(2, 17, true, "match.xg:g2:m17:cube")]
    [InlineData(7, 42, false, "match.xg:g7:m42:play")]
    public void XgDecisionId_ToString_CanonicalForm(int game, int move, bool isCube, string expected)
    {
        var id = new XgDecisionId("match.xg", game, move, isCube);
        Assert.Equal(expected, id.ToString());
    }

    // -----------------------------------------------------------------------
    //  ToString → Parse round-trip
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("match.xgp")]
    [InlineData("Mochy vs Falafel.xgp")]
    [InlineData("a.b.c.xgp")]   // multiple dots
    public void XgpDecisionId_RoundTrip(string filename)
    {
        var original = new XgpDecisionId(filename);
        var parsed = DecisionId.Parse(original.ToString());

        Assert.Equal(original, parsed);
        Assert.IsType<XgpDecisionId>(parsed);
    }

    [Theory]
    [InlineData("match.xg", 1, 1, false)]
    [InlineData("match.xg", 2, 17, true)]
    [InlineData("match.xg", 7, 42, false)]
    [InlineData("Mochy vs Falafel.xg", 3, 99, true)]
    public void XgDecisionId_RoundTrip(string filename, int game, int move, bool isCube)
    {
        var original = new XgDecisionId(filename, game, move, isCube);
        var parsed = DecisionId.Parse(original.ToString());

        Assert.Equal(original, parsed);
        Assert.IsType<XgDecisionId>(parsed);
    }

    // -----------------------------------------------------------------------
    //  Filename invariant — ':' rejected by both subtypes (symmetric)
    // -----------------------------------------------------------------------

    [Fact]
    public void XgpDecisionId_Ctor_RejectsColonInFilename()
    {
        var ex = Assert.Throws<ArgumentException>(() => new XgpDecisionId("bad:name.xgp"));
        Assert.Equal("filename", ex.ParamName);
    }

    [Fact]
    public void XgDecisionId_Ctor_RejectsColonInFilename()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => new XgDecisionId("bad:name.xg", 1, 1, IsCube: false));
        Assert.Equal("filename", ex.ParamName);
    }

    [Fact]
    public void XgpDecisionId_Ctor_RejectsNullFilename()
    {
        Assert.Throws<ArgumentNullException>(() => new XgpDecisionId(null!));
    }

    [Fact]
    public void XgDecisionId_Ctor_RejectsNullFilename()
    {
        Assert.Throws<ArgumentNullException>(
            () => new XgDecisionId(null!, 1, 1, IsCube: false));
    }

    // -----------------------------------------------------------------------
    //  TryParse — false on every rejected form
    // -----------------------------------------------------------------------

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        Assert.False(DecisionId.TryParse((string?)null, provider: null, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        Assert.False(DecisionId.TryParse(string.Empty, provider: null, out _));
    }

    [Theory]
    [InlineData(":g1:m1:play")]                   // empty filename
    [InlineData("match.xg")]                      // bare filename parses as Xgp, not a rejection
    [InlineData("match.xg:g1:m1")]                // missing kind discriminator
    [InlineData("match.xg:g1")]                   // missing move and kind
    [InlineData("match.xg:g1:m1:play:extra")]     // trailing junk (fourth ':')
    [InlineData("match.xg:1:m1:play")]            // game part missing 'g' prefix
    [InlineData("match.xg:g1:1:play")]            // move part missing 'm' prefix
    [InlineData("match.xg:gX:m1:play")]           // non-integer game number
    [InlineData("match.xg:g1:mX:play")]           // non-integer move number
    [InlineData("match.xg:g1:m1:double")]         // unknown kind discriminator
    [InlineData("match.xg:g1:m1:CUBE")]           // wrong case on kind discriminator
    [InlineData("match.xg:g:m1:play")]            // empty integer after 'g' prefix
    [InlineData("match.xg:g1:m:play")]            // empty integer after 'm' prefix
    public void TryParse_InvalidXgForms_ReturnFalse(string input)
    {
        // "match.xg" without colons is a valid XgpDecisionId, not a rejection.
        if (input == "match.xg")
        {
            Assert.True(DecisionId.TryParse(input, provider: null, out var ok));
            Assert.IsType<XgpDecisionId>(ok);
            return;
        }

        Assert.False(DecisionId.TryParse(input, provider: null, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void Parse_InvalidForm_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DecisionId.Parse("match.xg:g1:m1:double"));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DecisionId.Parse((string)null!));
    }

    // -----------------------------------------------------------------------
    //  Parse(string) and Parse(ReadOnlySpan<char>) parity
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("match.xgp")]
    [InlineData("Mochy vs Falafel.xgp")]
    [InlineData("match.xg:g1:m1:play")]
    [InlineData("match.xg:g2:m17:cube")]
    [InlineData("Mochy vs Falafel.xg:g3:m99:cube")]
    public void Parse_StringAndSpan_AgreeOnValidInput(string input)
    {
        var fromString = DecisionId.Parse(input);
        var fromSpan = DecisionId.Parse(input.AsSpan());

        Assert.Equal(fromString, fromSpan);
    }

    [Theory]
    [InlineData("match.xg:g1:m1:double")]
    [InlineData(":g1:m1:play")]
    [InlineData("match.xg:gX:m1:play")]
    public void TryParse_StringAndSpan_AgreeOnInvalidInput(string input)
    {
        bool stringResult = DecisionId.TryParse(input, provider: null, out var fromString);
        bool spanResult = DecisionId.TryParse(input.AsSpan(), provider: null, out var fromSpan);

        Assert.False(stringResult);
        Assert.False(spanResult);
        Assert.Null(fromString);
        Assert.Null(fromSpan);
    }

    // -----------------------------------------------------------------------
    //  JSON round-trip — bundled converter
    // -----------------------------------------------------------------------

    [Fact]
    public void DecisionId_JsonRoundTrip_Xgp_AsCanonicalString()
    {
        DecisionId original = new XgpDecisionId("match.xgp");
        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var restored = System.Text.Json.JsonSerializer.Deserialize<DecisionId>(json);

        Assert.Equal("\"match.xgp\"", json);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void DecisionId_JsonRoundTrip_Xg_AsCanonicalString()
    {
        DecisionId original = new XgDecisionId("match.xg", 2, 17, IsCube: true);
        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var restored = System.Text.Json.JsonSerializer.Deserialize<DecisionId>(json);

        Assert.Equal("\"match.xg:g2:m17:cube\"", json);
        Assert.Equal(original, restored);
    }
}
