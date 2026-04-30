namespace BgDataTypes_Lib;

/// <summary>
/// Mutable backgammon board position.
///
/// <para>
/// <c>Points[0..25]</c>: 26-element array.
///   <c>Points[25]</c> = on-roll player's bar,
///   <c>Points[1..24]</c> = playing surface,
///   <c>Points[0]</c> = opponent's bar.
/// </para>
///
/// <para>
/// Positive values = on-roll player's checkers; negative = opponent's.
/// On-roll moves from high indices toward low (25 → 1, bearing off past 1).
/// Layout matches <c>PositionData.Mop</c> / <c>IDecisionFilterData.Board</c>.
/// </para>
///
/// <para>
/// Designed for apply/undo mutation — no heap allocations during move generation.
/// Mutability is a deliberate exception within BgDataTypes_Lib (everything else
/// is class with init-only properties); see INSTRUCTIONS.md "Mutability exception".
/// </para>
///
/// <para>
/// Hot-path consumers use <see cref="ApplyMove(Move)"/> / <see cref="UndoMove(Move)"/>
/// to recurse through candidate plays. Non-hot-path consumers should advance
/// state via <see cref="ApplyPlay(Play)"/>, which applies all moves and
/// transparently flips perspective so the next call still reasons in on-roll POV.
/// </para>
/// </summary>
public class BoardState
{
    public readonly int[] Points = new int[26];

    /// <summary>
    /// Highest point (1–25) with an on-roll checker, 0 if none.
    /// Maintained incrementally by <see cref="ApplyMove(Move)"/> /
    /// <see cref="UndoMove(Move)"/>; recomputed by <see cref="RecalcHighPoint"/>
    /// after bulk mutation (e.g., <see cref="FromMop"/>, internal flip).
    /// External code that mutates <see cref="Points"/> directly must call
    /// <see cref="RecalcHighPoint"/> or this field will desync.
    /// </summary>
    public int HighPointOccupied;

    public BoardState() { }

    /// <summary>
    /// Recompute <see cref="HighPointOccupied"/> from scratch. Call after
    /// directly mutating <see cref="Points"/>.
    /// </summary>
    public void RecalcHighPoint()
    {
        HighPointOccupied = 0;
        for (int i = 25; i >= 1; i--)
        {
            if (Points[i] > 0) { HighPointOccupied = i; return; }
        }
    }

    /// <summary>Deep copy.</summary>
    public BoardState Copy()
    {
        var copy = new BoardState();
        Array.Copy(Points, copy.Points, 26);
        copy.HighPointOccupied = HighPointOccupied;
        return copy;
    }

    // ── Mop bridge ────────────────────────────────────────────────

    /// <summary>
    /// Build a BoardState from a 26-element on-roll-relative point array
    /// (the <c>Mop</c> shape used by <c>PositionData.Mop</c> and
    /// <c>IDecisionFilterData.Board</c>). Layout matches <see cref="Points"/> exactly:
    /// <c>[0]</c> = opponent bar, <c>[1..24]</c> = playing surface, <c>[25]</c> = on-roll bar;
    /// positive = on-roll's checkers, negative = opponent's.
    /// <see cref="HighPointOccupied"/> is recomputed from the copied points.
    /// No checker-count or sign validation is performed — pseudoboards
    /// (e.g., cube-decision references) are legitimate inputs.
    /// </summary>
    public static BoardState FromMop(IReadOnlyList<int> mop)
    {
        ArgumentNullException.ThrowIfNull(mop);
        if (mop.Count != 26)
            throw new ArgumentException(
                $"Mop must have exactly 26 elements; got {mop.Count}.", nameof(mop));

        var s = new BoardState();
        for (int i = 0; i < 26; i++)
            s.Points[i] = mop[i];
        s.RecalcHighPoint();
        return s;
    }

    /// <summary>
    /// Return <see cref="Points"/> as a fresh 26-element list — same layout
    /// as <see cref="FromMop"/> accepts. Defensive copy: subsequent mutations
    /// of this BoardState do not affect the returned list, and vice versa.
    /// </summary>
    public IReadOnlyList<int> ToMop()
    {
        var copy = new int[26];
        Array.Copy(Points, copy, 26);
        return copy;
    }

    // ── Standard starting positions ───────────────────────────────

    /// <summary>
    /// Standard backgammon starting position.
    /// Player's checkers: 6-pt(5), 8-pt(3), 13-pt(5), 24-pt(2)
    /// Opponent's checkers: 19-pt(-5), 17-pt(-3), 12-pt(-5), 1-pt(-2)
    /// </summary>
    public static BoardState Standard()
    {
        var s = new BoardState();
        s.Points[6] = 5;
        s.Points[8] = 3;
        s.Points[13] = 5;
        s.Points[24] = 2;
        s.Points[19] = -5;
        s.Points[17] = -3;
        s.Points[12] = -5;
        s.Points[1] = -2;
        s.RecalcHighPoint();
        return s;
    }

    /// <summary>Nackgammon starting position.</summary>
    public static BoardState Nackgammon()
    {
        var s = new BoardState();
        s.Points[6] = 4;
        s.Points[8] = 3;
        s.Points[13] = 4;
        s.Points[23] = 2;
        s.Points[24] = 2;
        s.Points[19] = -4;
        s.Points[17] = -3;
        s.Points[12] = -4;
        s.Points[2] = -2;
        s.Points[1] = -2;
        s.RecalcHighPoint();
        return s;
    }

    // ── Bg960 setup ───────────────────────────────────────────────

    // Quadrant boundaries (1-indexed point indices)
    private static readonly (int from, int to)[] Quadrants =
    [
        (1,  6),   // home board
        (7,  12),  // outer board
        (13, 18),  // opponent outer board
        (19, 24),  // opponent home board
    ];

    // Made-point weights: num_points → weight
    private static readonly (int points, int weight)[] MadePointWeights =
    [
        (2, 1), (3, 3), (4, 10), (5, 10), (6, 5), (7, 2),
    ];

    /// <summary>
    /// Generate a random Bg960 starting position.
    /// Constraints: symmetrical, no blots (≥ 2 per point), one point per quadrant,
    /// no mirror conflicts, pip count ≥ 100, weighted toward 4–5 made points.
    /// </summary>
    /// <param name="seed">Optional RNG seed for reproducibility. Null = random.</param>
    /// <exception cref="InvalidOperationException">No valid position found in 1000 attempts.</exception>
    public static BoardState Bg960(int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        // Precompute sampling distribution
        int maxPoints = 15 / 2;  // min 2 checkers per point → max 7 points
        int totalWeight = 0;
        for (int i = 0; i < MadePointWeights.Length; i++)
            if (MadePointWeights[i].points >= 4 && MadePointWeights[i].points <= maxPoints)
                totalWeight += MadePointWeights[i].weight;

        for (int attempt = 0; attempt < 1000; attempt++)
        {
            int numPoints = SampleNumPoints(rng, totalWeight);
            int[]? points = SelectPoints(rng, numPoints);
            if (points == null) continue;

            int[] checkers = DistributeCheckers(rng, points, 15, 2);

            // Check pip count (1-indexed: point i contributes checkers[i-1] * i)
            int pips = 0;
            for (int i = 0; i < points.Length; i++)
                pips += checkers[i] * points[i];
            if (pips < 100) continue;

            // Build board
            var s = new BoardState();
            for (int i = 0; i < points.Length; i++)
            {
                int pt = points[i];
                int mirror = 25 - pt;   // 1-indexed mirror
                s.Points[pt] = checkers[i];
                s.Points[mirror] = -checkers[i];
            }
            s.RecalcHighPoint();
            return s;
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
            if (points < 4 || points > 15 / 2) continue;
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

    // ── Apply / Undo (instance, hot-path) ─────────────────────────

    /// <summary>
    /// Apply a single move in place. Maintains <see cref="HighPointOccupied"/>
    /// incrementally — when emptying the highest point, scans down for the new high.
    /// No legality validation; callers (e.g. BgMoveGen.MoveGenerator) own that.
    /// </summary>
    public void ApplyMove(Move move)
    {
        Points[move.FrPt]--;
        if (move.ToPt > 0)
        {
            Points[move.ToPt]++;
        }
        else if (move.ToPt < 0)
        {
            int dest = -move.ToPt;
            Points[dest] = 1;
            Points[0]--;
        }
        // ToPt == 0: bear off, checker disappears.

        if (move.FrPt == HighPointOccupied && Points[move.FrPt] == 0)
        {
            HighPointOccupied = 0;
            for (int i = move.FrPt - 1; i >= 1; i--)
            {
                if (Points[i] > 0) { HighPointOccupied = i; break; }
            }
        }
    }

    /// <summary>
    /// Reverse a previously applied move in place. The move encodes everything
    /// needed (regular / hit / bear-off) — no separate undo log required.
    /// </summary>
    public void UndoMove(Move move)
    {
        if (move.ToPt > 0)
        {
            Points[move.ToPt]--;
        }
        else if (move.ToPt < 0)
        {
            int dest = -move.ToPt;
            Points[dest] = -1;
            Points[0]++;
        }

        Points[move.FrPt]++;
        if (move.FrPt > HighPointOccupied)
            HighPointOccupied = move.FrPt;
    }

    // ── Turn boundary: ApplyPlay (apply-all + flip) ───────────────

    /// <summary>
    /// Apply all moves in <paramref name="play"/> and flip perspective so the
    /// state is re-expressed from the next mover's POV. After this call, the
    /// previous opponent is on roll; positive values are now their checkers.
    ///
    /// <para>
    /// This is the turn-boundary primitive: callers reasoning in on-roll POV
    /// never need to flip directly. Empty plays (<c>play.Count == 0</c>) still
    /// flip — they represent a forced pass.
    /// </para>
    ///
    /// <para>
    /// No legality validation. Validating wrappers (legal-play check + apply)
    /// belong with the move generator (<c>BgMoveGen</c>); BoardState here is
    /// the raw data primitive.
    /// </para>
    /// </summary>
    public void ApplyPlay(Play play)
    {
        for (int i = 0; i < play.Count; i++)
            ApplyMove(play[i]);
        Flip();
    }

    /// <summary>
    /// Flip perspective: negate every value and reverse the array, so points
    /// 0↔25, 1↔24, …, 12↔13. Implementation mechanics for
    /// <see cref="ApplyPlay(Play)"/> — never exposed publicly. Callers should
    /// always reason in on-roll POV; <see cref="ApplyPlay(Play)"/> performs
    /// the flip atomically with the move application.
    ///
    /// Borne-off counts are not tracked on <see cref="BoardState"/> (checkers
    /// simply leave the board), so nothing else needs swapping.
    /// </summary>
    private void Flip()
    {
        for (int i = 0; i < 13; i++)
        {
            int j = 25 - i;
            int a = Points[i];
            int b = Points[j];
            Points[i] = -b;
            Points[j] = -a;
        }
        // Flip changed which point is "high"; recompute from scratch.
        RecalcHighPoint();
    }

    // ── Derived properties ────────────────────────────────────────

    /// <summary>
    /// Pip count for the on-roll player: sum over <c>i ∈ [1..25]</c> of
    /// <c>i × Points[i]</c> for positive entries. Bar checkers contribute 25
    /// pips each; bear-off contributes 0 (checkers off the board are gone).
    ///
    /// <para>
    /// Use <see langword="int"/> arithmetic — products fit comfortably (15 × 25 = 375
    /// max contribution; total ≤ 375). See BgMoveGen pitfall on integer width.
    /// </para>
    ///
    /// <para>
    /// Distinct from <c>PositionData.OnRollPipCount</c>, which carries the
    /// XG-parser-supplied value. This property is a pure derivation from
    /// <see cref="Points"/> and may not match parser output bit-for-bit if XG
    /// ever rounds.
    /// </para>
    /// </summary>
    public int PipCount
    {
        get
        {
            int total = 0;
            for (int i = 1; i <= 25; i++)
            {
                int n = Points[i];
                if (n > 0) total += i * n;
            }
            return total;
        }
    }

    /// <summary>
    /// Pip count for the opponent: sum over <c>i ∈ [0..24]</c> of
    /// <c>(25 - i) × |Points[i]|</c> for negative entries. Opponent's bar
    /// (index 0) contributes 25 pips per checker; opponent moves
    /// low-index → high-index in the on-roll storage frame, so distance to
    /// bear-off from index <c>i</c> is <c>25 - i</c>.
    /// </summary>
    public int OpponentPipCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i <= 24; i++)
            {
                int n = Points[i];
                if (n < 0) total += (25 - i) * (-n);
            }
            return total;
        }
    }

    /// <summary>
    /// True iff the position is a race — no on-roll checker can ever collide
    /// with an opponent checker. Equivalent statements:
    /// <list type="bullet">
    /// <item>Backgammon-natural: opponent's furthest-back checker is past
    /// on-roll's furthest-back checker.</item>
    /// <item>Array-index form: <c>max(i where Points[i] &gt; 0) &lt; min(i where Points[i] &lt; 0)</c>.</item>
    /// </list>
    /// On-roll moves high → low in this frame; opponent moves low → high. A
    /// race exists iff the two ranges no longer overlap. If either side has
    /// no checkers (all borne off), the position is vacuously a race.
    /// </summary>
    public bool IsRace
    {
        get
        {
            int maxOnRoll = int.MinValue;
            int minOpponent = int.MaxValue;
            for (int i = 0; i <= 25; i++)
            {
                int n = Points[i];
                if (n > 0) { if (i > maxOnRoll) maxOnRoll = i; }
                else if (n < 0) { if (i < minOpponent) minOpponent = i; }
            }
            return maxOnRoll < minOpponent;
        }
    }
}
