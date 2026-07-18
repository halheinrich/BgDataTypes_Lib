using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class DiceRollTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    // ---------------------------------------------------------------------
    //  Construction + canonicalization
    // ---------------------------------------------------------------------

    [Theory]
    // Either argument order canonicalizes to the same High/Low.
    [InlineData(3, 1, 3, 1)]
    [InlineData(1, 3, 3, 1)]
    [InlineData(6, 5, 6, 5)]
    [InlineData(5, 6, 6, 5)]
    [InlineData(4, 4, 4, 4)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(6, 6, 6, 6)]
    public void Construct_EitherOrder_Canonicalizes(int die1, int die2, int high, int low)
    {
        var roll = new DiceRoll(die1, die2);

        Assert.Equal(high, roll.High);
        Assert.Equal(low, roll.Low);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(63)]
    public void Construct_FaceOutOfRange_Die1_Throws(int badFace)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new DiceRoll(badFace, 3));
        Assert.Equal("die1", ex.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    [InlineData(63)]
    public void Construct_FaceOutOfRange_Die2_Throws(int badFace)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new DiceRoll(3, badFace));
        Assert.Equal("die2", ex.ParamName);
    }

    [Fact]
    public void Deconstruct_YieldsHighThenLow()
    {
        var (high, low) = new DiceRoll(2, 5);

        Assert.Equal(5, high);
        Assert.Equal(2, low);
    }

    // ---------------------------------------------------------------------
    //  IsDouble
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(4, 4, true)]
    [InlineData(6, 6, true)]
    [InlineData(3, 1, false)]
    [InlineData(6, 5, false)]
    public void IsDouble_TrueOnlyWhenFacesMatch(int die1, int die2, bool expected)
    {
        Assert.Equal(expected, new DiceRoll(die1, die2).IsDouble);
    }

    // ---------------------------------------------------------------------
    //  Value equality — canonical form makes 3-1 ≡ 1-3
    // ---------------------------------------------------------------------

    [Fact]
    public void Equality_ArgumentOrderIrrelevant()
    {
        var a = new DiceRoll(3, 1);
        var b = new DiceRoll(1, 3);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentRolls_AreNotEqual()
    {
        var a = new DiceRoll(3, 1);
        var b = new DiceRoll(3, 2);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    // ---------------------------------------------------------------------
    //  ToString — canonical high-first token
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(6, 3, "63")]
    [InlineData(1, 3, "31")]
    [InlineData(5, 5, "55")]
    [InlineData(1, 1, "11")]
    public void ToString_EmitsCanonicalToken(int die1, int die2, string expected)
    {
        Assert.Equal(expected, new DiceRoll(die1, die2).ToString());
    }

    // ---------------------------------------------------------------------
    //  Parse / TryParse
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("31", 3, 1)]
    [InlineData("13", 3, 1)]   // low-first spelling canonicalizes
    [InlineData("66", 6, 6)]
    [InlineData("11", 1, 1)]
    public void Parse_ValidToken_Canonicalizes(string token, int high, int low)
    {
        var roll = DiceRoll.Parse(token);

        Assert.Equal(high, roll.High);
        Assert.Equal(low, roll.Low);
    }

    [Theory]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("315")]
    [InlineData("07")]
    [InlineData("70")]
    [InlineData("ab")]
    [InlineData("3 ")]
    [InlineData(" 3")]
    public void Parse_MalformedToken_ThrowsFormatException(string token)
    {
        Assert.Throws<FormatException>(() => DiceRoll.Parse(token));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DiceRoll.Parse(null!));
    }

    [Fact]
    public void TryParse_ValidToken_ReturnsTrue()
    {
        Assert.True(DiceRoll.TryParse("13", provider: null, out var roll));
        Assert.Equal(new DiceRoll(3, 1), roll);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("70")]
    [InlineData("xx")]
    public void TryParse_Invalid_ReturnsFalse(string? token)
    {
        Assert.False(DiceRoll.TryParse(token, provider: null, out _));
    }

    [Fact]
    public void Parse_SpanOverload_MatchesStringOverload()
    {
        Assert.Equal(DiceRoll.Parse("52"), DiceRoll.Parse("52".AsSpan()));
        Assert.True(DiceRoll.TryParse("25".AsSpan(), provider: null, out var roll));
        Assert.Equal(new DiceRoll(5, 2), roll);
    }

    [Fact]
    public void ParseFormat_RoundTrips_AllTwentyOneRolls()
    {
        for (int high = 1; high <= 6; high++)
        {
            for (int low = 1; low <= high; low++)
            {
                var original = new DiceRoll(high, low);
                Assert.Equal(original, DiceRoll.Parse(original.ToString()));
            }
        }
    }

    // ---------------------------------------------------------------------
    //  JSON — bundled converter, string-token shape
    // ---------------------------------------------------------------------

    [Fact]
    public void Json_Serializes_AsCanonicalToken()
    {
        Assert.Equal("\"31\"", JsonSerializer.Serialize(new DiceRoll(1, 3), Options));
        Assert.Equal("\"55\"", JsonSerializer.Serialize(new DiceRoll(5, 5), Options));
    }

    [Fact]
    public void Json_Deserializes_EitherSpelling_ToCanonical()
    {
        Assert.Equal(new DiceRoll(3, 1), JsonSerializer.Deserialize<DiceRoll>("\"31\"", Options));
        Assert.Equal(new DiceRoll(3, 1), JsonSerializer.Deserialize<DiceRoll>("\"13\"", Options));
    }

    [Fact]
    public void Json_RoundTrips_AllTwentyOneRolls()
    {
        for (int high = 1; high <= 6; high++)
        {
            for (int low = 1; low <= high; low++)
            {
                var original = new DiceRoll(high, low);
                var json = JsonSerializer.Serialize(original, Options);
                Assert.Equal(original, JsonSerializer.Deserialize<DiceRoll>(json, Options));
            }
        }
    }

    [Fact]
    public void Json_NullableRoll_RoundTripsNull()
    {
        var json = JsonSerializer.Serialize((DiceRoll?)null, Options);

        Assert.Equal("null", json);
        Assert.Null(JsonSerializer.Deserialize<DiceRoll?>(json, Options));
    }

    [Theory]
    [InlineData("\"70\"")]
    [InlineData("\"1\"")]
    [InlineData("\"\"")]
    [InlineData("31")]        // number token, not a string
    [InlineData("[3,1]")]
    public void Json_MalformedToken_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DiceRoll>(json, Options));
    }

    // ---------------------------------------------------------------------
    //  Ordering — CompareTo + comparison operators
    // ---------------------------------------------------------------------

    [Fact]
    public void CompareTo_OrdersByHighThenLow()
    {
        // Ascending canonical-token order: 11 < 21 < 22 < 31 < 65 < 66.
        Assert.True(new DiceRoll(1, 1).CompareTo(new DiceRoll(2, 1)) < 0);
        Assert.True(new DiceRoll(2, 1).CompareTo(new DiceRoll(2, 2)) < 0);
        Assert.True(new DiceRoll(2, 2).CompareTo(new DiceRoll(3, 1)) < 0);
        Assert.True(new DiceRoll(6, 6).CompareTo(new DiceRoll(6, 5)) > 0);
        Assert.Equal(0, new DiceRoll(3, 1).CompareTo(new DiceRoll(1, 3)));
    }

    [Fact]
    public void ComparisonOperators_MatchCompareTo()
    {
        var small = new DiceRoll(2, 1);
        var large = new DiceRoll(2, 2);

        Assert.True(small < large);
        Assert.True(small <= large);
        Assert.True(large > small);
        Assert.True(large >= small);
        Assert.True(small <= new DiceRoll(1, 2));
        Assert.True(small >= new DiceRoll(1, 2));
        Assert.False(small > large);
    }

    [Fact]
    public void Sort_YieldsAscendingCanonicalOrder()
    {
        var rolls = new List<DiceRoll>
        {
            new(6, 6), new(2, 1), new(1, 1), new(6, 5), new(2, 2), new(3, 1)
        };

        rolls.Sort();

        Assert.Equal(
            [new(1, 1), new(2, 1), new(2, 2), new(3, 1), new(6, 5), new(6, 6)],
            rolls);
    }

    [Fact]
    public void CompareTo_NonGeneric_FollowsStandardContract()
    {
        IComparable roll = new DiceRoll(3, 1);

        Assert.True(roll.CompareTo(null) > 0);
        Assert.Equal(0, roll.CompareTo(new DiceRoll(1, 3)));
        Assert.Throws<ArgumentException>(() => roll.CompareTo("31"));
    }
}
