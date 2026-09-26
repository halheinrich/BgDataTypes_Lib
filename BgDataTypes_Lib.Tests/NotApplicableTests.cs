using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// "Not applicable" is never a value (halheinrich/backgammon#273, the records
/// leg; halheinrich/backgammon#124). Each stand-in the records used to carry —
/// a -1, an empty list, a zeroed or empty cube half, <c>Unknown</c> modes on
/// a checker play, [0, 0] dice, a stamped game and move 1, a 0 roll, a 0
/// error — is pinned here as no longer expressible, and the real "none" that
/// replaced it, where one did, as round-tripping on both paths.
/// </summary>
public class NotApplicableTests
{
    private static bool HasMember(Type type, string name) =>
        type.GetMember(name, BindingFlags.Public | BindingFlags.Instance).Length > 0;

    // ── UserPlayIndex: -1 → null ──────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(3)]
    public void UserPlayIndex_OutsideTheCandidates_CannotBeExpressed(int index)
    {
        // -1 was "not applicable"; it is refused like any index that
        // identifies no candidate (the default decision has three).
        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => TestRecords.CheckerPlayData(userPlayIndex: index));

        Assert.Equal("UserPlayIndex", ex.ParamName);
    }

    [Fact]
    public void UserPlayIndex_OutsideTheCandidates_IsRefusedOnTheWire_BothPaths()
    {
        var document = WirePaths.Document<BgDecisionData>(TestRecords.CheckerPlay());
        document["Decision"]!["UserPlayIndex"] = -1;

        var ex = WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    [Fact]
    public void UserPlayIndex_None_RoundTrips_BothPaths()
    {
        var play = TestRecords.CheckerPlay(decision: TestRecords.CheckerPlayData(userPlayIndex: null));

        foreach (var (_, options) in WirePaths.Both)
        {
            var json = JsonSerializer.Serialize<BgDecisionData>(play, options);
            var restored = Assert.IsType<CheckerPlayDecision>(JsonSerializer.Deserialize<BgDecisionData>(json, options));

            Assert.Contains("\"UserPlayIndex\":null", json);
            Assert.Null(restored.Decision.UserPlayIndex);
            Assert.Null(restored.Decision.UserPlay);
            Assert.Null(restored.AfterPlayerBoard);
        }
    }

    [Fact]
    public void UserPlayIndex_IsCheckedWhicheverOfItAndPlaysIsSetSecond()
    {
        // Order-independent: the index set before the list and after it.
        // Rewritten: the third case held a stated best index to the list; the
        // best is derived now, and the rule it stood beside is the user's
        // play against an unlisted play's error — the two say different
        // things, so neither may be stated with the other, in either order.
        Assert.Throws<ArgumentException>(() => new CheckerPlayDecisionData
        {
            Dice = [3, 1],
            UserPlayIndex = 1,
            Plays = [TestRecords.Candidate()],
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => new CheckerPlayDecisionData
        {
            Dice = [3, 1],
            Plays = [TestRecords.Candidate()],
            UserPlayIndex = 1,
        });
        var indexFirst = Assert.Throws<ArgumentException>(() => new CheckerPlayDecisionData
        {
            Dice = [3, 1],
            Plays = [TestRecords.Candidate()],
            UserPlayIndex = 0,
            UnlistedPlayError = 0.1,
        });
        var errorFirst = Assert.Throws<ArgumentException>(() => new CheckerPlayDecisionData
        {
            Dice = [3, 1],
            Plays = [TestRecords.Candidate()],
            UnlistedPlayError = 0.1,
            UserPlayIndex = 0,
        });
        Assert.Equal("UnlistedPlayError", indexFirst.ParamName);
        Assert.Equal("UserPlayIndex", errorFirst.ParamName);
    }

    // ── A checker play's cube half, and a cube's checker half ─────

    [Theory]
    [InlineData("CubeDepth")]
    [InlineData("Depth")]
    [InlineData("CubeAnalysisMode")]
    [InlineData("NoDoubleEquity")]
    [InlineData("WinPctAfterNoDouble")]
    [InlineData("UserDoubleError")]
    [InlineData("UserDoublerAction")]
    [InlineData("IsCube")]
    public void ACheckerPlay_HasNoCubeMember(string member)
    {
        // Empty depth strings, zeroed equities and Unknown modes stood for a
        // cube half that did not apply; a checker play has no such member.
        Assert.False(HasMember(typeof(CheckerPlayDecisionData), member));
        Assert.False(HasMember(typeof(CheckerPlayDecision), member));
    }

    [Theory]
    [InlineData("Dice")]
    [InlineData("Plays")]
    [InlineData("BestPlayIndex")]
    [InlineData("UserPlayIndex")]
    [InlineData("UserPlayError")]
    [InlineData("AfterBestBoard")]
    [InlineData("AfterPlayerBoard")]
    [InlineData("Outcome")]
    [InlineData("IsCube")]
    public void ACubeDecision_HasNoCheckerPlayMember(string member)
    {
        // [0, 0] dice, an empty candidate list, index -1 and null after-boards
        // stood for a checker half that did not apply; a cube decision has no
        // such member.
        Assert.False(HasMember(typeof(CubeDecisionData), member));
        Assert.False(HasMember(typeof(CubeDecision), member));
    }

    [Fact]
    public void ACheckerPlay_HasAtLeastOneCandidate_AndItsBestIsOne()
    {
        // The empty list was a cube's; and a best index that identifies no
        // candidate was the case the Unknown-mode fallback covered.
        // Rewritten: the best is derived — the first candidate of the highest
        // equity — so it is one of the candidates by construction, and the
        // two out-of-range indices it pinned cannot be stated.
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlayData(plays: []));
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlayData(plays: [TestRecords.Candidate(), null!]));

        var tied = TestRecords.CheckerPlayData(plays:
        [
            TestRecords.Candidate(play: [new(8, 5), new(6, 5)], equity: 0.1),
            TestRecords.Candidate(play: [new(13, 10), new(6, 5)], equity: 0.3),
            TestRecords.Candidate(play: [new(24, 23), new(13, 10)], equity: 0.3),
        ]);
        Assert.Equal(1, tied.BestPlayIndex);
        Assert.Same(tied.Plays[1], tied.BestPlay);
        Assert.Equal(0.0, tied.EquityLoss(2));
    }

    [Fact]
    public void ACandidatesEquity_IsANumber()
    {
        // Added: the best is the highest equity's, which a NaN or an infinity
        // would leave undefined, so a candidate's equity is finite.
        foreach (double equity in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.Candidate(equity: equity));
            Assert.Equal("Equity", ex.ParamName);
        }
    }

    [Theory]
    [InlineData(new[] { 0, 0 })]
    [InlineData(new[] { 3, 0 })]
    [InlineData(new[] { 7, 2 })]
    public void ACheckerPlaysDice_AreTwoFaces(int[] dice)
    {
        // [0, 0] was a cube's dice; on a checker play it is refused.
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.CheckerPlayData(dice: dice));
    }

    [Fact]
    public void ACheckerPlaysCollections_AreCopiedOnInit()
    {
        // A guard on a list the caller can still change would be no guard.
        var dice = new[] { 3, 1 };
        var plays = new List<PlayCandidate> { TestRecords.Candidate() };
        var data = TestRecords.CheckerPlayData(dice: dice, plays: plays);

        dice[0] = 0;
        plays.Clear();

        Assert.Equal(new[] { 3, 1 }, data.Dice);
        Assert.Single(data.Plays);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)data.Dice)[0] = 0);
    }

    // ── A standalone position's game facts (halheinrich/backgammon#124) ──

    [Fact]
    public void AStandalonePositionsGameAndMove_CannotBeStated()
    {
        // The stamped 1 needed a place to be written; there is none: the
        // numbers are derived from the identifier, on the record and the row.
        foreach (var type in new[] { typeof(BgDecisionData), typeof(DescriptiveData), typeof(DecisionRow) })
            foreach (var name in new[] { "Game", "MoveNumber" })
            {
                var property = type.GetProperty(name);
                Assert.True(property is null || property.SetMethod is null, $"{type.Name}.{name} can be set");
            }
    }

    [Fact]
    public void AStandalonePositionsStart_CannotBeStated_AndNoneRoundTrips()
    {
        // A standalone position belongs to no game, so no start: stating one
        // is refused, from either side, and none round-trips.
        Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlay(
            id: new XgpDecisionId("p.xgp"), descriptive: TestRecords.Descriptive(isStandardStart: false)));
        Assert.Throws<ArgumentException>(() => TestRecords.Cube(
            id: new XgDecisionId("m.xg", 1, 2, IsCube: true), descriptive: TestRecords.Descriptive(isStandardStart: null)));

        var standalone = TestRecords.CheckerPlay(id: new XgpDecisionId("p.xgp"));
        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = JsonSerializer.Deserialize<BgDecisionData>(
                JsonSerializer.Serialize<BgDecisionData>(standalone, options), options)!;
            Assert.Null(restored.IsStandardStart);
            Assert.Null(restored.Game);
            Assert.Null(restored.MoveNumber);
        }
    }

    [Fact]
    public void AStandalonePositionsStart_IsCheckedWhicheverOfIdAndDescriptiveIsSetSecond()
    {
        var descriptiveFirst = Assert.Throws<ArgumentException>(() => new CheckerPlayDecision
        {
            Descriptive = TestRecords.Descriptive(isStandardStart: true),
            Id = new XgpDecisionId("p.xgp"),
            Xgid = "",
            Position = TestRecords.Position(),
            Decision = TestRecords.CheckerPlayData(),
        });
        var idFirst = Assert.Throws<ArgumentException>(() => new CheckerPlayDecision
        {
            Id = new XgpDecisionId("p.xgp"),
            Descriptive = TestRecords.Descriptive(isStandardStart: true),
            Xgid = "",
            Position = TestRecords.Position(),
            Decision = TestRecords.CheckerPlayData(),
        });

        Assert.Equal("Id", descriptiveFirst.ParamName);
        Assert.Equal("Descriptive", idFirst.ParamName);
    }

    // ── The row: a cube's roll and a missing error ────────────────

    [Fact]
    public void ACubeRowsRoll_IsNone_NeverZero()
    {
        var row = DecisionRow.From(TestRecords.Cube());

        Assert.Null(row.Roll);
        foreach (var (_, options) in WirePaths.Both)
        {
            var json = JsonSerializer.Serialize(row, options);
            Assert.Contains("\"Roll\":null", json);
            Assert.Null(JsonSerializer.Deserialize<DecisionRow>(json, options)!.Roll);
        }

        // And a 0 roll is refused on read, of either kind.
        foreach (var record in new BgDecisionData[] { TestRecords.Cube(), TestRecords.CheckerPlay() })
        {
            var document = WirePaths.Document(DecisionRow.From(record));
            document["Roll"] = 0;
            WirePaths.AssertRefused<DecisionRow>(document.ToJsonString());
        }
    }

    [Fact]
    public void ARowsError_NoneRecorded_IsNone_NeverZero()
    {
        // The converter wrote 0 when no user error was recorded; the row's
        // error is the record's, null when none is.
        var row = DecisionRow.From(TestRecords.CheckerPlay(
            decision: TestRecords.CheckerPlayData(userPlayIndex: null)));

        Assert.Null(row.Error);
        foreach (var (_, options) in WirePaths.Both)
            Assert.Null(JsonSerializer.Deserialize<DecisionRow>(JsonSerializer.Serialize(row, options), options)!.Error);
    }
}
