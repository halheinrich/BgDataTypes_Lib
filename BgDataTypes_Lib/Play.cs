using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A complete play: the sequence of moves for one turn.
/// Uses a fixed-size buffer (max 4 moves for doubles) to avoid heap allocation.
///
/// Construct with <c>Play.Create(…)</c> when the moves are known up front —
/// the fixed-arity overloads (<see cref="Create(Move)"/> through
/// <see cref="Create(Move, Move, Move, Move)"/>) for a literal call site,
/// <see cref="Create(ReadOnlySpan{Move})"/> for moves already in a span or
/// array — or, equivalently, a collection expression:
/// <c>Play play = [new(13, 10), new(10, 8)];</c>, with <c>[]</c> the empty
/// play, a forced pass. The fixed-arity overloads construct at parity with
/// <see cref="Add"/>; see their remarks for when that matters.
/// <see cref="Add"/> / <see cref="RemoveLast"/> are the incremental build
/// primitives for callers that discover moves one at a time
/// (move-generation recursion). Read with <c>foreach</c> (see
/// <see cref="GetEnumerator"/>) or the indexer.
///
/// A play has no equality. Whether two plays are the same play depends on the
/// position they are played from, which a play does not carry: identity is
/// <see cref="BoardState.IsSamePlay"/>, asked of the starting position, and
/// the list match is <see cref="BoardState.IndexOfSamePlay"/>. So
/// <c>==</c> and <c>!=</c> are not defined, and <see cref="Equals(object)"/>
/// and <see cref="GetHashCode"/> throw (halheinrich/backgammon#273, ruling A).
/// <see cref="IsSameEncoding"/> compares exact encodings, for storage.
/// <see cref="ToNotation"/> writes the play in standard notation, its display
/// form.
///
/// Serialised as a JSON array of <see cref="Move"/> via <see cref="PlayJsonConverter"/>;
/// the raw move sequence round-trips exactly. The private buffer fields and the
/// <see cref="Count"/> setter are not exposed to the default property-based
/// serialiser.
/// </summary>
[JsonConverter(typeof(PlayJsonConverter))]
[CollectionBuilder(typeof(Play), nameof(Create))]
public struct Play
{
    // Fixed buffer: max 4 moves (doubles)
    private Move _m0, _m1, _m2, _m3;

    /// <summary>Number of moves in the play (0–4). 0 is the empty play — a forced pass.</summary>
    public int Count { get; private set; }

    /// <summary>
    /// Creates a play holding <paramref name="moves"/>, in the given order —
    /// the general-arity construction door, and the type's
    /// <see cref="CollectionBuilderAttribute"/> target, so collection
    /// expressions land here: <c>Play p = [new(13, 10), new(10, 8)];</c>,
    /// with <c>[]</c> — equivalently <c>Play.Create()</c> — the empty play,
    /// a forced pass.
    /// </summary>
    /// <remarks>
    /// <b>Division of labour with the fixed-arity overloads.</b> Reach for
    /// this one when the moves are already a span, an array, or a collection
    /// expression. When they are separate values at the call site, the
    /// fixed-arity overloads (<see cref="Create(Move)"/> through
    /// <see cref="Create(Move, Move, Move, Move)"/>) are the ones to call,
    /// and overload resolution picks them without help. A <c>params</c> span
    /// argument list is materialised into a buffer by the <em>caller</em>
    /// before the call, and that buffer round-trip is the one cost this
    /// method cannot optimise away — it happens outside the method.
    /// Measured against the incremental <see cref="Add"/> spelling on
    /// <c>PlayConstructionBenchmarks</c>, the fixed-arity overloads run at
    /// parity (0.85–0.96x across arities 1–4) while this one costs
    /// 1.3–1.8x; both allocate nothing. Collection expressions necessarily
    /// route here and carry the same overhead (1.3–2.1x) — they are a
    /// readability idiom, not a hot-path one.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="moves"/> holds more than 4 moves (the doubles maximum).
    /// </exception>
    // Load-bearing: demotes this overload so a typeless new(...)
    // single-argument call binds Create(Move) instead of tying with the
    // params span expansion (CS0121). Deleting this attribute breaks those
    // call sites — Play.Create(new(13, 7)) is one, pinned by
    // PlayTests.Create_OneMove_TypelessNew_Compiles.
    [OverloadResolutionPriority(-1)]
    public static Play Create(params ReadOnlySpan<Move> moves)
    {
        if (moves.Length > 4)
            ThrowTooManyMoves(moves.Length, nameof(moves));

        // Unrolled straight-line SetSlot calls with *literal* slot indices,
        // deliberately, rather than the obvious loop over Add. Add reaches
        // the same seam, so there is still exactly one encoding of how a
        // move lands in a slot — but Add necessarily passes Count, a value
        // the JIT can only fold when it knows it, and it does not know it
        // here: the params span points into a caller-side stack buffer,
        // which leaves the in-progress play address-exposed and Count a
        // memory load. A literal index folds SetSlot's switch
        // unconditionally. Measured on PlayConstructionBenchmarks
        // (halheinrich/backgammon#137): the loop-over-Add original cost
        // 3.94x the raw Add spelling at four moves; an unrolled *Add* ladder
        // still cost 1.64x, because every Add re-read Count from memory and
        // dispatched through a jump table; this shape costs 1.6x, all of it
        // the caller's argument buffer.
        var play = new Play();
        if (moves.Length > 0) play.SetSlot(0, moves[0]);
        if (moves.Length > 1) play.SetSlot(1, moves[1]);
        if (moves.Length > 2) play.SetSlot(2, moves[2]);
        if (moves.Length > 3) play.SetSlot(3, moves[3]);
        return play;
    }

    /// <summary>Creates a one-move play.</summary>
    /// <remarks>
    /// Fixed-arity: the move goes straight into the play's slot, with no
    /// argument buffer in between. See
    /// <see cref="Create(ReadOnlySpan{Move})"/> for the division of labour
    /// between these overloads and the general-arity one.
    /// </remarks>
    public static Play Create(Move move0)
    {
        var play = new Play();
        play.SetSlot(0, move0);
        return play;
    }

    /// <summary>Creates a two-move play, in the given order.</summary>
    /// <inheritdoc cref="Create(Move)" path="/remarks"/>
    public static Play Create(Move move0, Move move1)
    {
        var play = new Play();
        play.SetSlot(0, move0);
        play.SetSlot(1, move1);
        return play;
    }

    /// <summary>Creates a three-move play, in the given order.</summary>
    /// <inheritdoc cref="Create(Move)" path="/remarks"/>
    public static Play Create(Move move0, Move move1, Move move2)
    {
        var play = new Play();
        play.SetSlot(0, move0);
        play.SetSlot(1, move1);
        play.SetSlot(2, move2);
        return play;
    }

    /// <summary>
    /// Creates a four-move play, in the given order — the doubles maximum.
    /// </summary>
    /// <inheritdoc cref="Create(Move)" path="/remarks"/>
    public static Play Create(Move move0, Move move1, Move move2, Move move3)
    {
        var play = new Play();
        play.SetSlot(0, move0);
        play.SetSlot(1, move1);
        play.SetSlot(2, move2);
        play.SetSlot(3, move3);
        return play;
    }

    /// <summary>
    /// Places <paramref name="move"/> in slot <paramref name="index"/> and
    /// makes the play <paramref name="index"/> + 1 moves long — the single
    /// source of both halves of "a move lands in the play": the ordinal →
    /// field mapping and the <see cref="Count"/> maintenance that goes with
    /// it. <see cref="Add"/> and all five <c>Create</c> overloads are its
    /// only callers, and none of them touches a slot field or
    /// <see cref="Count"/> itself, so no construction path can drift from
    /// any other.
    ///
    /// <para>
    /// Callers must fill slots densely from 0 upwards: the play's length is
    /// taken from the last slot written, so writing slot 3 of an empty play
    /// would claim four moves and expose three uninitialised ones.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSlot(int index, Move move)
    {
        switch (index)
        {
            case 0: _m0 = move; break;
            case 1: _m1 = move; break;
            case 2: _m2 = move; break;
            case 3: _m3 = move; break;
            default: ThrowSlotOutOfRange(index); break;
        }
        Count = index + 1;
    }

    /// <summary>
    /// The <see cref="Create(ReadOnlySpan{Move})"/> overflow throw, out of
    /// line. Kept out of that method's body deliberately: the interpolated
    /// message costs enough IL to push it past the JIT's inlining budget,
    /// and an un-inlined <c>Create</c> is exactly the regression this shape
    /// exists to avoid. The parameter name travels in rather than being
    /// spelled literally here, so a rename of <c>Create</c>'s parameter
    /// cannot silently desync the exception contract.
    /// </summary>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTooManyMoves(int count, string paramName) =>
        throw new ArgumentException(
            $"A play has at most 4 moves, got {count}.", paramName);

    /// <summary>
    /// The <see cref="SetSlot"/> guard, out of line for the same reason as
    /// <see cref="ThrowTooManyMoves"/>. Unreachable through the public
    /// surface — every caller bounds the index first — so this is a defect
    /// trap for a future one, not a documented contract.
    /// </summary>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowSlotOutOfRange(int index) =>
        throw new ArgumentOutOfRangeException(
            nameof(index), index, "A play has 4 slots, numbered 0 to 3.");

    /// <summary>The move at <paramref name="index"/> (0 to <see cref="Count"/> − 1), in insertion order.</summary>
    public readonly Move this[int index] => index switch
    {
        0 => _m0,
        1 => _m1,
        2 => _m2,
        3 => _m3,
        _ => throw new IndexOutOfRangeException()
    };

    /// <summary>
    /// Appends <paramref name="move"/> to the play. Throws
    /// <see cref="InvalidOperationException"/> when the play already holds
    /// 4 moves (the doubles maximum). Mutates in place — see the
    /// value-type caveat on <see cref="Snapshot"/>.
    /// </summary>
    public void Add(Move move)
    {
        if ((uint)Count >= 4)
            throw new InvalidOperationException("Play already has 4 moves");
        SetSlot(Count, move);
    }

    /// <summary>
    /// Removes the most recently added move (undo support for
    /// move-generation recursion). Throws <see cref="InvalidOperationException"/>
    /// when the play is empty.
    /// </summary>
    public void RemoveLast()
    {
        if (Count == 0) throw new InvalidOperationException("Play is empty");
        Count--;
    }

    /// <summary>
    /// An explicit independent copy. <see cref="Play"/> is a mutable value
    /// type, so any assignment already copies — use this where the copy is
    /// the point (e.g. capturing the current play during generation), making
    /// the intent visible at the call site.
    /// </summary>
    public readonly Play Snapshot()
    {
        var copy = new Play();
        copy._m0 = _m0;
        copy._m1 = _m1;
        copy._m2 = _m2;
        copy._m3 = _m3;
        copy.Count = Count;
        return copy;
    }

    /// <summary>
    /// The canonical chain form of this play — its display form: the chains
    /// its notation shows and where the hit marks go (see
    /// <see cref="CanonicalPlay"/>). Not identity; see the type summary.
    /// </summary>
    public readonly CanonicalPlay ToCanonical() => CanonicalPlay.FromPlay(in this);

    /// <summary>
    /// This play in standard backgammon notation — <c>"24/18* 13/9"</c>,
    /// <c>"bar/22"</c>, <c>"6/off"</c>, <c>"8/5(2) 6/3(2)"</c> — and the empty
    /// string for a pass. The one public way to spell a play
    /// (halheinrich/backgammon#273); <see cref="object.ToString"/> is not it,
    /// because an encoding holds more than its notation shows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each term is <c>from/to</c>, written in order of source point
    /// descending, then landing point descending: a source of 25 is written
    /// <c>bar</c>, a landing of 0 (bear-off) <c>off</c>, and a hit adds
    /// <c>*</c>. Consecutive moves of one checker are written as one term,
    /// so the order the moves are listed in and how a trajectory is split
    /// into hops do not show (13/10 10/8 and 13/8 are both <c>"13/8"</c>),
    /// except that a hit stays visible: a checker hitting on an intermediate
    /// point is written as two terms (13/10*/8 is <c>"13/10* 10/8"</c>). A
    /// hit belongs to its point, so it is marked once, on the first term
    /// landing there, whichever move recorded it (8/3* 7/3 and 8/3 7/3* are
    /// both <c>"8/3* 7/3"</c>). Identical terms are written once with their
    /// count, <c>"8/5(2)"</c>, the mark after the count, <c>"6/2(2)*"</c>.
    /// </para>
    /// <para>
    /// The notation is the play's display form, not its identity: a
    /// multi-die move pairs sources with destinations as written, so two
    /// encodings of one play can be written differently (<c>"13/9 11/7"</c>
    /// and <c>"13/7 11/9"</c>). Whether two plays are the same play is
    /// <see cref="BoardState.IsSamePlay"/>, from the position they are
    /// played from. Point numbers are written with the invariant culture.
    /// </para>
    /// </remarks>
    /// <returns>The notation, or <see cref="string.Empty"/> for the empty play.</returns>
    public readonly string ToNotation() => ToCanonical().ToString();

    /// <summary>
    /// Whether <paramref name="other"/> is the identical encoding: the same
    /// moves, in the same order, with the same hit marks. For storage and
    /// round-trip checks. <b>This is not play identity</b> — two encodings of
    /// one play (another move order, another decomposition into hops, the
    /// hit mark on another checker) differ here; identity is
    /// <see cref="BoardState.IsSamePlay"/>. Only the first <see cref="Count"/>
    /// moves are compared: a slot left behind by <see cref="RemoveLast"/> is
    /// not part of the encoding.
    /// </summary>
    public readonly bool IsSameEncoding(Play other)
    {
        if (Count != other.Count) return false;
        for (int i = 0; i < Count; i++)
            if (this[i] != other[i]) return false;
        return true;
    }

    /// <summary>
    /// Not supported: <see cref="Play"/> has no equality (see the type
    /// summary), so this throws. It is overridden only so that every
    /// comparison the runtime routes here — a boxed <c>Equals</c>, an equality
    /// comparer, a hash set or dictionary, <c>Distinct</c>, a record or tuple
    /// holding a play — fails loudly, instead of falling back to the default
    /// field-wise struct equality, which would compare stale slots.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override readonly bool Equals(object? obj) => throw NoEquality();

    /// <summary>
    /// Not supported, as for <see cref="Equals(object)"/>: a play has no
    /// equality, so it has no hash either, and this throws.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override readonly int GetHashCode() => throw NoEquality();

    private static NotSupportedException NoEquality() => new(
        "Play has no equality: whether two plays are the same play depends on the position they "
        + "are played from. Use BoardState.IsSamePlay, or BoardState.IndexOfSamePlay to find a play "
        + "in a list; Play.IsSameEncoding compares exact encodings, for storage.");

    /// <summary>
    /// An allocation-free enumerator over the moves in insertion order,
    /// making <c>foreach (var move in play)</c> the read idiom. The
    /// enumerator carries its own copy of the play (the value-type copy any
    /// assignment already makes), so mutating the source mid-enumeration
    /// cannot affect the sequence. The type deliberately implements the
    /// <c>foreach</c> pattern rather than <see cref="IEnumerable{T}"/> —
    /// the interface would box on every use.
    /// </summary>
    public readonly Enumerator GetEnumerator() => new(in this);

    /// <summary>
    /// Move enumerator for <see cref="Play"/> — see <see cref="GetEnumerator"/>.
    /// </summary>
    public struct Enumerator
    {
        private readonly Play _play;
        private int _index;

        internal Enumerator(in Play play)
        {
            _play = play;
            _index = -1;
        }

        /// <summary>The move at the enumerator's current position.</summary>
        public readonly Move Current => _play[_index];

        /// <summary>
        /// Advances to the next move; false once the play is exhausted.
        /// </summary>
        public bool MoveNext() => ++_index < _play.Count;
    }
}
