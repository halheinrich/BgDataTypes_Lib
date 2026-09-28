using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// No stored copy of a derivable value (the umbrella's verdict on the records
/// leg of halheinrich/backgammon#273): each member the others determine is
/// derived from them, never stored, so it cannot disagree with what it is
/// derived from. Pinned here, one derivation each: the pip counts from the
/// board, the source file from the id, the depth rank from the mode and
/// level, the error of each stated cube action, and the loss probabilities
/// from the win probabilities. The best play, each candidate's error and the
/// user's play error are a ranking's derivations, pinned in
/// <see cref="PlayRankingTests"/>. The one error each kind stores is the one
/// nothing determines, and it cannot be stated beside what would determine
/// it.
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
        // Measured through AllocationProbe, which warms the path and is
        // immune to a one-off allocation that is not the path's.
        var position = TestRecords.Position();
        int sink = 0;

        long allocated = AllocationProbe.SteadyStateBytes(
            () => sink += position.OnRollPipCount + position.OpponentPipCount);

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
            Assert.Equal(id.Filename, TestRecords.Row(record).SourceFile);
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
    [InlineData(AnalysisMode.Evaluation, AnalysisLevel.Unknown, null)]
    [InlineData(AnalysisMode.BookRollout, AnalysisLevel.Unknown, 99)]
    [InlineData(AnalysisMode.BookRollout, AnalysisLevel.Ply4, 99)]
    [InlineData(AnalysisMode.Rollout, AnalysisLevel.Unknown, 100)]
    [InlineData(AnalysisMode.Rollout, AnalysisLevel.Ply3, 130)]
    [InlineData(AnalysisMode.Rollout, AnalysisLevel.XgRoller, 135)]
    [InlineData(AnalysisMode.Unknown, AnalysisLevel.Unknown, null)]
    [InlineData(AnalysisMode.Unknown, AnalysisLevel.Ply3, null)]
    public void DepthRank_IsTheGridsRankOfTheModeAndLevel_OnACandidateAndACube(AnalysisMode mode, AnalysisLevel level, int? rank)
    {
        // The producer's grid, unchanged in its move here: an evaluation by
        // its level, a book hit 99, a rollout 100 plus its inner level. Where
        // the mode, or an evaluation's level, is unknown there is no rank —
        // null, "none recorded" — where the producer wrote the floor, 0.
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

    // ── The user's error ──────────────────────────────────────────
    //
    // The best play, each candidate's error against it, and the user's error
    // where a candidate is the user's are a ranking's derivations, pinned
    // under each ranking in PlayRankingTests. The one play error stored is an
    // unlisted play's, which no ranking changes.

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
            AnalysisMode = AnalysisMode.Evaluation, AnalysisLevel = AnalysisLevel.Ply3,
            NoDoubleEquity = 0.5, DoubleTakeEquity = 0.6, CubelessNoDoubleEquity = 0.4, CubelessDoubleTakeEquity = 0.4,
            WinPctAfterNoDouble = 0.7, GammonPctAfterNoDouble = 0, BgPctAfterNoDouble = 0,
            LoseGammonPctAfterNoDouble = 0, LoseBgPctAfterNoDouble = 0,
            WinPctAfterDoubleTake = 0.7, GammonPctAfterDoubleTake = 0, BgPctAfterDoubleTake = 0,
            LoseGammonPctAfterDoubleTake = 0, LoseBgPctAfterDoubleTake = 0,
            UnstatedDoublerActionError = 0.05,
            UserDoublerAction = CubeAction.Double,
        });
        Assert.Equal("UserDoublerAction", doublerSecond.ParamName);
    }

    [Fact]
    public void AnUnstatedTakerError_ThenATakerAction_IsRefusedToo_FromCodeAndFromADocument()
    {
        // Added after a mutation check: the builder and a written document
        // both state the action before its unstated-action error, so only
        // the error's own guard ran. The action's guard is order-independence's
        // other half — here the error is stated first, in an initializer and
        // in a document (the reflection path sets members in document order).
        var takerSecond = Assert.Throws<ArgumentException>(() => new CubeDecisionData
        {
            AnalysisMode = AnalysisMode.Evaluation, AnalysisLevel = AnalysisLevel.Ply3,
            NoDoubleEquity = 0.5, DoubleTakeEquity = 0.6, CubelessNoDoubleEquity = 0.4, CubelessDoubleTakeEquity = 0.4,
            WinPctAfterNoDouble = 0.7, GammonPctAfterNoDouble = 0, BgPctAfterNoDouble = 0,
            LoseGammonPctAfterNoDouble = 0, LoseBgPctAfterNoDouble = 0,
            WinPctAfterDoubleTake = 0.7, GammonPctAfterDoubleTake = 0, BgPctAfterDoubleTake = 0,
            LoseGammonPctAfterDoubleTake = 0, LoseBgPctAfterDoubleTake = 0,
            UnstatedTakerActionError = 0.06,
            UserTakerAction = CubeAction.Take,
        });
        Assert.Equal("UserTakerAction", takerSecond.ParamName);

        var document = WirePaths.Document(TestRecords.CubeData(userTakerAction: null, unstatedTakerActionError: 0.06));
        document.Remove("UserTakerAction");
        document["UserTakerAction"] = "Take";
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CubeDecisionData>(document.ToJsonString(), WirePaths.Reflection));
        Assert.Equal("UserTakerAction", Assert.IsType<ArgumentException>(ex.InnerException).ParamName);
    }

    // ── The loss probabilities, from the win probabilities ────────

    [Fact]
    public void LosePct_IsOneMinusWinPct_OnACandidate_AndNoneWhenNotEvaluated()
    {
        // Every game is won or lost, so the total loss is 1 − the total win:
        // XG's stored figure matched it within 9.5e-7 over 273,592 corpus
        // candidates (the umbrella's measurement, 2026-09-26).
        Assert.Equal(1.0 - 0.5358, TestRecords.Candidate(winPct: 0.5358).LosePct);
        Assert.Null(TestRecords.Candidate(winPct: null).LosePct);
    }

    [Fact]
    public void LosePct_IsOneMinusWinPct_OnBothCubeHalves()
    {
        var data = TestRecords.CubeData(winPctAfterNoDouble: 0.709, winPctAfterDoubleTake: 0.612);

        Assert.Equal(1.0 - 0.709, data.LosePctAfterNoDouble);
        Assert.Equal(1.0 - 0.612, data.LosePctAfterDoubleTake);
    }

    [Fact]
    public void TheLossProbabilities_AreNotOnTheWire_AndAStatedOneIsIgnored_BothPaths()
    {
        var candidate = WirePaths.Document(TestRecords.Candidate(winPct: 0.6));
        var cube = WirePaths.Document(TestRecords.CubeData(winPctAfterNoDouble: 0.7, winPctAfterDoubleTake: 0.6));
        Assert.False(candidate.ContainsKey("LosePct"));
        Assert.False(cube.ContainsKey("LosePctAfterNoDouble"));
        Assert.False(cube.ContainsKey("LosePctAfterDoubleTake"));

        candidate["LosePct"] = 0.9;
        cube["LosePctAfterNoDouble"] = 0.9;
        cube["LosePctAfterDoubleTake"] = 0.9;
        foreach (var (_, options) in WirePaths.Both)
        {
            Assert.Equal(1.0 - 0.6, JsonSerializer.Deserialize<PlayCandidate>(candidate.ToJsonString(), options)!.LosePct);
            var read = JsonSerializer.Deserialize<CubeDecisionData>(cube.ToJsonString(), options)!;
            Assert.Equal(1.0 - 0.7, read.LosePctAfterNoDouble);
            Assert.Equal(1.0 - 0.6, read.LosePctAfterDoubleTake);
        }
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
        document["Decision"]!["Plays"]![0]!["DepthRank"] = 1;
        document["Decision"]!["Plays"]![0]!["EquityLoss"] = 9.0;

        foreach (var (_, options) in WirePaths.Both)
        {
            var read = Assert.IsType<CheckerPlayDecision>(JsonSerializer.Deserialize<BgDecisionData>(document.ToJsonString(), options));
            Assert.Equal(record.Position.OnRollPipCount, read.Position.OnRollPipCount);
            Assert.Equal(record.Position.OpponentPipCount, read.Position.OpponentPipCount);
            Assert.Equal("match.xg", read.SourceFile);
            foreach (var ranking in Enum.GetValues<PlayRanking>())
            {
                var ranked = read.Decision.RankedBy(ranking);
                Assert.Equal(0, ranked.Best.Index);
                Assert.Equal(record.Decision.RankedBy(ranking).PlayerResult, ranked.PlayerResult);
                Assert.Equal(0.0, ranked.ForCandidate(0).Error);
            }
            Assert.Equal(30, read.Decision.Plays[0].DepthRank);
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
