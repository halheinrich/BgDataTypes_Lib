using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Each side's borne-off count on <see cref="BoardPosition"/>: its value in
/// both frames for the standard start, a race, a terminal position, the empty
/// board and positions with checkers on the bars; that it reads the
/// invariant's number of checkers a side; that it never throws; and that it
/// allocates nothing.
/// </summary>
public class BorneOffCountTests
{
    // ── Fixtures ──────────────────────────────────────────────────

    /// <summary>
    /// Positions in the on-roll player's frame, with each side's borne-off
    /// count. The expected counts are written as numbers, so the table pins
    /// the number of checkers a side as well as the rule.
    /// </summary>
    public static TheoryData<string, int[], int, int> Positions => new()
    {
        { "the standard start",
            [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0], 0, 0 },
        // Every checker home on both sides: 11 of the on-roll player's, 8 of
        // the opponent's. The two counts differ, so a count reading the other
        // side cannot pass.
        { "a race, checkers off on both sides",
            [0, 1, 1, 2, 2, 3, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, -2, -2, -1, -1, -1, 0], 4, 7 },
        // The on-roll player has borne off all fifteen; 12 of the opponent's
        // are home. A position, by ruling, though no decision is made on it.
        { "a terminal position",
            [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -3, -3, -2, -2, -1, -1, 0], 15, 3 },
        { "the empty board", new int[26], 15, 15 },
        // Two of the on-roll player's on their bar (slot 25) beside 8 on the
        // points; one of the opponent's on theirs (slot 0) beside 11. A count
        // that skipped either bar would read that side's off count high.
        { "checkers on both bars",
            [-1, 0, 0, 0, 0, 0, 3, 0, 2, 0, 0, 0, -4, 3, 0, 0, 0, -3, 0, -4, 0, 0, 0, 0, 0, 2], 5, 3 },
        { "every checker on the bars",
            [-15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15], 0, 0 },
    };

    // ── The count, in both frames ─────────────────────────────────

    [Theory]
    [MemberData(nameof(Positions))]
    public void Counts_InThePositionsFrame(string name, int[] counts, int onRollOff, int opponentOff)
    {
        var position = new BoardPosition(counts);

        Assert.True(onRollOff == position.OnRollBorneOffCount, $"{name}: on roll, {position.OnRollBorneOffCount}");
        Assert.True(opponentOff == position.OpponentBorneOffCount, $"{name}: opponent, {position.OpponentBorneOffCount}");
    }

    [Theory]
    [MemberData(nameof(Positions))]
    public void Counts_InTheFlippedFrame_Swap(string name, int[] counts, int onRollOff, int opponentOff)
    {
        // The flipped view is the other player's frame: its on-roll player
        // is this position's opponent.
        var flipped = new BoardPosition(counts).Flipped();

        Assert.True(opponentOff == flipped.OnRollBorneOffCount, $"{name}: on roll, {flipped.OnRollBorneOffCount}");
        Assert.True(onRollOff == flipped.OpponentBorneOffCount, $"{name}: opponent, {flipped.OpponentBorneOffCount}");
    }

    [Fact]
    public void EmptyAndDefault_EveryCheckerIsOff()
    {
        // default is built by no constructor; the count answers for it too.
        Assert.Equal(15, default(BoardPosition).OnRollBorneOffCount);
        Assert.Equal(15, default(BoardPosition).OpponentBorneOffCount);
        Assert.Equal(15, BoardPosition.Empty.OnRollBorneOffCount);
        Assert.Equal(15, BoardPosition.Empty.OpponentBorneOffCount);
    }

    // ── One number of checkers a side ─────────────────────────────

    /// <summary>
    /// A side's slot for its bar and a point of its own, in the on-roll
    /// frame, with the sign of its counts.
    /// </summary>
    public static TheoryData<string, int, int, int> Sides => new()
    {
        { "on roll", 25, 6, 1 },
        { "opponent", 0, 19, -1 },
    };

    [Theory]
    [MemberData(nameof(Sides))]
    public void AtTheInvariantsBound_NoneIsOff_AndOneMoreCheckerIsNoPosition(
        string side, int bar, int point, int sign)
    {
        // The count and the invariant read one number: a side holding as many
        // checkers as the invariant admits, its bar included, has none off,
        // and one more is refused by the side's bound (not a slot's).
        var counts = new int[26];
        counts[bar] = sign;
        counts[point] = sign * (BoardPosition.CheckersPerSide - 1);
        var full = new BoardPosition(counts);

        int off = sign > 0 ? full.OnRollBorneOffCount : full.OpponentBorneOffCount;
        Assert.True(off == 0, $"{side}: {off} off");

        counts[point] += sign;
        var refusal = Assert.Throws<ArgumentException>("counts", () => new BoardPosition(counts));
        Assert.Contains(
            $"has {BoardPosition.CheckersPerSide + 1} checkers on the board; a side has at most {BoardPosition.CheckersPerSide}.",
            refusal.Message);
    }

    // ── Allocation ────────────────────────────────────────────────

    [Fact]
    public void Counts_AllocateNothing()
    {
        // Measured through AllocationProbe, which warms the path and is
        // immune to a one-off allocation that is not the path's.
        var position = new BoardPosition(
            [-1, 0, 0, 0, 0, 0, 3, 0, 2, 0, 0, 0, -4, 3, 0, 0, 0, -3, 0, -4, 0, 0, 0, 0, 0, 2]);
        int sink = 0;

        long allocated = AllocationProbe.SteadyStateBytes(
            () => sink += position.OnRollBorneOffCount + position.OpponentBorneOffCount);

        Assert.Equal(0, allocated);
        Assert.NotEqual(int.MinValue, sink);   // keep the work observable
    }
}
