namespace BgDataTypes_Lib.Tests;

public class IMatchInfoTests
{
    /// <summary>
    /// Minimal implementer declaring only the abstract members, so the
    /// money-game default implementation is what these tests exercise.
    /// </summary>
    private sealed class FakeMatchInfo : IMatchInfo
    {
        public string Player1 { get; init; } = string.Empty;
        public string Player2 { get; init; } = string.Empty;
        public int MatchLength { get; init; }
    }

    [Fact]
    public void IMatchInfo_Members_SurfaceThroughContract()
    {
        IMatchInfo info = new FakeMatchInfo { Player1 = "Mochy", Player2 = "Falafel", MatchLength = 9 };

        Assert.Equal("Mochy", info.Player1);
        Assert.Equal("Falafel", info.Player2);
        Assert.Equal(9, info.MatchLength);
    }

    [Fact]
    public void IMatchInfo_IsMoneyGame_TrueForUnlimitedSession()
    {
        // Producer contract: XG's raw 99999 sentinel is normalized to 0 at the
        // parse boundary, so 0 is the only spelling of "money" this contract sees.
        IMatchInfo info = new FakeMatchInfo { Player1 = "Mochy", Player2 = "Falafel", MatchLength = 0 };

        Assert.True(info.IsMoneyGame);
    }

    [Theory]
    [InlineData(1)]  // shortest possible match
    [InlineData(7)]
    [InlineData(25)]
    public void IMatchInfo_IsMoneyGame_FalseForAnyMatchLength(int matchLength)
    {
        IMatchInfo info = new FakeMatchInfo { MatchLength = matchLength };

        Assert.False(info.IsMoneyGame);
    }
}
