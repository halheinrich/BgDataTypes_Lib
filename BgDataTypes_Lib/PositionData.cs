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

    /// <summary>
    /// Whether the Jacoby rule was in force — <b>a money-game fact
    /// only</b>. Under Jacoby, gammons and backgammons count as a single
    /// point until the cube has been turned; with a centered cube that voids
    /// undoubled gammons outright and shifts the doubling window, so it can
    /// change the correct answer and participates in
    /// <see cref="ProblemKey"/> identity for money records
    /// (SPEC-stats-identity.md §1, amended 2026-08-20;
    /// halheinrich/backgammon#120).
    ///
    /// <para>
    /// <b>Three states, deliberately.</b> <see langword="null"/> means the
    /// producer did not supply the fact — not "off". Whether the record
    /// is a money game is <em>not</em> encoded here: that remains the
    /// away-scores pair (<see cref="OnRollNeeds"/> and
    /// <see cref="OpponentNeeds"/> both <c>0</c>), the single source of that
    /// truth. So the three meaningful readings are: match record (away
    /// scores non-zero) — the question does not arise and this member
    /// is ignored wherever it matters; money record with a value — the
    /// fact, which the key spells; money record with
    /// <see langword="null"/> — unknown, which is
    /// <see cref="ProblemKey"/>'s no-key rung (a money record whose Jacoby
    /// fact is missing yields no key rather than a guessed one).
    /// </para>
    ///
    /// <para>
    /// <b>Producer-stamped, never parsed back out of the XGID.</b> The
    /// converting parser (<c>ConvertXgToJson_Lib</c>) stamps this from the
    /// source record. The same information sits in bit 0 of XGID field 7
    /// (a Jacoby + 2×Beaver bitmask, never the raw value), but the XGID
    /// string is display and provenance only — it is an identity
    /// nowhere, so nothing downstream re-derives this from
    /// <see cref="BgDecisionData.Xgid"/>. A stamp on a match record is
    /// harmless and expected (XG carries the bit regardless of match play);
    /// it simply means nothing there.
    /// </para>
    /// </summary>
    public bool? IsJacoby { get; init; }
}
