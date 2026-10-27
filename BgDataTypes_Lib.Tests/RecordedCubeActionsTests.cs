using System.Globalization;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A cube decision's recorded cube actions as one value
/// (<see cref="RecordedCubeActions"/>, halheinrich/backgammon#440): its six
/// valid shapes, each through the constructor and its factory; every
/// combination its rules refuse, each with the parameter it names; and its
/// equality and diagnostics. How a record holds it, and how a document
/// reaches it, are pinned in <see cref="CubeDecisionDataRecordedTests"/>.
/// </summary>
public class RecordedCubeActionsTests
{
    // ── The six valid shapes ──────────────────────────────────────

    public static TheoryData<string, CubeAction?, CubeAction?, double?, double?> Shapes => new()
    {
        { nameof(RecordedCubeActions.None), null, null, null, null },
        { nameof(RecordedCubeActions.NoDouble), CubeAction.NoDouble, null, null, null },
        { nameof(RecordedCubeActions.DoubleUnanswered), CubeAction.Double, null, null, null },
        { "DoubleAnswered(Take)", CubeAction.Double, CubeAction.Take, null, null },
        { "DoubleAnswered(Pass)", CubeAction.Double, CubeAction.Pass, null, null },
        { nameof(RecordedCubeActions.DoubleWithUnstatedResponse), CubeAction.Double, null, null, 0.013 },
        { nameof(RecordedCubeActions.UnstatedDoubler), null, null, 0.042, null },
    };

    /// <summary>The factory a shape is named for.</summary>
    private static RecordedCubeActions Factory(string shape) => shape switch
    {
        nameof(RecordedCubeActions.None) => RecordedCubeActions.None,
        nameof(RecordedCubeActions.NoDouble) => RecordedCubeActions.NoDouble,
        nameof(RecordedCubeActions.DoubleUnanswered) => RecordedCubeActions.DoubleUnanswered,
        "DoubleAnswered(Take)" => RecordedCubeActions.DoubleAnswered(CubeAction.Take),
        "DoubleAnswered(Pass)" => RecordedCubeActions.DoubleAnswered(CubeAction.Pass),
        nameof(RecordedCubeActions.DoubleWithUnstatedResponse) => RecordedCubeActions.DoubleWithUnstatedResponse(0.013),
        nameof(RecordedCubeActions.UnstatedDoubler) => RecordedCubeActions.UnstatedDoubler(0.042),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void EveryValidShape_BuildsThroughTheConstructorAndItsFactory_AndReadsBack(
        string shape, CubeAction? doubler, CubeAction? taker, double? unstatedDoubler, double? unstatedTaker)
    {
        var built = new RecordedCubeActions(doubler, taker, unstatedDoubler, unstatedTaker);

        Assert.Equal((doubler, taker, unstatedDoubler, unstatedTaker),
            (built.DoublerAction, built.TakerAction, built.UnstatedDoublerError, built.UnstatedTakerError));
        Assert.Equal(built, Factory(shape));
    }

    [Fact]
    public void Default_IsNone()
    {
        Assert.Equal(RecordedCubeActions.None, default);
        Assert.True(RecordedCubeActions.None == default(RecordedCubeActions));
        Assert.Equal((null, null, null, null), (default(RecordedCubeActions).DoublerAction, default(RecordedCubeActions).TakerAction,
            default(RecordedCubeActions).UnstatedDoublerError, default(RecordedCubeActions).UnstatedTakerError));
    }

    [Fact]
    public void ANegativeUnstatedError_IsCarriedAsStated_EitherHalf()
    {
        // The analyser's number is carried as stated, sign and all, as
        // PlayerResult.Unstated carries it: no sign rule.
        Assert.Equal(-0.01, RecordedCubeActions.UnstatedDoubler(-0.01).UnstatedDoublerError);
        Assert.Equal(-0.02, RecordedCubeActions.DoubleWithUnstatedResponse(-0.02).UnstatedTakerError);
    }

    // ── A taker half needs a stated double (SPEC-scoring §3) ──────

    public static TheoryData<string, CubeAction?, CubeAction?, double?, string> TakerHalvesWithoutADouble => new()
    {
        { "a response, the doubler not stated", null, CubeAction.Take, null, "takerAction" },
        { "a response, the doubler not doubling", CubeAction.NoDouble, CubeAction.Pass, null, "takerAction" },
        { "an unstated take error, the doubler not stated", null, null, 0.013, "unstatedTakerError" },
        { "an unstated take error, the doubler not doubling", CubeAction.NoDouble, null, 0.013, "unstatedTakerError" },
    };

    [Theory]
    [MemberData(nameof(TakerHalvesWithoutADouble))]
    public void ATakerHalf_WithoutAStatedDouble_IsRefused(
        string because, CubeAction? doubler, CubeAction? taker, double? unstatedTaker, string paramName)
    {
        var ex = Assert.Throws<ArgumentException>(() => new RecordedCubeActions(doubler, taker, null, unstatedTaker));

        Assert.True(ex.ParamName == paramName, $"{because}: named {ex.ParamName}");
        Assert.Contains("needs a stated Double", ex.Message);
    }

    [Fact]
    public void ATakerHalf_WithoutAStatedDouble_IsRefused_BesideAnUnstatedDoublerError()
    {
        // The analyser's doubling error is no stated double either.
        var ex = Assert.Throws<ArgumentException>(() => new RecordedCubeActions(null, CubeAction.Take, 0.05, null));
        Assert.Equal("takerAction", ex.ParamName);
    }

    // ── Each half states its action or the analyser's error ───────

    [Theory]
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    public void AStatedDoublerAction_BesideAnUnstatedDoublerError_IsRefused(CubeAction doubler)
    {
        var ex = Assert.Throws<ArgumentException>(() => new RecordedCubeActions(doubler, null, 0.05, null));
        Assert.Equal("unstatedDoublerError", ex.ParamName);
    }

    [Theory]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    public void AStatedTakerAction_BesideAnUnstatedTakerError_IsRefused(CubeAction taker)
    {
        var ex = Assert.Throws<ArgumentException>(() => new RecordedCubeActions(CubeAction.Double, taker, null, 0.06));
        Assert.Equal("unstatedTakerError", ex.ParamName);
    }

    // ── Each half's action is its own half's ──────────────────────

    [Theory]
    [InlineData(CubeAction.Take)]
    [InlineData(CubeAction.Pass)]
    [InlineData((CubeAction)99)]
    public void ADoublerAction_OutOfItsHalf_IsRefused(CubeAction doubler)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedCubeActions(doubler, null, null, null));
        Assert.Equal("doublerAction", ex.ParamName);
    }

    [Theory]
    [InlineData(CubeAction.NoDouble)]
    [InlineData(CubeAction.Double)]
    [InlineData((CubeAction)99)]
    public void ATakerAction_OutOfItsHalf_IsRefused_ThroughTheConstructorAndTheFactory(CubeAction taker)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedCubeActions(CubeAction.Double, taker, null, null));
        Assert.Equal("takerAction", ex.ParamName);
        Assert.Equal("takerAction",
            Assert.Throws<ArgumentOutOfRangeException>(() => RecordedCubeActions.DoubleAnswered(taker)).ParamName);
    }

    // ── The analyser's errors are finite (FiniteNumber) ───────────

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ANonFiniteError_IsRefused_EitherHalf_NamingItsParameter(double error)
    {
        // The library's one rule for a stored number: its refusal is an
        // ArgumentOutOfRangeException, an ArgumentException naming the member.
        Assert.Equal("unstatedDoublerError",
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedCubeActions(null, null, error, null)).ParamName);
        Assert.Equal("unstatedTakerError",
            Assert.Throws<ArgumentOutOfRangeException>(() => new RecordedCubeActions(CubeAction.Double, null, null, error)).ParamName);
        Assert.Equal("unstatedDoublerError",
            Assert.Throws<ArgumentOutOfRangeException>(() => RecordedCubeActions.UnstatedDoubler(error)).ParamName);
        Assert.Equal("unstatedTakerError",
            Assert.Throws<ArgumentOutOfRangeException>(() => RecordedCubeActions.DoubleWithUnstatedResponse(error)).ParamName);
    }

    // ── Equality ──────────────────────────────────────────────────

    [Fact]
    public void TheSameFourFacts_AreEqual_WithTheSameHash()
    {
        var one = new RecordedCubeActions(CubeAction.Double, null, null, 0.013);
        var other = RecordedCubeActions.DoubleWithUnstatedResponse(0.013);

        Assert.True(one.Equals(other));
        Assert.True(one.Equals((object)other));
        Assert.True(one == other);
        Assert.False(one != other);
        Assert.Equal(one.GetHashCode(), other.GetHashCode());
    }

    public static TheoryData<string, RecordedCubeActions, RecordedCubeActions> OneMemberApart => new()
    {
        { "the doubler's action", RecordedCubeActions.NoDouble, RecordedCubeActions.DoubleUnanswered },
        { "the taker's action", RecordedCubeActions.DoubleAnswered(CubeAction.Take), RecordedCubeActions.DoubleAnswered(CubeAction.Pass) },
        { "the doubler's unstated error", RecordedCubeActions.UnstatedDoubler(0.1), RecordedCubeActions.UnstatedDoubler(0.2) },
        { "the taker's unstated error", RecordedCubeActions.DoubleWithUnstatedResponse(0.1), RecordedCubeActions.DoubleWithUnstatedResponse(0.2) },
        { "a stated error against none", RecordedCubeActions.UnstatedDoubler(0.0), RecordedCubeActions.None },
    };

    [Theory]
    [MemberData(nameof(OneMemberApart))]
    public void ValuesDifferingInOneMember_AreNotEqual(string member, RecordedCubeActions one, RecordedCubeActions other)
    {
        Assert.False(one.Equals(other), member);
        Assert.False(one.Equals((object)other), member);
        Assert.False(one == other, member);
        Assert.True(one != other, member);
    }

    [Fact]
    public void ADifferentType_IsNotEqual()
    {
        Assert.False(RecordedCubeActions.None.Equals(PlayerResult.NotRecorded));
        Assert.False(RecordedCubeActions.None.Equals(null));
    }

    // ── Diagnostics ───────────────────────────────────────────────

    [Fact]
    public void ToString_NamesTheShape_ByItsFactory()
    {
        Assert.Equal("None", RecordedCubeActions.None.ToString());
        Assert.Equal("NoDouble", RecordedCubeActions.NoDouble.ToString());
        Assert.Equal("DoubleUnanswered", RecordedCubeActions.DoubleUnanswered.ToString());
        Assert.Equal("DoubleAnswered Take", RecordedCubeActions.DoubleAnswered(CubeAction.Take).ToString());
        Assert.Equal("DoubleAnswered Pass", RecordedCubeActions.DoubleAnswered(CubeAction.Pass).ToString());
        Assert.Equal("DoubleWithUnstatedResponse 0.013", RecordedCubeActions.DoubleWithUnstatedResponse(0.013).ToString());
        Assert.Equal("UnstatedDoubler 0.042", RecordedCubeActions.UnstatedDoubler(0.042).ToString());
    }

    [Fact]
    public void ToString_IsCultureInvariant()
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("UnstatedDoubler 0.042", RecordedCubeActions.UnstatedDoubler(0.042).ToString());
            Assert.Equal("DoubleWithUnstatedResponse 0.013", RecordedCubeActions.DoubleWithUnstatedResponse(0.013).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }
}
