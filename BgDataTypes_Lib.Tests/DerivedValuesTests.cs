using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// No stored copy of a derivable value (the umbrella's verdict on the records
/// leg of halheinrich/backgammon#273): each member the others determine is
/// derived from them, never stored, so it cannot disagree with what it is
/// derived from. Pinned here, one derivation each: the pip counts from the
/// board, the source file from the id, the depth rank from the mode and
/// level, the best play from the candidates' equities, each candidate's loss
/// against it, the user's error where the record determines it, and the
/// error of each stated cube action. The one error each kind stores is the
/// one nothing determines, and it cannot be stated beside what would
/// determine it.
/// </summary>
public class DerivedValuesTests
{
    // ── The pip counts, from the board ────────────────────────────

    [Fact]
    public void PipCounts_AreTheBoardsOwn_BarsIncluded()
    {
        // On roll: 2 on the 1, 1 on the bar (25). Opponent: 5 on its 19
        // (slot 6), 2 on its bar (slot 0).
        var mop = new int[26];
        mop[1] = 2; mop[25] = 1; mop[6] = -5; mop[0] = -2;
        var position = TestRecords.Position(mop: new BoardPosition(mop));

        Assert.Equal(2 * 1 + 1 * 25, position.OnRollPipCount);
        Assert.Equal(5 * 19 + 2 * 25, position.OpponentPipCount);
        Assert.Equal(new BoardState(new BoardPosition(mop)).PipCount, position.OnRollPipCount);
        Assert.Equal(new BoardState(new BoardPosition(mop)).OpponentPipCount, position.OpponentPipCount);
    }

    [Fact]
    public void PipCounts_AreNotOnTheWire_AndAStatedOneIsIgnored_BothPaths()
    {
        var position = TestRecords.Position();
        var document = WirePaths.Document(position);
        Assert.False(document.ContainsKey("OnRollPipCount"));
        Assert.False(document.ContainsKey("OpponentPipCount"));

        document["OnRollPipCount"] = 99;
        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal(167, JsonSerializer.Deserialize<PositionData>(document.ToJsonString(), options)!.OnRollPipCount);
    }

    [Fact]
    public void ReadingThePipCounts_AllocatesNothing()
    {
        var position = TestRecords.Position();
        int sink = position.OnRollPipCount + position.OpponentPipCount;   // warm

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            sink += position.OnRollPipCount + position.OpponentPipCount;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.NotEqual(int.MinValue, sink);
    }

    // ── The source file, from the id ──────────────────────────────

    [Fact]
    public void SourceFile_IsTheIdsFilename_OnTheRecordAndItsRow_EachShapeOfId()
    {
        foreach (DecisionId id in new DecisionId[] { new XgDecisionId("m.xg", 2, 5, IsCube: true), new XgpDecisionId("p.xgp") })
        {
            var record = TestRecords.Cube(id: id);
            Assert.Equal(id.Filename, record.SourceFile);
            Assert.Equal(id.Filename, DecisionRow.From(record).SourceFile);
        }
    }

    // ── The depth rank, from the mode and level ───────────────────

    [Theory]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply1, 10)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply2, 20)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply3Red, 25)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply3, 30)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.XgRoller, 35)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply4, 40)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.XgRollerPlus, 45)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply5, 50)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply6, 60)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Ply7, 70)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.XgRollerPlusPlus, 75)]
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Unknown, 0)]
    [InlineData(AnalysisMode.BookRollout, AnalysisLevel.Unknown, 99)]
    [InlineData(AnalysisMode.BookRollout, AnalysisLevel.Ply4, 99)]
    [InlineData(AnalysisMode.Rollout, AnalysisLevel.Unknown, 100)]
    [InlineData(AnalysisMode.Rollout, AnalysisLevel.Ply3, 130)]
    [InlineData(AnalysisMode.Rollout, AnalysisLevel.XgRoller, 135)]
    [InlineData(AnalysisMode.Unknown, AnalysisLevel.Unknown, 0)]
    [InlineData(AnalysisMode.Unknown, AnalysisLevel.Ply3, 0)]
    public void DepthRank_IsTheGridsRankOfTheModeAndLevel_OnACandidateAndACube(AnalysisMode mode, AnalysisLevel level, int rank)
    {
        // The producer's grid, unchanged in its move here: an evaluation by
        // its level, a book hit 99, a rollout 100 plus its inner level, and
        // the floor 0 where the mode, or an evaluation's level, is unknown.
        Assert.Equal(rank, TestRecords.Candidate(analysisMode: mode, analysisLevel: level).DepthRank);
        Assert.Equal(rank, TestRecords.CubeData(analysisMode: mode, analysisLevel: level).DepthRank);
    }

    [Fact]
    public void DepthRank_OrdersTheEvaluationLevels_AsTheLevelsDeclarationDoes()
    {
        // The grid's order is AnalysisLevel's contractual rigor order.
        var levels = Enum.GetValues<AnalysisLevel>().Where(l => l != AnalysisLevel.Unknown).ToArray();
        var ranks = levels.Select(l => TestRecords.Candidate(analysisLevel: l).DepthRank).ToArray();

        Assert.Equal(ranks.Order().ToArray(), ranks);
        Assert.Equal(ranks.Length, ranks.Distinct().Count());
    }

    // ── The best play, and each candidate's loss against it ───────

    [Fact]
    public void BestPlay_IsTheFirstOfTheHighestEquity_WhateverTheOrder()
    {
        Play[] plays = [[new(8, 5), new(6, 5)], [new(13, 10), new(6, 5)], [new(24, 23), new(13, 10)]];
        var data = TestRecords.CheckerPlayData(plays:
        [
            TestRecords.Candidate(play: plays[0], equity: -0.2),
            TestRecords.Candidate(play: plays[1], equity: 0.1),
            TestRecords.Candidate(play: plays[2], equity: 0.1),
        ]);

        Assert.Equal(1, data.BestPlayIndex);
        Assert.Equal([0.30000000000000004, 0.0, 0.0], [data.EquityLoss(0), data.EquityLoss(1), data.EquityLoss(2)]);
        Assert.Throws<ArgumentOutOfRangeException>(() => data.EquityLoss(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => data.EquityLoss(-1));
    }

    [Fact]
    public void BestPlay_AndEveryLoss_AreNeverNegative()
    {
        // The best has the highest equity, so no candidate loses less than 0.
        var data = TestRecords.CheckerPlayData();
        for (int i = 0; i < data.Plays.Count; i++)
            Assert.True(data.EquityLoss(i) >= 0.0, $"candidate {i} loses {data.EquityLoss(i)}");
    }

    // ── The user's error ──────────────────────────────────────────

    [Fact]
    public void UserPlayError_OfAListedPlay_IsItsLoss_OfAnUnlistedOne_IsTheAnalysers()
    {
        var listed = TestRecords.CheckerPlayData(userPlayIndex: 2);
        var unlisted = TestRecords.CheckerPlayData(userPlayIndex: null, unlistedPlayError: 0.07);
        var none = TestRecords.CheckerPlayData(userPlayIndex: null);

        Assert.Equal(listed.EquityLoss(2), listed.UserPlayError);
        Assert.Null(listed.UnlistedPlayError);
        Assert.Equal(0.07, unlisted.UserPlayError);
        Assert.Null(none.UserPlayError);
    }

    [Fact]
    public void AnUnlistedPlaysError_CannotBeStatedBesideAListedPlay_EitherOrder_OrOnTheWire()
    {
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlayData(userPlayIndex: 0, unlistedPlayError: 0.1));

        var document = WirePaths.Document(TestRecords.CheckerPlayData(userPlayIndex: 0));
        document["UnlistedPlayError"] = 0.1;
        var ex = WirePaths.AssertRefused<CheckerPlayDecisionData>(document.ToJsonString());
        Assert.IsType<ArgumentException>(ex.InnerException);
    }

    // ── The cube errors ───────────────────────────────────────────

    [Theory]
    [InlineData(CubeAction.Double, 0.0)]      // double/take +0.634 beats no double +0.512
    [InlineData(CubeAction.NoDouble, 0.122)]
    public void UserDoubleError_OfAStatedAction_IsThePolicysErrorOfIt(CubeAction played, double error)
    {
        var data = TestRecords.CubeData(userDoublerAction: played, userTakerAction: null);

        Assert.Equal(data.DoublerActionError(played), data.UserDoubleError);
        Assert.Equal(error, data.UserDoubleError!.Value, 12);
    }

    [Theory]
    [InlineData(CubeAction.Take, 0.0)]        // double/take +0.634 is under the cash
    [InlineData(CubeAction.Pass, 0.366)]
    public void UserTakeError_OfAStatedAction_IsThePolicysErrorOfIt(CubeAction played, double error)
    {
        var data = TestRecords.CubeData(userTakerAction: played);

        Assert.Equal(data.TakerActionError(played), data.UserTakeError);
        Assert.Equal(error, data.UserTakeError!.Value, 12);
    }

    [Fact]
    public void ACubeError_OfAnUnstatedAction_IsTheAnalysers_AndCannotBeStatedBesideTheAction()
    {
        var unstated = TestRecords.CubeData(
            userDoublerAction: null, userTakerAction: null,
            unstatedDoublerActionError: 0.05, unstatedTakerActionError: 0.06);
        Assert.Equal(0.05, unstated.UserDoubleError);
        Assert.Equal(0.06, unstated.UserTakeError);

        Assert.Throws<ArgumentException>(() => TestRecords.CubeData(unstatedDoublerActionError: 0.05));
        Assert.Throws<ArgumentException>(() => TestRecords.CubeData(unstatedTakerActionError: 0.06));
        var doublerSecond = Assert.Throws<ArgumentException>(() => new CubeDecisionData
        {
            Depth = "3-ply", DepthAbbreviation = "3-ply",
            AnalysisMode = AnalysisMode.Evaluation, AnalysisLevel = AnalysisLevel.Ply3,
            NoDoubleEquity = 0.5, DoubleTakeEquity = 0.6, CubelessNoDoubleEquity = 0.4, CubelessDoubleTakeEquity = 0.4,
            WinPctAfterNoDouble = 0.7, GammonPctAfterNoDouble = 0, BgPctAfterNoDouble = 0,
            LosePctAfterNoDouble = 0.3, LoseGammonPctAfterNoDouble = 0, LoseBgPctAfterNoDouble = 0,
            WinPctAfterDoubleTake = 0.7, GammonPctAfterDoubleTake = 0, BgPctAfterDoubleTake = 0,
            LosePctAfterDoubleTake = 0.3, LoseGammonPctAfterDoubleTake = 0, LoseBgPctAfterDoubleTake = 0,
            ProbOfOpponentErrorJustifyingDouble = 0,
            UnstatedDoublerActionError = 0.05,
            UserDoublerAction = CubeAction.Double,
        });
        Assert.Equal("UserDoublerAction", doublerSecond.ParamName);
    }

    // ── Retired copies on the wire ────────────────────────────────

    [Fact]
    public void ARecordDocument_StatingEveryRetiredCopy_ReadsWithThemIgnored_TheDerivationsStand_BothPaths()
    {
        // A document from a producer that still states the copies loads, and
        // what it stated never overrides a derivation: the serializer knows
        // each derived member and skips its JSON, in the categories that
        // refuse unknown members too (measured on .NET 10, both paths).
        var record = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: 1));
        var document = WirePaths.Document<BgDecisionData>(record);
        document["Position"]!["OnRollPipCount"] = 1;
        document["Position"]!["OpponentPipCount"] = 2;
        document["Descriptive"]!["SourceFile"] = "other.xg";
        document["Decision"]!["BestPlayIndex"] = 2;
        document["Decision"]!["UserPlayError"] = 9.0;
        document["Decision"]!["Plays"]![0]!["DepthRank"] = 1;
        document["Decision"]!["Plays"]![0]!["EquityLoss"] = 9.0;

        foreach (var (_, options) in WirePaths.Both)
        {
            var read = Assert.IsType<CheckerPlayDecision>(JsonSerializer.Deserialize<BgDecisionData>(document.ToJsonString(), options));
            Assert.Equal(record.Position.OnRollPipCount, read.Position.OnRollPipCount);
            Assert.Equal(record.Position.OpponentPipCount, read.Position.OpponentPipCount);
            Assert.Equal("match.xg", read.SourceFile);
            Assert.Equal(0, read.Decision.BestPlayIndex);
            Assert.Equal(record.Decision.UserPlayError, read.Decision.UserPlayError);
            Assert.Equal(30, read.Decision.Plays[0].DepthRank);
            Assert.Equal(0.0, read.Decision.EquityLoss(0));
        }

        var cube = WirePaths.Document<BgDecisionData>(TestRecords.Cube());
        cube["Decision"]!["DepthRank"] = 1;
        cube["Decision"]!["UserDoubleError"] = 9.0;
        cube["Decision"]!["UserTakeError"] = 9.0;
        foreach (var (_, options) in WirePaths.Both)
        {
            var read = Assert.IsType<CubeDecision>(JsonSerializer.Deserialize<BgDecisionData>(cube.ToJsonString(), options));
            Assert.Equal(30, read.Decision.DepthRank);
            Assert.Equal(0.0, read.Decision.UserDoubleError);
            Assert.Equal(0.0, read.Decision.UserTakeError);
        }
    }
}
