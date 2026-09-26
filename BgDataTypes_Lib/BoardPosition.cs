using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// An immutable backgammon position: the checker count on each of a board's
/// 26 slots, in <see cref="BoardState"/>'s frame. Index 0 is the opponent's
/// bar, 1–24 are the points, and 25 is the bar of the player the frame
/// belongs to — the player on roll, who moves from the 25 toward the 1.
/// Positive counts are that player's checkers, negative counts the
/// opponent's. This type is the one definition of "the same position".
/// </summary>
/// <remarks>
/// <para>
/// <b>Well-formed, always.</b> Every position satisfies two conditions:
/// </para>
/// <list type="bullet">
/// <item><description>each bar holds only its own side's checkers — the
/// opponent's bar (index 0) a count of 0 or less, the on-roll player's bar
/// (index 25) a count of 0 or more;</description></item>
/// <item><description>each side has at most 15 checkers on the board — the
/// positive counts sum to at most 15, and so do the magnitudes of the
/// negative counts.</description></item>
/// </list>
/// <para>
/// Fewer than 15 is well-formed, since borne-off checkers are not tracked, so
/// the empty board is a position: every count 0, which is
/// <see cref="Empty"/> and <see langword="default"/>. The invariant is
/// enforced wherever a position is built from outside data — the
/// constructor and <see cref="TryCreate(ReadOnlySpan{int}, out BoardPosition)"/>
/// refuse counts that break it —
/// so no instance can hold a malformed board.
/// </para>
/// <para>
/// <b>Same position.</b> Two positions are equal exactly when all 26 counts
/// are equal, both bars included. <see cref="GetHashCode"/> is consistent
/// with that equality and is never identity: equal positions hash equally,
/// but equal hashes prove nothing, and equality decides. The hash is seeded
/// per process, so it is never stored or compared across processes.
/// </para>
/// <para>
/// <b>Allocation-free.</b> Creating, comparing and hashing a position
/// allocate nothing: the move generator deduplicates plays by the position
/// they reach, on its hot path. The counts are stored inline and narrowed,
/// which the invariant makes lossless (no count exceeds 15 in magnitude);
/// the storage is private and reads widen back to <see langword="int"/>.
/// </para>
/// <para>
/// <b>The frame is the holder's to state.</b> A position does not record
/// whose turn it describes; every member that stores one states its frame
/// in its own documentation. <see cref="Flipped"/> re-expresses a position
/// from the other player's frame.
/// </para>
/// <para>
/// <b>On the wire</b> a position is a JSON array of its 26 counts in slot
/// order, through the bundled <see cref="BoardPositionJsonConverter"/>; a
/// read that does not form a position is a <c>JsonException</c>.
/// </para>
/// </remarks>
[JsonConverter(typeof(BoardPositionJsonConverter))]
public readonly struct BoardPosition :
    IEquatable<BoardPosition>,
    IEqualityOperators<BoardPosition, BoardPosition, bool>
{
    /// <summary>The number of slots: two bars and 24 points.</summary>
    internal const int SlotCount = 26;

    /// <summary>The checkers each side owns; at most this many are on the board.</summary>
    private const int CheckersPerSide = 15;

    private readonly Counts _counts;

    /// <summary>
    /// Creates a position from its 26 counts, in the layout of the type
    /// summary.
    /// </summary>
    /// <param name="counts">Exactly 26 counts forming a well-formed board (see the type remarks).</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="counts"/> does not hold exactly 26 counts, or the
    /// board they describe is not well-formed; the message names the fault.
    /// </exception>
    public BoardPosition(ReadOnlySpan<int> counts)
    {
        if (FindFault(counts) is { } fault)
            throw new ArgumentException(fault, nameof(counts));
        _counts = Narrow(counts);
    }

    private BoardPosition(Counts counts) => _counts = counts;

    /// <summary>
    /// The empty board: every count 0. Equal to <see langword="default"/>, and
    /// well-formed — no checkers on the board is a position.
    /// </summary>
    public static BoardPosition Empty => default;

    /// <summary>
    /// Creates a position from its 26 counts without throwing: the
    /// non-throwing form of <see cref="BoardPosition(ReadOnlySpan{int})"/>.
    /// </summary>
    /// <param name="counts">The candidate counts, in the layout of the type summary.</param>
    /// <param name="position">
    /// The position when <paramref name="counts"/> forms one; otherwise
    /// <see cref="Empty"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="counts"/> holds exactly 26
    /// counts forming a well-formed board; otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryCreate(ReadOnlySpan<int> counts, out BoardPosition position)
        => TryCreate(counts, out position, out _);

    /// <summary>
    /// <see cref="TryCreate(ReadOnlySpan{int}, out BoardPosition)"/>, also
    /// naming the fault, for a caller that reports it in its own exception.
    /// </summary>
    internal static bool TryCreate(
        ReadOnlySpan<int> counts, out BoardPosition position, [NotNullWhen(false)] out string? fault)
    {
        fault = FindFault(counts);
        position = fault is null ? new BoardPosition(Narrow(counts)) : default;
        return fault is null;
    }

    /// <summary>
    /// A position from counts the caller guarantees form one: a
    /// <see cref="BoardState"/>'s own board, or a board the play rule reached
    /// from it, both well-formed by <see cref="BoardState"/>'s invariant. This
    /// is the snapshot on the move generator's hot path, so the invariant is
    /// asserted in Debug builds only; outside data goes through the
    /// constructor or <see cref="TryCreate(ReadOnlySpan{int}, out BoardPosition)"/>.
    /// </summary>
    internal static BoardPosition FromWellFormed(ReadOnlySpan<int> counts)
    {
        Debug.Assert(FindFault(counts) is null,
            "FromWellFormed: the counts do not form a well-formed position.");
        return new BoardPosition(Narrow(counts));
    }

    // ── Starting positions ────────────────────────────────────────

    /// <summary>
    /// The standard starting position, in the on-roll player's frame.
    /// On-roll: 6-pt(5), 8-pt(3), 13-pt(5), 24-pt(2).
    /// Opponent: 19-pt(-5), 17-pt(-3), 12-pt(-5), 1-pt(-2).
    /// </summary>
    public static BoardPosition Standard { get; } = new(
        [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0]);

    /// <summary>
    /// The Nackgammon starting position, in the on-roll player's frame.
    /// On-roll: 6-pt(4), 8-pt(3), 13-pt(4), 23-pt(2), 24-pt(2).
    /// Opponent: 19-pt(-4), 17-pt(-3), 12-pt(-4), 2-pt(-2), 1-pt(-2).
    /// </summary>
    public static BoardPosition Nackgammon { get; } = new(
        [0, -2, -2, 0, 0, 0, 4, 0, 3, 0, 0, 0, -4, 4, 0, 0, 0, -3, 0, -4, 0, 0, 0, 2, 2, 0]);

    // Bg960 quadrant boundaries (1-indexed point indices)
    private static readonly (int from, int to)[] Quadrants =
    [
        (1,  6),   // home board
        (7,  12),  // outer board
        (13, 18),  // opponent outer board
        (19, 24),  // opponent home board
    ];

    // Bg960 made-point weights: num_points → weight
    private static readonly (int points, int weight)[] MadePointWeights =
    [
        (2, 1), (3, 3), (4, 10), (5, 10), (6, 5), (7, 2),
    ];

    /// <summary>
    /// Generate a random Bg960 starting position, in the on-roll player's frame.
    /// Constraints: symmetrical, no blots (≥ 2 per point), one point per quadrant,
    /// no mirror conflicts, pip count ≥ 100, weighted toward 4–5 made points.
    /// </summary>
    /// <param name="seed">Optional RNG seed for reproducibility. Null = random.</param>
    /// <exception cref="InvalidOperationException">No valid position found in 1000 attempts.</exception>
    public static BoardPosition Bg960(int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // Precompute sampling distribution
        int maxPoints = CheckersPerSide / 2;  // min 2 checkers per point → max 7 points
        int totalWeight = 0;
        for (int i = 0; i < MadePointWeights.Length; i++)
            if (MadePointWeights[i].points >= 4 && MadePointWeights[i].points <= maxPoints)
                totalWeight += MadePointWeights[i].weight;

        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int numPoints = SampleNumPoints(rng, totalWeight);
            int[]? points = SelectPoints(rng, numPoints);
            if (points == null) continue;

            int[] checkers = DistributeCheckers(rng, points, CheckersPerSide, 2);

            // Check pip count (1-indexed: point i contributes checkers[i-1] * i)
            int pips = 0;
            for (int i = 0; i < points.Length; i++)
                pips += checkers[i] * points[i];
            if (pips < 100) continue;

            // Build board
            var counts = new int[SlotCount];
            for (int i = 0; i < points.Length; i++)
            {
                int pt = points[i];
                int mirror = 25 - pt;   // 1-indexed mirror
                counts[pt] = checkers[i];
                counts[mirror] = -checkers[i];
            }
            return new BoardPosition(counts);
        }

        throw new InvalidOperationException("Bg960: failed to generate valid position in 1000 attempts");
    }

    private static int SampleNumPoints(Random rng, int totalWeight)
    {
        int r = rng.Next(totalWeight);
        int cumulative = 0;
        for (int i = 0; i < MadePointWeights.Length; i++)
        {
            var (points, weight) = MadePointWeights[i];
            if (points < 4 || points > CheckersPerSide / 2) continue;
            cumulative += weight;
            if (r < cumulative) return points;
        }
        return MadePointWeights[^1].points;
    }

    /// <summary>
    /// Select numPoints distinct points satisfying quadrant coverage and no mirror conflicts.
    /// Returns null if 1000 inner attempts fail.
    /// </summary>
    private static int[]? SelectPoints(Random rng, int numPoints)
    {
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            var blocked = new HashSet<int>();
            var mandatory = new List<int>();
            bool failed = false;

            // One mandatory point per quadrant
            foreach (var (from, to) in Quadrants)
            {
                var candidates = new List<int>();
                for (int p = from; p <= to; p++)
                    if (!blocked.Contains(p)) candidates.Add(p);

                if (candidates.Count == 0) { failed = true; break; }

                int pt = candidates[rng.Next(candidates.Count)];
                mandatory.Add(pt);
                blocked.Add(pt);
                blocked.Add(25 - pt);   // block mirror
            }

            if (failed) continue;

            if (numPoints < mandatory.Count) continue;

            // Fill remaining slots
            int remaining = numPoints - mandatory.Count;
            var available = new List<int>();
            for (int p = 1; p <= 24; p++)
                if (!blocked.Contains(p)) available.Add(p);

            if (remaining > available.Count) continue;

            var extra = new List<int>();
            for (int i = 0; i < remaining; i++)
            {
                if (available.Count == 0) break;
                int idx = rng.Next(available.Count);
                int pt = available[idx];
                extra.Add(pt);
                available.RemoveAt(idx);
                available.Remove(25 - pt);  // remove mirror
            }

            if (extra.Count < remaining) continue;

            mandatory.AddRange(extra);
            mandatory.Sort();
            return mandatory.ToArray();
        }

        return null;
    }

    /// <summary>
    /// Distribute totalCheckers across points with at least minPerPoint each.
    /// Remainder distributed via stars-and-bars (sorted random dividers).
    /// </summary>
    private static int[] DistributeCheckers(Random rng, int[] points, int totalCheckers, int minPerPoint)
    {
        int k = points.Length;
        int remainder = totalCheckers - minPerPoint * k;
        int[] extra = new int[k];

        if (remainder > 0)
        {
            // Stars and bars: k-1 random dividers in [0, remainder]
            int[] dividers = new int[k - 1];
            for (int i = 0; i < dividers.Length; i++)
                dividers[i] = rng.Next(remainder + 1);
            Array.Sort(dividers);

            int prev = 0;
            for (int i = 0; i < k - 1; i++)
            {
                extra[i] = dividers[i] - prev;
                prev = dividers[i];
            }
            extra[k - 1] = remainder - prev;
        }

        int[] result = new int[k];
        for (int i = 0; i < k; i++)
            result[i] = minPerPoint + extra[i];
        return result;
    }

    /// <summary>
    /// The count on one slot: negative for the opponent's checkers, positive
    /// for the on-roll player's, 0 for an empty slot.
    /// </summary>
    /// <param name="point">
    /// The slot, 0–25: 0 is the opponent's bar, 1–24 the points, 25 the
    /// on-roll player's bar.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="point"/> is outside 0–25.</exception>
    public int this[int point]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(point);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(point, SlotCount);
            return _counts[point];
        }
    }

    /// <summary>
    /// Copies the 26 counts, in the layout of the type summary, into the
    /// first 26 elements of <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">A span of at least 26 elements.</param>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than 26 elements.</exception>
    public void CopyTo(Span<int> destination)
    {
        if (destination.Length < SlotCount)
            throw new ArgumentException(
                $"The destination must hold at least {SlotCount} counts; it holds {destination.Length}.",
                nameof(destination));

        ReadOnlySpan<sbyte> counts = _counts;
        for (int i = 0; i < SlotCount; i++)
            destination[i] = counts[i];
    }

    /// <summary>
    /// This position seen from the other side — re-expressed in the other
    /// player's frame, as <see cref="BoardState.ApplyPlay"/> leaves a board
    /// at a turn boundary. Slot <c>i</c> takes the negated count of slot
    /// <c>25 - i</c>: the points mirror, the bars swap (0 ↔ 25), and every
    /// sign inverts, so the other player's checkers become the positive
    /// ones. The one statement of the flip: every flip in this library goes
    /// through it.
    /// </summary>
    /// <remarks>
    /// An involution — <c>p.Flipped().Flipped() == p</c> — and
    /// allocation-free. The result is well-formed without a check: each bar's
    /// count moves to the other bar with its sign inverted, so each bar still
    /// holds only its own side's checkers, and the two side totals swap.
    /// </remarks>
    public BoardPosition Flipped()
    {
        ReadOnlySpan<sbyte> counts = _counts;
        var flipped = new Counts();
        for (int i = 0; i < SlotCount; i++)
            flipped[i] = (sbyte)-counts[SlotCount - 1 - i];
        return new BoardPosition(flipped);
    }

    /// <summary>
    /// Whether <paramref name="other"/> is the same position: all 26 counts
    /// equal, both bars included.
    /// </summary>
    public bool Equals(BoardPosition other) =>
        ((ReadOnlySpan<sbyte>)_counts).SequenceEqual(other._counts);

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is BoardPosition other && Equals(other);

    /// <summary>
    /// A hash over all 26 counts, consistent with <see cref="Equals(BoardPosition)"/>.
    /// Never identity, and seeded per process — see the type remarks.
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(MemoryMarshal.AsBytes((ReadOnlySpan<sbyte>)_counts));
        return hash.ToHashCode();
    }

    /// <summary>Whether the two are the same position (<see cref="Equals(BoardPosition)"/>).</summary>
    public static bool operator ==(BoardPosition left, BoardPosition right) => left.Equals(right);

    /// <summary>Whether the two are different positions (<see cref="Equals(BoardPosition)"/>).</summary>
    public static bool operator !=(BoardPosition left, BoardPosition right) => !left.Equals(right);

    /// <summary>
    /// The occupied slots as <c>slot:count</c> pairs in ascending slot order,
    /// separated by spaces — <c>"1:-2 6:5 8:3 12:-5 13:5 17:-3 19:-5 24:2"</c>
    /// for the standard start — or <c>"empty"</c> for the empty board. Each
    /// pair names its slot, so two positions that differ are easy to tell
    /// apart in a test failure.
    /// </summary>
    public override string ToString()
    {
        ReadOnlySpan<sbyte> counts = _counts;
        var text = new StringBuilder();
        for (int i = 0; i < SlotCount; i++)
        {
            if (counts[i] == 0)
                continue;
            if (text.Length > 0)
                text.Append(' ');
            text.Append(i).Append(':').Append(counts[i]);
        }
        return text.Length == 0 ? "empty" : text.ToString();
    }

    /// <summary>
    /// The first way <paramref name="counts"/> fails to form a position, as
    /// a message naming it, or <see langword="null"/> when it forms one. The
    /// one statement of the invariant in code.
    /// </summary>
    private static string? FindFault(ReadOnlySpan<int> counts)
    {
        if (counts.Length != SlotCount)
            return $"A position has exactly {SlotCount} counts; got {counts.Length}.";

        // Each count's magnitude first, so the side totals below cannot
        // overflow; one slot beyond a side's checkers is already malformed.
        for (int i = 0; i < SlotCount; i++)
        {
            if (counts[i] is < -CheckersPerSide or > CheckersPerSide)
                return $"Slot {i} holds a count of {counts[i]}; a side has only {CheckersPerSide} checkers.";
        }

        if (counts[0] > 0)
            return $"The opponent's bar (slot 0) holds {counts[0]} of the on-roll player's checkers; it holds only the opponent's.";
        if (counts[SlotCount - 1] < 0)
            return $"The on-roll player's bar (slot {SlotCount - 1}) holds {-counts[SlotCount - 1]} of the opponent's checkers; it holds only the on-roll player's.";

        int onRoll = 0, opponent = 0;
        foreach (int count in counts)
        {
            if (count > 0) onRoll += count;
            else opponent -= count;
        }
        if (onRoll > CheckersPerSide)
            return $"The on-roll player has {onRoll} checkers on the board; a side has at most {CheckersPerSide}.";
        if (opponent > CheckersPerSide)
            return $"The opponent has {opponent} checkers on the board; a side has at most {CheckersPerSide}.";
        return null;
    }

    /// <summary>
    /// The counts in storage form. Lossless for any board the invariant
    /// admits; callers establish it first.
    /// </summary>
    private static Counts Narrow(ReadOnlySpan<int> counts)
    {
        var narrowed = new Counts();
        for (int i = 0; i < SlotCount; i++)
            narrowed[i] = (sbyte)counts[i];
        return narrowed;
    }

    /// <summary>The 26 counts, stored inline.</summary>
    [InlineArray(SlotCount)]
    private struct Counts
    {
        private sbyte _element0;
    }
}
