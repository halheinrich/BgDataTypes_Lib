using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The builders every test builds records with, here and in every consumer
/// (<see cref="TestRecords"/>): each yields a well-formed value of its type
/// with realistic defaults, so a test states only what it cares about. Pinned
/// here: the defaults build, read back unchanged on both paths, hold the
/// realistic values the builders document, and adapt to an id or a board
/// exactly as documented.
/// </summary>
public class TestRecordsTests
{
    [Fact]
    public void EveryBuilder_BuildsWithNoArgument()
    {
        Assert.NotNull(TestRecords.CheckerPlay());
        Assert.NotNull(TestRecords.Cube());
        Assert.NotNull(TestRecords.Row());
        Assert.NotNull(TestRecords.Position());
        Assert.NotNull(TestRecords.MatchSession());
        Assert.NotNull(TestRecords.MoneySession());
        Assert.NotNull(TestRecords.CheckerPlayData());
        Assert.NotNull(TestRecords.CubeData());
        Assert.NotNull(TestRecords.Descriptive());
        Assert.NotNull(TestRecords.Candidate());
    }

    [Fact]
    public void TheDefaults_ReadBackUnchanged_BothPaths()
    {
        foreach (BgDecisionData record in new BgDecisionData[] { TestRecords.CheckerPlay(), TestRecords.Cube() })
            foreach (var (_, options) in WirePaths.Both)
            {
                var json = JsonSerializer.Serialize(record, options);
                Assert.Equal(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<BgDecisionData>(json, options), options));

                var rowJson = JsonSerializer.Serialize(TestRecords.Row(record), options);
                Assert.Equal(rowJson, JsonSerializer.Serialize(JsonSerializer.Deserialize<DecisionRow>(rowJson, options), options));
            }
    }

    [Fact]
    public void TheCheckerPlay_IsTheOpening31_WithItsThreeCandidates()
    {
        var play = TestRecords.CheckerPlay();

        Assert.Equal(BoardPosition.Standard, play.Position.Mop);
        Assert.Equal(new DiceRoll(3, 1), play.Dice);
        Assert.Equal(["8/5 6/5", "13/10 6/5", "24/23 13/10"], play.Decision.Plays.Select(c => c.Notation).ToArray());
        Assert.Equal(0, play.Decision.UserPlayIndex);
        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var ranked = play.Decision.RankedBy(ranking);
            Assert.Equal(0, ranked.Best.Index);
            Assert.Equal(0.0, ranked.ForCandidate(0).Error);
            Assert.Equal(PlayerResult.Scored(0.0), ranked.PlayerResult);
        }
        Assert.Equal(167, play.Position.OnRollPipCount);
        Assert.Equal(167, play.Position.OpponentPipCount);
        Assert.Equal(new XgDecisionId("match.xg", 1, 1, IsCube: false), play.Id);
        Assert.True(play.IsStandardStart);
    }

    [Fact]
    public void TheSessions_AreASevenPointMatchAtZeroZero_AndMoneyUnderJacoby()
    {
        // Added with the session kinds (halheinrich/backgammon#273): the
        // position's default session is the match both records sit in, and
        // money's default terms are XG's defaults. Rewritten for the composed
        // session: the builders reach Session.Create with the player on roll
        // in seat 1, so each argument is the member of the same name.
        var match = TestRecords.MatchSession();
        var money = TestRecords.MoneySession();

        Assert.Equal((7, 7, 7, false), (match.Terms.Length, match.OnRollNeeds, match.OpponentNeeds, match.IsCrawford));
        Assert.Equal((true, false, 1024, 0, 0),
            (money.Terms.IsJacoby, money.Terms.IsBeaver, money.Terms.CubeLimit, money.OnRollScore, money.OpponentScore));
        var custom = TestRecords.MatchSession(length: 9, onRollNeeds: 2, opponentNeeds: 5);
        Assert.Equal((9, 2, 5), (custom.Terms.Length, custom.OnRollNeeds, custom.OpponentNeeds));
        var scored = TestRecords.MoneySession(onRollScore: 4, opponentScore: 1);
        Assert.Equal((4, 1), (scored.OnRollScore, scored.OpponentScore));
        Assert.Equal(match, TestRecords.Position().Session);
        Assert.Equal(match, TestRecords.CheckerPlay().Session);
        Assert.Equal(match, TestRecords.Cube().Session);
    }

    [Fact]
    public void TheCube_IsADoubleTake_InARace()
    {
        var cube = TestRecords.Cube();

        Assert.Equal(CubeAnswer.DoubleTake, cube.Decision.BestAnswer);
        Assert.True(new BoardState(cube.Position.Mop).IsRace);
        Assert.Equal(54, cube.Position.OnRollPipCount);
        Assert.Equal(65, cube.Position.OpponentPipCount);
        Assert.Equal(CubeOwner.Centered, cube.Position.CubeOwner);
        Assert.Equal(new XgDecisionId("match.xg", 1, 2, IsCube: true), cube.Id);
        // Nothing borne off, the cube below the 7 points needed: a consumer's
        // test building on the default has gammons possible.
        Assert.True(cube.GammonsPossible);
    }

    [Fact]
    public void AStandaloneId_GetsNoStart()
    {
        // The record rules demand it; the builder follows the id.
        var play = TestRecords.CheckerPlay(id: new XgpDecisionId("p.xgp"));
        var cube = TestRecords.Cube(id: new XgpDecisionId("q.xgp"));

        Assert.Null(play.IsStandardStart);
        Assert.Null(cube.IsStandardStart);
        Assert.Equal("p.xgp", play.SourceFile);
    }

    [Fact]
    public void AnotherBoard_GetsThePass_UnlessTheTestStatesItsDecision()
    {
        var board = new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, 0]);

        var play = TestRecords.CheckerPlay(position: TestRecords.Position(mop: board));

        var candidate = Assert.Single(play.Decision.Plays);
        Assert.Equal(0, candidate.Play.Count);
        Assert.Equal(board.Flipped(), play.AfterBoardOfBest(PlayRanking.Equity));

        // A stated decision is taken as stated: the opening's candidates are
        // not valid from this board, so the record cannot be built.
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: board), decision: TestRecords.CheckerPlayData()));
    }

    [Fact]
    public void ThePosition_RefusesABoardNoDecisionIsMadeOn()
    {
        // Added (halheinrich/backgammon#273, Hal's ruling of 2026-09-27): the
        // builder builds through the category, so it cannot make a decision
        // position the category refuses — a side borne off, or the empty
        // board — and no record builder can be handed one.
        var opponentBorneOff = new BoardPosition([0, 3, 3, 3, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

        Assert.Equal("Mop", Assert.Throws<ArgumentException>(() => TestRecords.Position(mop: opponentBorneOff)).ParamName);
        Assert.Equal("Mop", Assert.Throws<ArgumentException>(() => TestRecords.Position(mop: BoardPosition.Empty)).ParamName);
    }

    [Fact]
    public void ThePosition_CountsItsOwnPips()
    {
        // Rewritten: the pip counts are the category's derivation from its
        // board, not the builder's defaults, so there is no count to override.
        var board = new BoardPosition([0, 2, 0, 0, 0, 0, -5, 0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2, 0]);
        var position = TestRecords.Position(mop: board);

        Assert.Equal(new BoardState(board).PipCount, position.OnRollPipCount);
        Assert.Equal(new BoardState(board).OpponentPipCount, position.OpponentPipCount);
        Assert.DoesNotContain(typeof(TestRecords).GetMethod(nameof(TestRecords.Position))!.GetParameters(),
            p => p.Name!.Contains("Pip", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheRow_IsTheProjectionOfTheDefaultCheckerPlay_UnderTheDefaultRanking()
    {
        var row = TestRecords.Row();
        var expected = DecisionRow.From(TestRecords.CheckerPlay(), PlayRanking.Equity);

        Assert.Equal(
            JsonSerializer.Serialize(expected, WirePaths.Context),
            JsonSerializer.Serialize(row, WirePaths.Context));
        Assert.Equal(PlayRanking.DepthFirst, TestRecords.Row(ranking: PlayRanking.DepthFirst).Ranking);
    }
}
