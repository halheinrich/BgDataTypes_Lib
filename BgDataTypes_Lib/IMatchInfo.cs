namespace BgDataTypes_Lib;

/// <summary>
/// Match-level metadata contract — the shape a consumer needs to decide
/// whether to skip an entire match before any of its decisions are produced.
/// Parallel in spirit to <see cref="IDecisionFilterData"/>: producers (e.g.
/// the XG parser's match-info type) implement it, and filter layers consume
/// it without referencing any producer's concrete types. Minimal by design —
/// members are added on demand, not mirrored wholesale from a producer.
/// See <see cref="IGameInfo"/> for the game-scope companion.
/// </summary>
public interface IMatchInfo
{
    /// <summary>Name of player 1 (bottom player in XG).</summary>
    string Player1 { get; }

    /// <summary>Name of player 2 (top player in XG).</summary>
    string Player2 { get; }

    /// <summary>
    /// Match length (points to win). 0 = unlimited / money session.
    /// Producer contract: raw sentinel lengths (XG stores unlimited sessions
    /// as 99999) are normalized to 0 at the parse boundary, before this
    /// contract ever sees them — implementations never surface the sentinel.
    /// </summary>
    int MatchLength { get; }

    /// <summary>
    /// True for an unlimited (money) session. This default implementation is
    /// the contract's single spelling of the money-game rule — derived from
    /// <see cref="MatchLength"/> (valid because of the sentinel normalization
    /// documented there). Implementers inherit it rather than restating the
    /// rule, and must never redeclare it with a different derivation.
    /// </summary>
    bool IsMoneyGame => MatchLength == 0;
}
