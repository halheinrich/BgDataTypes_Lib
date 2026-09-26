using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The position value: its invariant, its equality and hash contract, its
/// read surface and text form, and that it allocates nothing to create,
/// compare or hash.
/// </summary>
public class BoardPositionTests
{
    // ── Fixtures ──────────────────────────────────────────────────

    /// <summary>The standard start, in the on-roll player's frame.</summary>
    private static readonly int[] StandardCounts =
        [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0];

    /// <summary>
    /// A position with a checker on each bar, so no slot is special-cased by
    /// being zero in every fixture, and 14 checkers a side, so any slot can
    /// take one more checker and stay well-formed.
    /// </summary>
    private static readonly int[] BothBarsCounts =
        [-1, 0, 2, -1, 2, 2, 2, 1, 2, -1, 0, 1, -2, 0, 0, 0, -2, 0, 0, -2, -2, 1, -2, 0, -1, 1];

    /// <summary>
    /// The empty board with one checker on <paramref name="slot"/>: the
    /// opponent's on the opponent's bar, the on-roll player's elsewhere, so
    /// every variant is well-formed.
    /// </summary>
    private static int[] OneChecker(int slot)
    {
        var counts = new int[26];
        counts[slot] = slot == 0 ? -1 : 1;
        return counts;
    }

    // ── Construction and the invariant ────────────────────────────

    [Fact]
    public void Constructor_WellFormedCounts_ReadBackUnchanged()
    {
        var position = new BoardPosition(BothBarsCounts);

        for (int i = 0; i < 26; i++)
            Assert.Equal(BothBarsCounts[i], position[i]);
    }

    [Fact]
    public void Constructor_CopiesTheCounts_LaterWritesToTheSourceDoNotReachIt()
    {
        int[] counts = [.. StandardCounts];
        var position = new BoardPosition(counts);

        counts[6] = 0;

        Assert.Equal(5, position[6]);
    }

    [Fact]
    public void Default_IsTheEmptyBoard()
    {
        Assert.Equal(BoardPosition.Empty, default);
        Assert.Equal(new BoardPosition(new int[26]), BoardPosition.Empty);
        for (int i = 0; i < 26; i++)
            Assert.Equal(0, BoardPosition.Empty[i]);
    }

    public static TheoryData<string, int[]> WellFormedEdges => new()
    {
        { "fifteen a side", [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0] },
        { "every checker on the bars", [-15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15] },
        { "fifteen on one point each", [0, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -15, 0] },
        { "one side only", [0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
        { "the empty board", new int[26] },
    };

    [Theory]
    [MemberData(nameof(WellFormedEdges))]
    public void WellFormedEdges_AreAccepted(string name, int[] counts)
    {
        Assert.True(BoardPosition.TryCreate(counts, out var position), name);
        Assert.Equal(new BoardPosition(counts), position);
    }

    public static TheoryData<string, int[]> Malformed => new()
    {
        { "no counts", [] },
        { "25 counts", new int[25] },
        { "27 counts", new int[27] },
        { "an on-roll checker on the opponent's bar", [1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
        { "an opponent's checker on the on-roll bar", [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1] },
        { "sixteen on-roll checkers", [0, 0, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0] },
        { "sixteen opponent's checkers", [-1, -2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -5, 0, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 0, 0] },
        { "sixteen on one point", [0, 0, 0, 0, 0, 0, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
        { "a count that would wrap when stored", [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 256, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
        // Magnitudes that would overflow a running side total were the
        // counts summed before each was bounded.
        { "int.MaxValue beside itself", [0, int.MaxValue, int.MaxValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
        { "int.MinValue", [int.MinValue, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0] },
    };

    [Theory]
    [MemberData(nameof(Malformed))]
    public void Malformed_ConstructorRefuses(string name, int[] counts)
    {
        var ex = Assert.Throws<ArgumentException>("counts", () => new BoardPosition(counts));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message), name);
    }

    [Theory]
    [MemberData(nameof(Malformed))]
    public void Malformed_TryCreateRefuses_AndYieldsTheEmptyBoard(string name, int[] counts)
    {
        Assert.False(BoardPosition.TryCreate(counts, out var position), name);
        Assert.Equal(BoardPosition.Empty, position);
    }

    // ── Equality and hash ─────────────────────────────────────────

    [Fact]
    public void Equality_SameCounts_EqualFromEveryDoor()
    {
        var a = new BoardPosition(StandardCounts);
        var b = new BoardPosition([.. StandardCounts]);
        Assert.True(BoardPosition.TryCreate(StandardCounts, out var c));

        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals((object)b));
        Assert.True(a.Equals(c) && c.Equals(a));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.Equal(a.GetHashCode(), c.GetHashCode());
    }

    [Fact]
    public void Equality_IsReflexiveAndSymmetric()
    {
        var a = new BoardPosition(BothBarsCounts);
        var b = new BoardPosition(StandardCounts);

        Assert.True(a.Equals(a));
        Assert.Equal(a.Equals(b), b.Equals(a));
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equality_EverySlotIsSignificant_AgainstABusyBoard()
    {
        // A one-checker change at each slot of a board holding checkers on
        // both bars: a checker leaves an occupied slot, or an on-roll
        // checker joins an empty one. Each change stays well-formed and is a
        // different position.
        var original = new BoardPosition(BothBarsCounts);
        for (int slot = 0; slot < 26; slot++)
        {
            int[] counts = [.. BothBarsCounts];
            counts[slot] += counts[slot] > 0 ? -1 : 1;
            var changed = new BoardPosition(counts);

            Assert.False(original.Equals(changed), $"slot {slot}");
            Assert.True(original != changed, $"slot {slot}");
            Assert.False(original == changed, $"slot {slot}");
        }
    }

    [Fact]
    public void Equality_EverySlotIsSignificant_BothBarsIncluded()
    {
        // The empty board and 26 one-checker boards: 27 positions, each
        // equal only to itself.
        var positions = new List<BoardPosition> { BoardPosition.Empty };
        for (int slot = 0; slot < 26; slot++)
            positions.Add(new BoardPosition(OneChecker(slot)));

        for (int i = 0; i < positions.Count; i++)
            for (int j = 0; j < positions.Count; j++)
                Assert.True((i == j) == (positions[i] == positions[j]),
                    $"positions {i} and {j}: {positions[i]} vs {positions[j]}");
    }

    [Fact]
    public void Hash_ReadsEverySlot()
    {
        // Not the contract (equality decides), but a hash that skipped a
        // slot would collide every board differing only there. With a
        // 32-bit hash, 27 distinct positions collide by chance with
        // probability under one in ten million.
        var hashes = new HashSet<int> { BoardPosition.Empty.GetHashCode() };
        for (int slot = 0; slot < 26; slot++)
            hashes.Add(new BoardPosition(OneChecker(slot)).GetHashCode());

        Assert.Equal(27, hashes.Count);
    }

    [Fact]
    public void Equals_Object_OtherTypesAndNull_AreNotEqual()
    {
        var position = new BoardPosition(StandardCounts);

        Assert.False(position.Equals(null));
        Assert.False(position.Equals(StandardCounts));
        Assert.False(position.Equals((object)BoardPosition.Empty));
    }

    [Fact]
    public void HashedCollections_DeduplicateByPosition()
    {
        var set = new HashSet<BoardPosition>
        {
            new(StandardCounts),
            new([.. StandardCounts]),
            new(BothBarsCounts),
        };

        Assert.Equal(2, set.Count);
        Assert.Contains(new BoardPosition(StandardCounts), set);
    }

    // ── Read surface ──────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(26)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Indexer_OutsideTheBoard_Throws(int point)
    {
        var position = new BoardPosition(StandardCounts);

        Assert.Throws<ArgumentOutOfRangeException>("point", () => position[point]);
    }

    [Fact]
    public void CopyTo_WritesAllTwentySixCounts()
    {
        var position = new BoardPosition(BothBarsCounts);
        Span<int> destination = stackalloc int[28];
        destination.Fill(99);

        position.CopyTo(destination);

        Assert.True(destination[..26].SequenceEqual(BothBarsCounts));
        Assert.Equal(99, destination[26]);
    }

    [Fact]
    public void CopyTo_ShortDestination_Throws()
    {
        var position = new BoardPosition(StandardCounts);

        Assert.Throws<ArgumentException>("destination", () => position.CopyTo(new int[25]));
    }

    // ── The flip ──────────────────────────────────────────────────

    public static TheoryData<string, int[]> FlipCases => new()
    {
        { "standard", StandardCounts },
        { "both bars", BothBarsCounts },
        { "one side only, on its bar", [0, 0, 0, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2] },
        { "every checker on the bars", [-15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 15] },
        { "the empty board", new int[26] },
    };

    [Theory]
    [MemberData(nameof(FlipCases))]
    public void Flipped_MirrorsThePoints_SwapsTheBars_InvertsTheSigns(string name, int[] counts)
    {
        var flipped = new BoardPosition(counts).Flipped();

        for (int i = 0; i < 26; i++)
            Assert.True(-counts[25 - i] == flipped[i], $"{name}: slot {i}");
        Assert.Equal(-counts[25], flipped[0]);
        Assert.Equal(-counts[0], flipped[25]);
    }

    [Theory]
    [MemberData(nameof(FlipCases))]
    public void Flipped_IsAnInvolution(string name, int[] counts)
    {
        var position = new BoardPosition(counts);

        Assert.True(position == position.Flipped().Flipped(), name);
    }

    [Theory]
    [MemberData(nameof(FlipCases))]
    public void Flipped_IsWellFormed(string name, int[] counts)
    {
        // The flip builds its result without the outside-data check; the
        // same counts pass that check.
        Span<int> flipped = stackalloc int[26];
        new BoardPosition(counts).Flipped().CopyTo(flipped);

        Assert.True(BoardPosition.TryCreate(flipped, out _), name);
    }

    [Fact]
    public void Flipped_AnAsymmetricPosition_IsADifferentPosition()
    {
        // Guards against a flip that returns its input: the standard start
        // and the empty board are flip-symmetric, so they cannot tell.
        var position = new BoardPosition(BothBarsCounts);

        Assert.NotEqual(position, position.Flipped());
        Assert.Equal(BoardPosition.Standard, BoardPosition.Standard.Flipped());
    }

    [Fact]
    public void Flipped_AllocatesNothing()
    {
        var position = new BoardPosition(BothBarsCounts);
        int sink = position.Flipped()[0];   // warm

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            var flipped = position.Flipped();
            sink += flipped[0] + flipped[25];
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.NotEqual(int.MinValue, sink);
    }

    // ── Text form ─────────────────────────────────────────────────

    [Fact]
    public void ToString_NamesEachOccupiedSlot()
    {
        Assert.Equal("1:-2 6:5 8:3 12:-5 13:5 17:-3 19:-5 24:2", new BoardPosition(StandardCounts).ToString());
        Assert.Equal("0:-1 25:1", new BoardPosition([-1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1]).ToString());
    }

    [Fact]
    public void ToString_EmptyBoard_SaysSo()
    {
        Assert.Equal("empty", BoardPosition.Empty.ToString());
    }

    // ── Allocation ────────────────────────────────────────────────

    [Fact]
    public void CreateCompareAndHash_AllocateNothing()
    {
        ReadOnlySpan<int> counts = StandardCounts;
        ReadOnlySpan<int> other = BothBarsCounts;

        // Warm every path first, so JIT and first-call work fall outside
        // the measured window.
        int sink = Exercise(counts, other);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
            sink += Exercise(counts, other);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
        Assert.NotEqual(int.MinValue, sink);   // keep the work observable
    }

    private static int Exercise(ReadOnlySpan<int> counts, ReadOnlySpan<int> other)
    {
        var a = new BoardPosition(counts);
        BoardPosition.TryCreate(other, out var b);
        int result = a.GetHashCode() ^ b.GetHashCode();
        if (a == b) result++;
        if (a != b) result--;
        if (a.Equals(b)) result += 2;
        return result + a[6] + b[25];
    }
}
