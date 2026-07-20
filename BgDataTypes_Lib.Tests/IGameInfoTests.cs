namespace BgDataTypes_Lib.Tests;

public class IGameInfoTests
{
    /// <summary>Minimal implementer pinning the contract's member shape.</summary>
    private sealed class FakeGameInfo : IGameInfo
    {
        public bool IsStandardStart { get; init; }
        public int Away1 { get; init; }
        public int Away2 { get; init; }
        public bool IsCrawfordGame { get; init; }
    }

    [Fact]
    public void IGameInfo_Members_SurfaceThroughContract()
    {
        IGameInfo info = new FakeGameInfo
        {
            IsStandardStart = true,
            Away1 = 3,
            Away2 = 5,
            IsCrawfordGame = false
        };

        Assert.True(info.IsStandardStart);
        Assert.Equal(3, info.Away1);
        Assert.Equal(5, info.Away2);
        Assert.False(info.IsCrawfordGame);
    }

    [Fact]
    public void IGameInfo_CrawfordGame_SurfacesThroughContract()
    {
        IGameInfo info = new FakeGameInfo { IsStandardStart = true, Away1 = 1, Away2 = 4, IsCrawfordGame = true };

        Assert.True(info.IsCrawfordGame);
    }

    [Fact]
    public void IGameInfo_MoneySessionConvention()
    {
        // Money sessions carry the documented convention:
        // Away1 = 0, Away2 = 0, IsCrawfordGame = false.
        IGameInfo info = new FakeGameInfo { IsStandardStart = true };

        Assert.Equal(0, info.Away1);
        Assert.Equal(0, info.Away2);
        Assert.False(info.IsCrawfordGame);
    }
}
