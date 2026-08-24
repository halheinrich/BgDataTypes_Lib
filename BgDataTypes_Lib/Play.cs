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
/// Equality is notation-level, not encoding-level: two plays are equal iff
/// their canonical chain forms (<see cref="ToCanonical"/>) are equal —
/// insensitive to move order and to how a checker's trajectory is decomposed
/// into single-die hops, but fully sensitive to hits. See
/// <see cref="CanonicalPlay"/> for the collapse semantics.
///
/// Serialised as a JSON array of <see cref="Move"/> via <see cref="PlayJsonConverter"/>;
/// the raw move sequence round-trips exactly — canonicalization affects
/// equality, never storage. The private buffer fields and the
/// <see cref="Count"/> setter are not exposed to the default property-based
/// serialiser.
/// </summary>
[JsonConverter(typeof(PlayJsonConverter))]
[CollectionBuilder(typeof(Play), nameof(Create))]
public struct Play : IEquatable<Play>
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
    /// The canonical chain form of this play — the single source of play
    /// equivalence (see <see cref="CanonicalPlay"/>). <see cref="Equals(Play)"/>
    /// and <see cref="GetHashCode"/> delegate here; a caller comparing one play
    /// against many should hoist its canonical form out of the loop.
    /// </summary>
    public readonly CanonicalPlay ToCanonical() => CanonicalPlay.FromPlay(in this);

    /// <summary>
    /// Canonical (notation-level) equivalence — delegates to
    /// <see cref="ToCanonical"/>; see the type summary for what compares equal.
    /// </summary>
    public readonly bool Equals(Play other) => ToCanonical().Equals(other.ToCanonical());
    /// <inheritdoc cref="Equals(Play)"/>
    public override readonly bool Equals(object? obj) => obj is Play p && Equals(p);
    /// <summary>Hash of the canonical form, consistent with <see cref="Equals(Play)"/>.</summary>
    public override readonly int GetHashCode() => ToCanonical().GetHashCode();

    /// <inheritdoc cref="Equals(Play)"/>
    public static bool operator ==(Play left, Play right) => left.Equals(right);
    /// <summary>Negation of <see cref="op_Equality"/>.</summary>
    public static bool operator !=(Play left, Play right) => !left.Equals(right);

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
