using System.ComponentModel;
using System.Reflection;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The player's result as a type (the umbrella's third-round ruling on the
/// records leg of halheinrich/backgammon#273): four named cases, the error
/// readable only for the two that have one, a scored error never negative,
/// and the default "no move recorded". Where records produce each case is
/// pinned in <see cref="BgDecisionDataPlayerResultTests"/> and
/// <see cref="PlayRankingTests"/>.
/// </summary>
public class PlayerResultTests
{
    public static TheoryData<string, PlayerResultKind, double?> Cases => new()
    {
        { "not recorded", PlayerResultKind.NotRecorded, null },
        { "not scored", PlayerResultKind.NotScored, null },
        { "scored", PlayerResultKind.Scored, 0.032 },
        { "unstated", PlayerResultKind.Unstated, 0.031 },
    };

    private static PlayerResult Build(PlayerResultKind kind, double? error) => kind switch
    {
        PlayerResultKind.NotRecorded => PlayerResult.NotRecorded,
        PlayerResultKind.NotScored => PlayerResult.NotScored,
        PlayerResultKind.Scored => PlayerResult.Scored(error!.Value),
        _ => PlayerResult.Unstated(error!.Value),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void EachCase_NamesItself_AndHasAnErrorExactlyWhenItShould(string name, PlayerResultKind kind, double? error)
    {
        var result = Build(kind, error);

        Assert.Equal(kind, result.Kind);
        Assert.True(result.TryGetError(out double actual) == error.HasValue, name);
        Assert.Equal(error ?? 0.0, actual);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Match_RunsTheCasesBranch_HandingTheErrorToTheTwoThatHaveOne(string name, PlayerResultKind kind, double? error)
    {
        string ran = Build(kind, error).Match(
            notRecorded: () => "not recorded",
            notScored: () => "not scored",
            scored: e => $"scored {e}",
            unstated: e => $"unstated {e}");

        Assert.Equal(error is double e ? $"{name} {e}" : name, ran);
    }

    [Fact]
    public void Match_RefusesAMissingBranch()
    {
        var result = PlayerResult.Scored(0.1);

        Assert.Throws<ArgumentNullException>(() => result.Match(null!, () => 0, _ => 0, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => 0, null!, _ => 0, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => 0, () => 0, null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => result.Match(() => 0, () => 0, _ => 0, null!));
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AScoredError_IsFiniteAndNeverNegative(double error)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => PlayerResult.Scored(error));
        Assert.Equal("error", ex.ParamName);
    }

    [Fact]
    public void AnUnstatedError_IsTheAnalysersNumber_AsStated()
    {
        Assert.True(PlayerResult.Unstated(0.031).TryGetError(out double error));
        Assert.Equal(0.031, error);
        Assert.True(PlayerResult.Scored(0.0).TryGetError(out double zero));
        Assert.Equal(0.0, zero);
    }

    [Fact]
    public void TheDefault_IsNotRecorded()
    {
        Assert.Equal(PlayerResult.NotRecorded, default);
        Assert.Equal(PlayerResultKind.NotRecorded, default(PlayerResultKind));
        Assert.False(default(PlayerResult).TryGetError(out _));
    }

    [Fact]
    public void Equality_IsTheCaseAndTheError()
    {
        Assert.Equal(PlayerResult.Scored(0.1), PlayerResult.Scored(0.1));
        Assert.True(PlayerResult.Scored(0.1) == PlayerResult.Scored(0.1));
        Assert.Equal(PlayerResult.Scored(0.1).GetHashCode(), PlayerResult.Scored(0.1).GetHashCode());
        Assert.NotEqual(PlayerResult.Scored(0.1), PlayerResult.Unstated(0.1));
        Assert.NotEqual(PlayerResult.Scored(0.1), PlayerResult.Scored(0.2));
        Assert.True(PlayerResult.NotScored != PlayerResult.NotRecorded);
        Assert.False(PlayerResult.Scored(0.1).Equals((object)0.1));
    }

    [Fact]
    public void ToString_IsTheCase_AndTheErrorWhereThereIsOne()
    {
        Assert.Equal("NotRecorded", PlayerResult.NotRecorded.ToString());
        Assert.Equal("NotScored", PlayerResult.NotScored.ToString());
        Assert.Equal("Scored 0.032", PlayerResult.Scored(0.032).ToString());
        Assert.Equal("Unstated 0.031", PlayerResult.Unstated(0.031).ToString());
    }

    [Fact]
    public void EachKind_HasItsLabel()
    {
        Assert.Equal(
            [PlayerResultKind.NotRecorded, PlayerResultKind.NotScored, PlayerResultKind.Scored, PlayerResultKind.Unstated],
            Enum.GetValues<PlayerResultKind>());
        Assert.Equal(["Not recorded", "Not scored", "Scored", "Unstated"], Enum.GetValues<PlayerResultKind>().Select(kind =>
            typeof(PlayerResultKind).GetField(kind.ToString())!.GetCustomAttribute<DescriptionAttribute>()!.Description));
    }
}
