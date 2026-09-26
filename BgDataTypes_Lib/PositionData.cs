using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The position-and-match-state category of a <see cref="BgDecisionData"/>:
/// the board, the score context, and the cube state at the moment of the
/// decision. The stored members are producer-supplied from the source file
/// (see <c>ConvertXgToJson_Lib</c>); the pip counts are derived from the
/// board and never stored (no stored copy of a derivable value). Every
/// stored member but the nullable <see cref="IsJacoby"/> is <c>required</c>,
/// per the wire rule stated on <see cref="BgDataTypesJsonContext"/>.
/// </summary>
public class PositionData
{
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
    /// Away score for the player on roll — points still needed to win the
    /// match (e.g. 3 means "3-away"). 0 for money games.
    /// </summary>
    public required int OnRollNeeds { get; init; }

    /// <summary>
    /// Away score for the opponent — points still needed to win the match.
    /// 0 for money games.
    /// </summary>
    public required int OpponentNeeds { get; init; }

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
    /// True when this decision occurred in the Crawford game (the one game,
    /// immediately after a player reaches match point, in which doubling is
    /// barred).
    /// </summary>
    public required bool IsCrawford { get; init; }

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
    /// fact is not carried — because it does not apply (a match record) or
    /// because the producer did not supply it — never "off". Whether the record
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
    /// <see cref="BgDecisionData.Xgid"/>.
    /// </para>
    ///
    /// <para>
    /// <b>What a producer stamps.</b> A money record carries the value; a
    /// match record carries <see langword="null"/>, because the fact does
    /// not apply there — the producer stamps XG's field-7 bit onto money
    /// records only, rather than passing it through on every record
    /// (<c>ConvertXgToJson_Lib</c>'s <c>MatchContext.JacobyStamp</c>). A
    /// non-null value on a match record is nonetheless tolerated, not
    /// rejected: <see cref="ProblemKey.TryDerive"/> ignores it and the
    /// match key is unaffected, so a record from a laxer producer still
    /// gets its key.
    /// </para>
    /// </summary>
    public bool? IsJacoby { get; init; }
}
