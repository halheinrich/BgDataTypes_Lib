using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The position-and-match-state category of a <see cref="BgDecisionData"/>:
/// the board, the cube state and the session at the moment of the decision.
/// The stored members are producer-supplied from the source file (see
/// <c>ConvertXgToJson_Lib</c>); the pip counts are derived from the board and
/// never stored (no stored copy of a derivable value). Every stored member is
/// <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>.
/// </summary>
/// <remarks>
/// <b>Money or a match is the session's kind</b> (halheinrich/backgammon#273,
/// Hal's ruling of 2026-09-26): <see cref="Session"/> is a
/// <see cref="MoneySession"/> or a <see cref="MatchSession"/>, each carrying
/// only its own facts. The away scores, the Crawford flag and the Jacoby rule
/// this category used to hold — a money record's away scores 0 and its
/// Crawford flag false as stand-ins, a match record's Jacoby fact meaningless
/// — are the kinds' now, so none can be stated where it does not apply.
/// </remarks>
public class PositionData
{
    // True while the category is read from a document (see the serializer's
    // constructor below): each rule then refuses as a JsonException.
    private readonly bool _read;

    // Null only while construction is still stating it; `required` guarantees
    // it is set by the time construction ends.
    private readonly Session? _session;

    /// <summary>Creates the category; its members are set by the initializer.</summary>
    public PositionData()
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds <paramref name="mop"/>, its
    /// first member, only because a serializer constructor must bind one; why
    /// the pattern exists is stated once, on <see cref="BgDataTypesJsonContext"/>
    /// ("The serializer constructors").
    /// </summary>
    [JsonConstructor]
    internal PositionData(BoardPosition mop)
    {
        _read = true;
        Mop = mop;
    }

    /// <summary>
    /// Men on Point — the board at the moment of the decision.
    /// <b>Frame: the player on roll's</b>, the decision-maker's:
    /// slot 0 is the opponent's bar, 1–24 the points from the on-roll
    /// player's perspective, 25 the on-roll player's bar; positive counts are
    /// the on-roll player's checkers, negative the opponent's (the
    /// <see cref="BoardPosition"/> layout, well-formed by its invariant).
    /// </summary>
    public required BoardPosition Mop { get; init; }

    /// <summary>
    /// The on-roll player's pip count, derived from <see cref="Mop"/> by the
    /// one pip rule (<see cref="BoardState.PipCount"/>'s) on each read, at no
    /// allocation. Never stored, so it cannot disagree with the board: not on
    /// the wire, and a document still stating it reads with the member
    /// ignored, as every retired member of this category does.
    /// </summary>
    [JsonIgnore]
    public int OnRollPipCount
    {
        get
        {
            Span<int> counts = stackalloc int[BoardPosition.SlotCount];
            Mop.CopyTo(counts);
            return BoardState.OnRollPips(counts);
        }
    }

    /// <summary>
    /// The opponent's pip count, derived from <see cref="Mop"/> as
    /// <see cref="OnRollPipCount"/> is (<see cref="BoardState.OpponentPipCount"/>'s rule).
    /// </summary>
    [JsonIgnore]
    public int OpponentPipCount
    {
        get
        {
            Span<int> counts = stackalloc int[BoardPosition.SlotCount];
            Mop.CopyTo(counts);
            return BoardState.OpponentPips(counts);
        }
    }

    /// <summary>
    /// Face value of the doubling cube: 1 (start), 2, 4, 8, …
    /// </summary>
    public required int CubeSize { get; init; }

    /// <summary>
    /// Who may next use the doubling cube. On-roll-relative (like
    /// <see cref="Mop"/>), not seat-relative — see <see cref="BgDataTypes_Lib.CubeOwner"/>.
    /// </summary>
    public required CubeOwner CubeOwner { get; init; }

    /// <summary>
    /// The session the decision is played in, as it stands at the decision,
    /// from the player on roll's side: a <see cref="MoneySession"/> (its rules)
    /// or a <see cref="MatchSession"/> (its length, both away scores and
    /// whether this is the Crawford game). Match on it with
    /// <see cref="Session.Match{TResult}"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown on init when the value is <see langword="null"/>.</exception>
    public required Session Session
    {
        get => _session!;
        init
        {
            try
            {
                ArgumentNullException.ThrowIfNull(value, nameof(Session));
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _session = value;
        }
    }
}
