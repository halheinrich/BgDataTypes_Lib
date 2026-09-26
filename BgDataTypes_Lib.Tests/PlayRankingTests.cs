using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The ranking of a checker play's candidates (SPEC-scoring §2a, Hal's ruling
/// on halheinrich/backgammon#282) — the one definition of the order, the best
/// play, each candidate's error, whether it is scored and the player's error,
/// each derived for a given ranking and pinned here under both.
/// <see cref="PlayRanking.Equity"/> orders by equity and depth has no effect;
/// <see cref="PlayRanking.DepthFirst"/> orders by depth rank, then equity;
/// ties keep the stored order. Under depth first a candidate at a different
/// depth from the best's whose equity is higher is not scored and has no
/// error; every other candidate is scored, and its error is never negative.
/// Nothing answers best or error without a ranking: the filter view and the
/// row are each built for one.
/// </summary>
public class PlayRankingTests
{
    // ── Fixtures ──────────────────────────────────────────────────

    /// <summary>
    /// Six candidates of the opening 3-1 at three depths, stored in an order
    /// neither ranking keeps (index: depth, rank, equity):
    /// 0: 3-ply evaluation, 30, +0.10;
    /// 1: 3-ply rollout, 130, +0.05;
    /// 2: 2-ply evaluation, 20, +0.12 — the highest equity;
    /// 3: 3-ply rollout, 130, +0.05 — tying 1 on both keys;
    /// 4: 3-ply evaluation, 30, −0.20;
    /// 5: 2-ply evaluation, 20, +0.05 — tying the depth-first best's equity at another depth.
    /// </summary>
    private static PlayCandidate[] Mixed() =>
    [
        TestRecords.Candidate(play: [new(8, 5), new(6, 5)], equity: 0.10),
        TestRecords.Candidate(play: [new(13, 10), new(6, 5)], analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296, equity: 0.05),
        TestRecords.Candidate(play: [new(24, 23), new(13, 10)], analysisLevel: AnalysisLevel.Ply2, equity: 0.12),
        TestRecords.Candidate(play: [new(24, 21), new(6, 5)], analysisMode: AnalysisMode.Rollout, rolloutTrials: 1296, equity: 0.05),
        TestRecords.Candidate(play: [new(13, 10), new(10, 9)], equity: -0.20),
        TestRecords.Candidate(play: [new(24, 21), new(21, 20)], analysisLevel: AnalysisLevel.Ply2, equity: 0.05),
    ];

    private static CheckerPlayDecisionData MixedData(int? userPlayIndex = null, double? unlistedPlayError = null) =>
        TestRecords.CheckerPlayData(plays: Mixed(), userPlayIndex: userPlayIndex, unlistedPlayError: unlistedPlayError);

    private static CheckerPlayDecision MixedRecord(int? userPlayIndex = null, double? unlistedPlayError = null) =>
        TestRecords.CheckerPlay(decision: MixedData(userPlayIndex, unlistedPlayError));

    // ── The order ─────────────────────────────────────────────────

    public static TheoryData<PlayRanking, int[]> Orders => new()
    {
        { PlayRanking.Equity, [2, 0, 1, 3, 5, 4] },
        { PlayRanking.DepthFirst, [1, 3, 0, 4, 2, 5] },
    };

    [Theory]
    [MemberData(nameof(Orders))]
    public void TheOrder_IsTheRankingsKeys_HighestFirst_TiesInTheStoredOrder(PlayRanking ranking, int[] order)
    {
        var data = MixedData();
        var ranked = data.RankedBy(ranking);

        Assert.Equal(ranking, ranked.Ranking);
        Assert.Equal(order.Length, ranked.Count);
        Assert.Equal(order, ranked.Select(p => p.Index));
        for (int position = 0; position < order.Length; position++)
        {
            Assert.Equal(order[position], ranked[position].Index);
            Assert.Equal(position + 1, ranked[position].Rank);
            Assert.Same(ranked[position], ranked.ForCandidate(order[position]));
            Assert.Same(data.Plays[order[position]], ranked[position].Candidate);
        }
    }

    [Fact]
    public void UnderEquity_DepthHasNoEffect_UnderDepthFirst_TheDeeperComesFirst()
    {
        // A shallower candidate stored before a deeper one of the same
        // equity: equity keeps the stored order; depth first does not.
        var data = TestRecords.CheckerPlayData(plays:
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], analysisLevel: AnalysisLevel.Ply1, equity: 0.1),
            TestRecords.Candidate(play: [new(13, 10), new(6, 5)], analysisMode: AnalysisMode.Rollout, equity: 0.1),
        ]);

        Assert.Equal([0, 1], data.RankedBy(PlayRanking.Equity).Select(p => p.Index));
        Assert.Equal([1, 0], data.RankedBy(PlayRanking.DepthFirst).Select(p => p.Index));
    }

    [Fact]
    public void Ties_KeepTheStoredOrder_UnderEachRanking_AtEverySize()
    {
        // The sort is not stable by itself; the stored order is its last key,
        // so equal candidates keep it at every size — past the size where the
        // sort stops inserting too.
        foreach (int count in new[] { 2, 3, 17, 40 })
        {
            var data = TestRecords.CheckerPlayData(plays: [.. Enumerable.Range(0, count).Select(_ => TestRecords.Candidate())]);
            foreach (var ranking in Enum.GetValues<PlayRanking>())
                Assert.Equal(Enumerable.Range(0, count), data.RankedBy(ranking).Select(p => p.Index));
        }
    }

    [Fact]
    public void OverManyDecisions_TheOrderIsAStableSortOfTheKeys_AndTheRuleHolds()
    {
        // Differential, over seeded random decisions: LINQ's sort is stable,
        // so ordering by the ranking's keys through it is the order the
        // definition states.
        foreach (var (trial, plays, data) in RandomDecisions())
        {
            foreach (var ranking in Enum.GetValues<PlayRanking>())
            {
                int[] expected = [.. Enumerable.Range(0, plays.Length)
                    .OrderByDescending(i => ranking == PlayRanking.DepthFirst ? plays[i].DepthRank ?? int.MinValue : 0)
                    .ThenByDescending(i => plays[i].Equity)];
                var ranked = data.RankedBy(ranking);
                Assert.Equal(expected, ranked.Select(p => p.Index));

                var best = plays[expected[0]];
                foreach (var play in ranked)
                {
                    bool notScored = ranking == PlayRanking.DepthFirst && play.Candidate.Equity > best.Equity;
                    Assert.Equal(notScored ? null : best.Equity - play.Candidate.Equity, play.Error);
                    Assert.Equal(!notScored, play.IsScored);
                    Assert.True(!play.IsScored || play.Error >= 0.0, $"trial {trial}: a scored error of {play.Error}");
                }
            }
        }
    }

    [Fact]
    public void OverManyDecisions_EveryUnscoredPlay_IsAtAnotherDepthFromTheRankingsBest()
    {
        // SPEC-scoring §2a's depth condition, as a property of the ranking's
        // own output: the rule is applied as the equity comparison, and the
        // order implies the rest. Checked on its own, so it guards the
        // implication whatever else a change breaks.
        foreach (var (trial, _, data) in RandomDecisions())
        {
            var ranked = data.RankedBy(PlayRanking.DepthFirst);
            foreach (var play in ranked.Where(play => !play.IsScored))
                Assert.True(play.Candidate.DepthRank != ranked.Best.Candidate.DepthRank,
                    $"trial {trial}: candidate {play.Index} is unscored at the best's depth");
        }
    }

    /// <summary>
    /// 2000 seeded random decisions of 1–24 candidates. Equities and depths
    /// are drawn from small sets so that ties are common, recorded and
    /// unrecorded depths alike.
    /// </summary>
    private static IEnumerable<(int Trial, PlayCandidate[] Plays, CheckerPlayDecisionData Data)> RandomDecisions()
    {
        (AnalysisMode Mode, AnalysisLevel Level)[] depths =
        [
            (AnalysisMode.Unknown, AnalysisLevel.Unknown), (AnalysisMode.Evaluation, AnalysisLevel.Unknown),
            (AnalysisMode.Evaluation, AnalysisLevel.Ply1), (AnalysisMode.Evaluation, AnalysisLevel.Ply3),
            (AnalysisMode.Evaluation, AnalysisLevel.XgRoller), (AnalysisMode.BookRollout, AnalysisLevel.XgRoller),
            (AnalysisMode.Rollout, AnalysisLevel.Ply3), (AnalysisMode.Rollout, AnalysisLevel.Unknown),
        ];
        var random = new Random(282);

        for (int trial = 0; trial < 2000; trial++)
        {
            PlayCandidate[] plays = [.. Enumerable.Range(0, random.Next(1, 25)).Select(_ =>
            {
                var (mode, level) = depths[random.Next(depths.Length)];
                return TestRecords.Candidate(analysisMode: mode, analysisLevel: level, equity: random.Next(-4, 5) * 0.05);
            })];
            yield return (trial, plays, TestRecords.CheckerPlayData(plays: plays));
        }
    }

    // ── The best play and each candidate's error ──────────────────

    [Theory]
    [InlineData(PlayRanking.Equity, 2)]
    [InlineData(PlayRanking.DepthFirst, 1)]
    public void TheBest_IsTheRankingsFirst(PlayRanking ranking, int best)
    {
        var data = MixedData();
        var ranked = data.RankedBy(ranking);

        Assert.Equal(best, ranked.Best.Index);
        Assert.Same(ranked[0], ranked.Best);
        Assert.Same(data.Plays[best], ranked.Best.Candidate);
        Assert.Equal(1, ranked.Best.Rank);
        Assert.Equal(0.0, ranked.Best.Error);
        Assert.True(ranked.Best.IsScored);
    }

    public static TheoryData<PlayRanking, double?[]> Errors => new()
    {
        // By candidate index: the best's equity minus each one's, or none
        // where the ranking does not score it.
        { PlayRanking.Equity, [0.02, 0.07, 0.0, 0.07, 0.32, 0.07] },
        { PlayRanking.DepthFirst, [null, 0.0, null, 0.0, 0.25, 0.0] },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public void EachScoredError_IsTheBestsEquityMinusItsOwn_AndAnUnscoredPlayHasNone(PlayRanking ranking, double?[] errors)
    {
        var data = MixedData();
        var ranked = data.RankedBy(ranking);

        for (int index = 0; index < errors.Length; index++)
        {
            var play = ranked.ForCandidate(index);
            Assert.Equal(index, play.Index);
            Assert.Equal(errors[index] is not null, play.IsScored);
            if (errors[index] is double expected)
            {
                Assert.Equal(ranked.Best.Candidate.Equity - data.Plays[index].Equity, play.Error);
                Assert.Equal(expected, play.Error!.Value, 12);
                Assert.True(play.Error >= 0.0);
            }
            else
            {
                Assert.Null(play.Error);
            }
        }
    }

    // ── Not scored, under depth first only ────────────────────────

    [Fact]
    public void AShallowerCandidateOfHigherEquity_IsNotScoredUnderDepthFirst_AndIsTheBestUnderEquity()
    {
        var data = MixedData();

        var shallower = data.RankedBy(PlayRanking.DepthFirst).ForCandidate(2);
        Assert.False(shallower.IsScored);
        Assert.Null(shallower.Error);

        var byEquity = data.RankedBy(PlayRanking.Equity);
        Assert.Equal(2, byEquity.Best.Index);
        Assert.All(byEquity, play => Assert.True(play.IsScored));
    }

    [Fact]
    public void EveryUnscoredCandidate_IsAtAnotherDepthFromTheBest()
    {
        // The rule is applied as the equity comparison; SPEC-scoring §2a's
        // depth condition follows from the order. On the fixture, and over
        // seeded random decisions in the differential test above.
        var ranked = MixedData().RankedBy(PlayRanking.DepthFirst);
        var unscored = ranked.Where(play => !play.IsScored).ToArray();

        Assert.Equal([0, 2], unscored.Select(play => play.Index).Order());
        Assert.All(unscored, play => Assert.NotEqual(ranked.Best.Candidate.DepthRank, play.Candidate.DepthRank));
    }

    [Fact]
    public void AnotherDepthsCandidateOfEqualEquity_IsScoredUnderDepthFirst_WithNoError()
    {
        // The rule's boundary: a higher equity, not an equal one.
        var play = MixedData().RankedBy(PlayRanking.DepthFirst).ForCandidate(5);

        Assert.NotEqual(MixedData().Plays[1].DepthRank, MixedData().Plays[5].DepthRank);
        Assert.True(play.IsScored);
        Assert.Equal(0.0, play.Error);
    }

    [Fact]
    public void ACandidateWhoseDepthIsNotRecorded_RanksBelowEveryRecordedOne_UnderDepthFirst()
    {
        // An unrecorded depth is a different depth from a recorded best's, so
        // its higher equity leaves it unscored; under equity it is the best.
        var data = TestRecords.CheckerPlayData(plays:
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], analysisLevel: AnalysisLevel.Ply2, equity: 0.0),
            TestRecords.Candidate(play: [new(13, 10), new(6, 5)],
                analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown, equity: 0.3),
            TestRecords.Candidate(play: [new(24, 23), new(13, 10)], analysisLevel: AnalysisLevel.Ply1, equity: -0.1),
        ]);
        Assert.Null(data.Plays[1].DepthRank);

        var depthFirst = data.RankedBy(PlayRanking.DepthFirst);
        Assert.Equal([0, 2, 1], depthFirst.Select(p => p.Index));
        Assert.False(depthFirst.ForCandidate(1).IsScored);
        Assert.Null(depthFirst.ForCandidate(1).Error);
        Assert.True(depthFirst.ForCandidate(2).IsScored);
        Assert.Equal(0.1, depthFirst.ForCandidate(2).Error!.Value, 12);

        var byEquity = data.RankedBy(PlayRanking.Equity);
        Assert.Equal([1, 0, 2], byEquity.Select(p => p.Index));
        Assert.All(byEquity, play => Assert.True(play.IsScored));
    }

    [Fact]
    public void WhereNoDepthIsRecorded_DepthFirstIsTheEquityOrder_AndScoresEveryCandidate()
    {
        var data = TestRecords.CheckerPlayData(plays: [.. new[] { 0.0, 0.3, -0.1 }.Select(equity =>
            TestRecords.Candidate(analysisMode: AnalysisMode.Unknown, analysisLevel: AnalysisLevel.Unknown, equity: equity))]);

        var depthFirst = data.RankedBy(PlayRanking.DepthFirst);
        Assert.Equal([1, 0, 2], depthFirst.Select(p => p.Index));
        Assert.All(depthFirst, play => Assert.True(play.IsScored));
    }

    // ── The player's error ────────────────────────────────────────

    [Theory]
    [InlineData(PlayRanking.Equity, 4, 0.32)]
    [InlineData(PlayRanking.DepthFirst, 4, 0.25)]
    [InlineData(PlayRanking.Equity, 0, 0.02)]
    [InlineData(PlayRanking.DepthFirst, 0, null)]
    [InlineData(PlayRanking.Equity, 2, 0.0)]
    [InlineData(PlayRanking.DepthFirst, 2, null)]
    [InlineData(PlayRanking.Equity, 5, 0.07)]
    [InlineData(PlayRanking.DepthFirst, 5, 0.0)]
    public void ThePlayersError_IsTheirCandidatesError_UnderTheRanking_NoneWhenItIsNotScored(
        PlayRanking ranking, int played, double? error)
    {
        var ranked = MixedData(userPlayIndex: played).RankedBy(ranking);

        Assert.Same(ranked.ForCandidate(played), ranked.UserPlay);
        if (error is double expected)
            Assert.Equal(expected, ranked.UserPlayError!.Value, 12);
        else
            Assert.Null(ranked.UserPlayError);
    }

    [Fact]
    public void AnUnlistedPlaysError_IsTheAnalysers_UnderEveryRanking_AndNoneRecordedIsNone()
    {
        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            var unlisted = MixedData(unlistedPlayError: 0.07).RankedBy(ranking);
            Assert.Null(unlisted.UserPlay);
            Assert.Equal(0.07, unlisted.UserPlayError);

            var none = MixedData().RankedBy(ranking);
            Assert.Null(none.UserPlay);
            Assert.Null(none.UserPlayError);
        }
    }

    // ── The filter view and the row, each built for one ranking ───

    [Theory]
    [InlineData(PlayRanking.Equity, 2, AnalysisMode.Evaluation, AnalysisLevel.Ply2, 0.32)]
    [InlineData(PlayRanking.DepthFirst, 1, AnalysisMode.Rollout, AnalysisLevel.Ply3, 0.25)]
    public void TheFilterView_SaysBestAndError_UnderItsRanking(
        PlayRanking ranking, int best, AnalysisMode mode, AnalysisLevel level, double error)
    {
        var record = MixedRecord(userPlayIndex: 4);
        var view = record.ViewFor(ranking);

        Assert.Equal(ranking, view.Ranking);
        Assert.Equal(error, view.FilterError!.Value, 12);
        Assert.Equal(mode, view.AnalysisMode);
        Assert.Equal(level, view.AnalysisLevel);
        Assert.Equal(record.AfterBoardOf(best), view.AfterBestBoard);
        Assert.Equal(record.AfterBestBoard(ranking), view.AfterBestBoard);
        Assert.Equal(record.AfterBoardOf(4), view.AfterPlayerBoard);
    }

    [Theory]
    [InlineData(PlayRanking.Equity, 0.32, "2-ply", 0.12)]
    [InlineData(PlayRanking.DepthFirst, 0.25, "Rollout: 1296 trials. 3-ply", 0.05)]
    public void TheRow_CarriesItsRankingsBestAndError(PlayRanking ranking, double error, string depth, double equity)
    {
        var record = MixedRecord(userPlayIndex: 4);
        var row = DecisionRow.From(record, ranking);

        Assert.Equal(ranking, row.Ranking);
        Assert.Equal(error, row.Error!.Value, 12);
        Assert.Equal(depth, row.AnalysisDepth);
        Assert.Equal(equity, row.Equity);
        Assert.Equal(record.AfterBestBoard(ranking), row.AfterBestBoard);
        Assert.EndsWith($",{ranking}", row.ToCsvLine());
    }

    [Fact]
    public void APlayTheRankingDoesNotScore_HasNoErrorInTheViewOrTheRow()
    {
        var record = MixedRecord(userPlayIndex: 0);

        Assert.Equal(0.02, record.ViewFor(PlayRanking.Equity).FilterError!.Value, 12);
        Assert.Null(record.ViewFor(PlayRanking.DepthFirst).FilterError);
        Assert.Null(DecisionRow.From(record, PlayRanking.DepthFirst).Error);
    }

    [Fact]
    public void ErredByMoreThanX_IsExpressibleUnderEitherRanking_OnViewsAndRowsAlike()
    {
        // The filter's question, asked of views built for each ranking: the
        // same records answer differently, as the rankings' errors differ.
        BgDecisionData[] records =
        [
            MixedRecord(userPlayIndex: 4),  // 0.32 by equity, 0.25 depth first
            MixedRecord(userPlayIndex: 0),  // 0.02 by equity, not scored depth first
            MixedRecord(userPlayIndex: 1),  // 0.07 by equity, 0 depth first
            TestRecords.Cube(decision: TestRecords.CubeData(
                userDoublerAction: null, userTakerAction: null, unstatedDoublerActionError: 0.3)),
        ];

        Assert.Equal([0, 3], ErredByMoreThan(records, PlayRanking.Equity, 0.28));
        Assert.Equal([3], ErredByMoreThan(records, PlayRanking.DepthFirst, 0.28));
        Assert.Equal([0, 2, 3], ErredByMoreThan(records, PlayRanking.Equity, 0.05));
        Assert.Equal([0, 3], ErredByMoreThan(records, PlayRanking.DepthFirst, 0.05));

        foreach (var ranking in Enum.GetValues<PlayRanking>())
        {
            int[] fromRows = [.. records.Index().Where(r => DecisionRow.From(r.Item, ranking).Error > 0.05).Select(r => r.Index)];
            Assert.Equal(ErredByMoreThan(records, ranking, 0.05), fromRows);
        }

        static int[] ErredByMoreThan(BgDecisionData[] records, PlayRanking ranking, double x) =>
            [.. records.Index().Where(r => r.Item.ViewFor(ranking).FilterError > x).Select(r => r.Index)];
    }

    [Fact]
    public void ACubeDecisionsViewAndRow_DoNotDependOnTheRanking()
    {
        var cube = TestRecords.Cube();
        var byEquity = cube.ViewFor(PlayRanking.Equity);
        var depthFirst = cube.ViewFor(PlayRanking.DepthFirst);

        foreach (var property in typeof(IDecisionFilterData).GetProperties().Where(p => p.Name != nameof(IDecisionFilterData.Ranking)))
            Assert.Equal(property.GetValue(byEquity), property.GetValue(depthFirst));

        string equityLine = DecisionRow.From(cube, PlayRanking.Equity).ToCsvLine();
        string depthFirstLine = DecisionRow.From(cube, PlayRanking.DepthFirst).ToCsvLine();
        Assert.Equal(equityLine[..equityLine.LastIndexOf(',')], depthFirstLine[..depthFirstLine.LastIndexOf(',')]);
    }

    // ── Built once per ranking ────────────────────────────────────

    [Fact]
    public void EachRanking_IsBuiltOnce_AndReadingItAgainAllocatesNothing()
    {
        var data = MixedData(userPlayIndex: 4);
        var byEquity = data.RankedBy(PlayRanking.Equity);
        var depthFirst = data.RankedBy(PlayRanking.DepthFirst);

        Assert.Same(byEquity, data.RankedBy(PlayRanking.Equity));
        Assert.Same(depthFirst, data.RankedBy(PlayRanking.DepthFirst));
        Assert.NotSame(byEquity, depthFirst);

        double sink = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            sink += data.RankedBy(PlayRanking.Equity).Best.Error!.Value + data.RankedBy(PlayRanking.DepthFirst).UserPlayError!.Value;
            sink += data.RankedBy(PlayRanking.DepthFirst)[3].Error!.Value + data.RankedBy(PlayRanking.Equity).ForCandidate(5).Error!.Value;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.True(sink > 0);
    }

    [Fact]
    public void RankingConcurrently_PublishesOneInstance()
    {
        var data = MixedData();
        var seen = new RankedPlays[32];

        Parallel.For(0, seen.Length, i => seen[i] = data.RankedBy(PlayRanking.DepthFirst));

        Assert.All(seen, ranked => Assert.Same(seen[0], ranked));
    }

    // ── What is refused ───────────────────────────────────────────

    [Fact]
    public void AnUndefinedRanking_IsRefused_AtEveryDoor()
    {
        var record = MixedRecord();
        var cube = TestRecords.Cube();
        var undefined = (PlayRanking)99;

        Assert.Equal("ranking", Assert.Throws<ArgumentOutOfRangeException>(() => record.Decision.RankedBy(undefined)).ParamName);
        Assert.Equal("ranking", Assert.Throws<ArgumentOutOfRangeException>(() => record.ViewFor(undefined)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => record.AfterBestBoard(undefined));
        Assert.Throws<ArgumentOutOfRangeException>(() => DecisionRow.From(record, undefined));
        Assert.Throws<ArgumentOutOfRangeException>(() => cube.ViewFor(undefined));
        Assert.Throws<ArgumentOutOfRangeException>(() => DecisionRow.From(cube, undefined));
    }

    [Fact]
    public void APositionOrAnIndexOutsideTheCandidates_IsRefused()
    {
        var ranked = MixedData().RankedBy(PlayRanking.Equity);

        Assert.Throws<ArgumentOutOfRangeException>(() => ranked[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => ranked[6]);
        Assert.Throws<ArgumentOutOfRangeException>(() => ranked.ForCandidate(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ranked.ForCandidate(6));
    }

    // ── No best or error without a ranking ────────────────────────

    [Fact]
    public void NoMember_AnswersBestOrError_WithoutARanking()
    {
        // Every public member whose name says best, error or loss, on the
        // types a checker play's derivations are read from, takes a ranking
        // or belongs to a type built for one, which states it — except the
        // one stored play error, an unlisted play's, which no ranking
        // changes. A ranked candidate is reachable only through its ranking.
        Assert.Empty(typeof(RankedPlay).GetConstructors());
        Assert.Empty(typeof(RankedPlays).GetConstructors());

        string[] words = ["Best", "Error", "Loss"];
        Type[] types =
        [
            typeof(BgDecisionData), typeof(CheckerPlayDecision), typeof(CheckerPlayDecisionData),
            typeof(PlayCandidate), typeof(IDecisionFilterData), typeof(DecisionRow), typeof(RankedPlays),
        ];
        foreach (var type in types)
        {
            bool builtForOne = type.GetProperty(nameof(IDecisionFilterData.Ranking))?.PropertyType == typeof(PlayRanking);
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (member is MethodInfo { IsSpecialName: true } || !words.Any(member.Name.Contains))
                    continue;
                bool takesARanking = member is MethodInfo method
                    && method.GetParameters().Any(p => p.ParameterType == typeof(PlayRanking));
                bool isTheStoredOne = member.Name == nameof(CheckerPlayDecisionData.UnlistedPlayError);
                Assert.True(takesARanking || builtForOne || isTheStoredOne,
                    $"{type.Name}.{member.Name} answers best or error without a ranking.");
            }
        }
    }

    // ── The enum ──────────────────────────────────────────────────

    [Fact]
    public void Equity_IsTheDefault_AndEachRankingHasItsLabel()
    {
        Assert.Equal(PlayRanking.Equity, default);
        Assert.Equal([PlayRanking.Equity, PlayRanking.DepthFirst], Enum.GetValues<PlayRanking>());
        Assert.Equal("Equity", Label(PlayRanking.Equity));
        Assert.Equal("Depth first", Label(PlayRanking.DepthFirst));

        static string? Label(PlayRanking ranking) =>
            typeof(PlayRanking).GetField(ranking.ToString())!.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    [Fact]
    public void TheRowsRanking_IsWrittenByName_RoundTrips_AndIsRequired_BothPaths()
    {
        var row = DecisionRow.From(MixedRecord(), PlayRanking.DepthFirst);

        foreach (var (_, options) in WirePaths.Both)
        {
            var json = JsonSerializer.Serialize(row, options);
            Assert.Contains("\"Ranking\":\"DepthFirst\"", json);
            Assert.Equal(PlayRanking.DepthFirst, JsonSerializer.Deserialize<DecisionRow>(json, options)!.Ranking);
        }

        var document = WirePaths.Document(row);
        document.Remove("Ranking");
        WirePaths.AssertRefused<DecisionRow>(document.ToJsonString());
        document["Ranking"] = 1;
        WirePaths.AssertRefused<DecisionRow>(document.ToJsonString());
    }
}
