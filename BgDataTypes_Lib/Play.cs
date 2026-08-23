using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A complete play: the sequence of moves for one turn.
/// Uses a fixed-size buffer (max 4 moves for doubles) to avoid heap allocation.
///
/// Construct with <see cref="Create"/> or, equivalently, a collection
/// expression — <c>Play play = [new(13, 10), new(10, 8)];</c> — when the
/// moves are known up front; <c>[]</c> is the empty play, a forced pass.
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
    /// the intent-level construction path for moves known up front:
    /// <c>Play.Create(new(13, 10), new(10, 8))</c>, or equivalently the
    /// collection expression <c>[new(13, 10), new(10, 8)]</c> (this method is
    /// the type's <see cref="CollectionBuilderAttribute"/> target). No
    /// arguments — <c>Play.Create()</c> / <c>[]</c> — is the empty play,
    /// a forced pass. A <em>single</em>-move call must spell the element
    /// type — <c>Play.Create(new Move(13, 7))</c> — or use a collection
    /// expression: one lone target-typed <c>new(…)</c> argument binds in
    /// normal form to the span parameter itself and fails to compile (a
    /// dedicated <c>Create(Move)</c> overload cannot fix this — a typeless
    /// <c>new(…)</c> is ambiguous between the two).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="moves"/> holds more than 4 moves (the doubles maximum).
    /// </exception>
    public static Play Create(params ReadOnlySpan<Move> moves)
    {
        if (moves.Length > 4)
            throw new ArgumentException(
                $"A play has at most 4 moves, got {moves.Length}.", nameof(moves));

        var play = new Play();
        foreach (var move in moves)
            play.Add(move);
        return play;
    }

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
        switch (Count)
        {
            case 0: _m0 = move; break;
            case 1: _m1 = move; break;
            case 2: _m2 = move; break;
            case 3: _m3 = move; break;
            default: throw new InvalidOperationException("Play already has 4 moves");
        }
        Count++;
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
