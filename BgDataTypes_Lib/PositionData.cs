namespace BgDataTypes_Lib;

/// <summary>
/// The position-and-match-state category of a <see cref="BgDecisionData"/>:
/// the board, the score context, and the cube state at the moment of the
/// decision. Everything here is producer-supplied from the source file
/// (see <c>ConvertXgToJson_Lib</c>), not derived.
/// </summary>
public class PositionData
{
    /// <summary>
    /// Men on Point — 26-element board array.
    /// [0]    = opponent's bar  (value always &lt;= 0)
    /// [1-24] = points 1-24 from on-roll player's perspective
    /// [25]   = on-roll player's bar (value always &gt;= 0)
    /// Positive = on-roll player's checkers; negative = opponent's.
    /// </summary>
    public IReadOnlyList<int> Mop { get; init; } = new int[26];

    /// <summary>
    /// Away score for the player on roll — points still needed to win the
    /// match (e.g. 3 means "3-away"). 0 for money games.
    /// </summary>
    public int OnRollNeeds { get; init; }

    /// <summary>
    /// Away score for the opponent — points still needed to win the match.
    /// 0 for money games.
    /// </summary>
    public int OpponentNeeds { get; init; }

    /// <summary>
    /// On-roll player's pip count as supplied by the producing parser (XG's
    /// stored value). Distinct from <see cref="BoardState.PipCount"/>, which
    /// is computed from a live board — use this one when reading parsed
    /// decisions.
    /// </summary>
    public int OnRollPipCount { get; init; }

    /// <summary>
    /// Opponent's pip count as supplied by the producing parser (XG's stored
    /// value). Distinct from <see cref="BoardState.OpponentPipCount"/> — see
    /// <see cref="OnRollPipCount"/>.
    /// </summary>
    public int OpponentPipCount { get; init; }

    /// <summary>
    /// Face value of the doubling cube: 1 (start), 2, 4, 8, … Defaults to 1.
    /// </summary>
    public int CubeSize { get; init; } = 1;

    /// <summary>
    /// Who may next use the doubling cube. On-roll-relative (like
    /// <see cref="Mop"/>), not seat-relative — see <see cref="BgDataTypes_Lib.CubeOwner"/>.
    /// </summary>
    public CubeOwner CubeOwner { get; init; }

    /// <summary>
    /// True when this decision occurred in the Crawford game (the one game,
    /// immediately after a player reaches match point, in which doubling is
    /// barred).
    /// </summary>
    public bool IsCrawford { get; init; }
}
