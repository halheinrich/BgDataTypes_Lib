using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// A checker play's after-boards are derived, never stored (the arc's rule:
/// no stored copy of a derivable value): the boards its best and its user's
/// candidate leave from its position, through <see cref="BoardState"/>'s one
/// play rule, in the frame <see cref="BoardState.ApplyPlay"/> leaves. For the
/// derivation never to fail on a record that exists, every candidate is valid
/// from the position — a record invariant, refused at construction and, read
/// as a <see cref="BgDecisionData"/>, as a <see cref="JsonException"/> on both
/// paths. The invariant is stated on <see cref="BoardState.IsSamePlay"/>.
/// </summary>
public class AfterBoardDerivationTests
{
    /// <summary>The tester's position of halheinrich/backgammon#273, in the mover's frame.</summary>
    private static readonly BoardPosition TesterMop = new(
        [-1, 0, 2, -1, 2, 2, 4, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -2, 0]);

    private static readonly Play[] TesterPlays =
    [
        [new(8, -3), new(7, 3)],
        [new(11, 6), new(7, -3)],
        [new(6, 1), new(5, 1)],
        [new(21, 17), new(11, 6)],
    ];

    /// <summary>The board <see cref="BoardState.ApplyPlay"/> leaves, as a value.</summary>
    private static BoardPosition Applied(BoardPosition start, Play play)
    {
        var board = new BoardState(start);
        board.ApplyPlay(play);
        return board.ToPosition();
    }

    private static CheckerPlayDecision Tester(int bestPlayIndex, int? userPlayIndex) => TestRecords.CheckerPlay(
        position: TestRecords.Position(mop: TesterMop, onRollNeeds: 3, opponentNeeds: 5),
        decision: TestRecords.CheckerPlayData(
            dice: [5, 4],
            plays: [.. TesterPlays.Select(play => TestRecords.Candidate(play: play))],
            bestPlayIndex: bestPlayIndex,
            userPlayIndex: userPlayIndex));

    // ── The derivation ────────────────────────────────────────────

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 0)]
    [InlineData(3, 3)]
    [InlineData(1, null)]
    public void AfterBoards_AreTheBoardsTheBestAndUsersPlaysLeave(int best, int? user)
    {
        var record = Tester(best, user);

        Assert.Equal(Applied(TesterMop, TesterPlays[best]), record.AfterBestBoard);
        Assert.Equal(user is int u ? Applied(TesterMop, TesterPlays[u]) : null, record.AfterPlayerBoard);
    }

    [Fact]
    public void AfterBoards_AreInTheNextMoversFrame()
    {
        // The decision-maker's checkers are negative after the play: the
        // best play 8/3* 7/3 lands two on the 3-point, which is the next
        // mover's 22-point; and the blot it hits joins the one already on
        // the bar, which is the next mover's own bar, slot 25.
        var record = Tester(0, null);

        Assert.Equal(-2, record.AfterBestBoard[22]);
        Assert.Equal(2, record.AfterBestBoard[25]);
    }

    [Fact]
    public void AfterBoards_DoNotDependOnTheOrderPositionAndDecisionAreSet()
    {
        var decision = TestRecords.CheckerPlayData(
            dice: [5, 4], plays: [.. TesterPlays.Select(play => TestRecords.Candidate(play: play))],
            bestPlayIndex: 2, userPlayIndex: 1);
        var position = TestRecords.Position(mop: TesterMop, onRollNeeds: 3, opponentNeeds: 5);

        var positionFirst = new CheckerPlayDecision
        {
            Id = new XgDecisionId("m.xg", 1, 1, IsCube: false), Xgid = "",
            Position = position, Decision = decision, Descriptive = TestRecords.Descriptive(),
        };
        var decisionFirst = new CheckerPlayDecision
        {
            Id = new XgDecisionId("m.xg", 1, 1, IsCube: false), Xgid = "",
            Decision = decision, Position = position, Descriptive = TestRecords.Descriptive(),
        };

        Assert.Equal(positionFirst.AfterBestBoard, decisionFirst.AfterBestBoard);
        Assert.Equal(positionFirst.AfterPlayerBoard, decisionFirst.AfterPlayerBoard);
        Assert.Equal(Applied(TesterMop, TesterPlays[2]), decisionFirst.AfterBestBoard);
    }

    [Fact]
    public void AfterBoards_AreRederivedOnRead_BothPaths()
    {
        var record = Tester(2, 3);

        foreach (var (_, options) in WirePaths.Both)
        {
            var restored = Assert.IsType<CheckerPlayDecision>(JsonSerializer.Deserialize<BgDecisionData>(
                JsonSerializer.Serialize<BgDecisionData>(record, options), options));
            Assert.Equal(record.AfterBestBoard, restored.AfterBestBoard);
            Assert.Equal(record.AfterPlayerBoard, restored.AfterPlayerBoard);
        }
    }

    [Fact]
    public void ReadingTheAfterBoards_AllocatesNothing()
    {
        // Computed once while the record is built, not per read: the filter
        // reads them over many records.
        var record = Tester(0, 1);
        IDecisionFilterData view = record;
        int sink = record.AfterBestBoard[22] + view.AfterBestBoard!.Value[1];   // warm both doors

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            sink += record.AfterBestBoard[22] + record.AfterPlayerBoard!.Value[3];
            sink += view.AfterBestBoard!.Value[1];
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.NotEqual(int.MinValue, sink);
    }

    // ── Every candidate is valid from the position ────────────────

    public static TheoryData<int> CandidateIndices => [0, 1, 2, 3];

    [Theory]
    [MemberData(nameof(CandidateIndices))]
    public void AnInvalidCandidate_AnyOfThem_IsRefusedAtConstruction(int invalid)
    {
        // Not only the best and the user's: every candidate. The tester's
        // 7-point checker hitting on 3 unmarked is the fault
        // halheinrich/backgammon#273 was found by.
        Play[] plays = [.. TesterPlays];
        plays[invalid] = [new(8, 3), new(7, 3)];
        var decision = TestRecords.CheckerPlayData(
            dice: [5, 4], plays: [.. plays.Select(play => TestRecords.Candidate(play: play))],
            bestPlayIndex: invalid == 0 ? 1 : 0, userPlayIndex: null);

        var ex = Assert.Throws<ArgumentException>(() => TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: TesterMop), decision: decision));

        Assert.Equal("Decision", ex.ParamName);
        Assert.Contains($"Candidate {invalid}'s play is invalid from its position", ex.Message);
        Assert.Contains("opposing blot", ex.Message);
    }

    [Fact]
    public void AnInvalidCandidate_IsRefusedWhicheverOfPositionAndDecisionIsSetSecond()
    {
        var decision = TestRecords.CheckerPlayData(plays: [TestRecords.Candidate(play: [new(13, 12)])]);
        var position = TestRecords.Position(mop: TesterMop);

        var decisionSecond = Assert.Throws<ArgumentException>(() => new CheckerPlayDecision
        {
            Id = new XgDecisionId("m.xg", 1, 1, IsCube: false), Xgid = "",
            Position = position, Decision = decision, Descriptive = TestRecords.Descriptive(),
        });
        var positionSecond = Assert.Throws<ArgumentException>(() => new CheckerPlayDecision
        {
            Id = new XgDecisionId("m.xg", 1, 1, IsCube: false), Xgid = "",
            Decision = decision, Position = position, Descriptive = TestRecords.Descriptive(),
        });

        Assert.Equal("Decision", decisionSecond.ParamName);
        Assert.Equal("Position", positionSecond.ParamName);
    }

    [Fact]
    public void AnInvalidCandidate_IsRefusedOnTheWire_BothPaths()
    {
        // The document states a candidate that cannot be played from its own
        // position: a JsonException, the refusal inside it.
        var document = WirePaths.Document<BgDecisionData>(Tester(0, 1));
        document["Decision"]!["Plays"]![3]!["Play"] = JsonSerializer.SerializeToNode<Play>([new(13, 12)], WirePaths.Context);

        var ex = WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());

        Assert.IsType<ArgumentException>(ex.InnerException);
        Assert.Contains("Candidate 3's play is invalid", ex.Message);
    }

    [Fact]
    public void APass_IsValidFromAnyPosition()
    {
        // The empty play reaches the position itself, flipped.
        var record = TestRecords.CheckerPlay(
            position: TestRecords.Position(mop: TesterMop),
            decision: TestRecords.CheckerPlayData(plays: [TestRecords.Candidate(play: [])]));

        Assert.Equal(TesterMop.Flipped(), record.AfterBestBoard);
    }

    [Fact]
    public void TheRowTakesTheBoardsFromTheDerivation()
    {
        // The flat export projection never computes them a second way.
        var record = Tester(3, 0);
        var row = DecisionRow.From(record);

        Assert.Equal(record.AfterBestBoard, row.AfterBestBoard);
        Assert.Equal(record.AfterPlayerBoard, row.AfterPlayerBoard);
    }
}
