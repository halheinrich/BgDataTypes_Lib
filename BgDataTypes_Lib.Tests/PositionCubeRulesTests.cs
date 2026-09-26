using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The position's cube is well-formed by construction
/// (<see cref="PositionData"/>, halheinrich/backgammon#273, the match-context
/// leg), so the XGID derived from it (<see cref="BgDecisionData.Xgid"/>) can
/// never be wrong for a record that exists: the cube's value is a positive
/// power of two — the XGID states its exponent — never above a money
/// session's cube limit, and its owner a defined one. Each is refused from
/// code (the guard's exception, naming the member that completed the
/// contradiction, in either member order) and from a document (a
/// <see cref="JsonException"/> on both paths, carrying it).
/// </summary>
public class PositionCubeRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(3)]
    [InlineData(6)]
    public void ACubeThatIsNotAPositivePowerOfTwo_IsRefused(int cubeSize)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.Position(cubeSize: cubeSize));
        Assert.Equal("CubeSize", ex.ParamName);
        Assert.Contains(SessionRules.CubeSizeMessage, ex.Message);

        var document = WirePaths.Document(TestRecords.Position());
        document["CubeSize"] = cubeSize;
        Assert.IsType<ArgumentOutOfRangeException>(WirePaths.AssertRefused<PositionData>(document.ToJsonString()).InnerException);
    }

    [Fact]
    public void AnUndefinedOwner_IsRefused()
    {
        // Code alone can state one — the strict enum token refuses it on the
        // wire before any guard runs.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.Position(cubeOwner: (CubeOwner)7));
        Assert.Equal("CubeOwner", ex.ParamName);

        var document = WirePaths.Document(TestRecords.Position());
        document["CubeOwner"] = "Nobody";
        WirePaths.AssertRefused<PositionData>(document.ToJsonString());
    }

    [Fact]
    public void ACubeAboveAMoneySessionsLimit_IsRefused_NamingWhicheverMemberCompletesIt()
    {
        var limitOfFour = TestRecords.MoneySession(cubeLimit: 4);

        var cubeSecond = Assert.Throws<ArgumentOutOfRangeException>(() => new PositionData
        {
            Mop = BoardPosition.Standard, CubeOwner = CubeOwner.OnRoll, Session = limitOfFour, CubeSize = 8,
        });
        Assert.Equal("CubeSize", cubeSecond.ParamName);
        Assert.Contains(SessionRules.CubeWithinLimitMessage, cubeSecond.Message);

        var sessionSecond = Assert.Throws<ArgumentOutOfRangeException>(() => new PositionData
        {
            Mop = BoardPosition.Standard, CubeOwner = CubeOwner.OnRoll, CubeSize = 8, Session = limitOfFour,
        });
        Assert.Equal("Session", sessionSecond.ParamName);

        // At the limit itself, and in a match, which has no limit, it builds.
        Assert.Equal(4, TestRecords.Position(cubeSize: 4, cubeOwner: CubeOwner.OnRoll, session: limitOfFour).CubeSize);
        Assert.Equal(4096, TestRecords.Position(cubeSize: 4096, cubeOwner: CubeOwner.Opponent).CubeSize);
    }

    [Fact]
    public void ACubeAboveTheLimit_InADocument_IsRefused_InEitherPropertyOrder_BothPaths()
    {
        foreach (bool cubeFirst in new[] { true, false })
        {
            var document = WirePaths.Document(TestRecords.Position(
                cubeSize: 2, cubeOwner: CubeOwner.OnRoll, session: TestRecords.MoneySession(cubeLimit: 4)));
            document.Remove("CubeSize");
            if (cubeFirst)
                document.Insert(0, "CubeSize", 8);
            else
                document.Add("CubeSize", 8);

            var ex = WirePaths.AssertRefused<PositionData>(document.ToJsonString());
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            Assert.Contains(SessionRules.CubeWithinLimitMessage, ex.Message);
        }
    }
}
