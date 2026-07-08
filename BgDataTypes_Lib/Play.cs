using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A complete play: the sequence of moves for one turn.
/// Uses a fixed-size buffer (max 4 moves for doubles) to avoid heap allocation.
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
public struct Play : IEquatable<Play>
{
    // Fixed buffer: max 4 moves (doubles)
    private Move _m0, _m1, _m2, _m3;
    public int Count { get; private set; }

    public readonly Move this[int index] => index switch
    {
        0 => _m0,
        1 => _m1,
        2 => _m2,
        3 => _m3,
        _ => throw new IndexOutOfRangeException()
    };

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

    public void RemoveLast()
    {
        if (Count == 0) throw new InvalidOperationException("Play is empty");
        Count--;
    }

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

    public readonly bool Equals(Play other) => ToCanonical().Equals(other.ToCanonical());
    public override readonly bool Equals(object? obj) => obj is Play p && Equals(p);
    public override readonly int GetHashCode() => ToCanonical().GetHashCode();

    public static bool operator ==(Play left, Play right) => left.Equals(right);
    public static bool operator !=(Play left, Play right) => !left.Equals(right);
}
