using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The composite decision record — one analysed backgammon decision as the
/// ecosystem's JSON wire unit, composing four orthogonal categories:
/// <see cref="Position"/> (board and match state), <see cref="Decision"/>
/// (the analysis and the user's choice), <see cref="Descriptive"/>
/// (provenance and metadata) and <see cref="Outcome"/> (after-boards).
/// Round-trips through <c>System.Text.Json</c> with no consumer-side
/// converter registration — the member types bundle their own converters.
/// Implements <see cref="IDecisionFilterData"/> by forwarding into the
/// category members; that view is a read-side derivation and is excluded
/// from JSON — the category members are the wire form
/// (halheinrich/backgammon#14).
/// </summary>
public class BgDecisionData : IDecisionFilterData
{
    /// <summary>
    /// Stable, persistent identifier for this decision within its source file.
    /// Producer-supplied at the build site (see <c>ConvertXgToJson_Lib</c>) —
    /// required so that uninitialized cases surface at construction rather than
    /// later as silent null reads. Not part of <see cref="IDecisionFilterData"/>
    /// (the filter passes records through unchanged and never needs to see the
    /// ID).
    /// </summary>
    public required DecisionId Id { get; init; }

    /// <summary>
    /// XGID position string. Lives at the top level rather than inside
    /// <see cref="Position"/> because it is a digest of the whole decision
    /// context (position, cube/match state, and the decision itself), not a
    /// property of the minimal derived <see cref="PositionData"/>. Mirrors
    /// <see cref="DecisionRow.Xgid"/>.
    /// </summary>
    public string Xgid { get; init; } = string.Empty;

    /// <summary>Board, score context and cube state at the moment of the decision.</summary>
    public PositionData    Position    { get; init; } = new();

    /// <summary>The analysis and how the user's choice scored — see <see cref="DecisionData"/>.</summary>
    public DecisionData    Decision    { get; init; } = new();

    /// <summary>Provenance and metadata: players, source file, position within the match.</summary>
    public DescriptiveData Descriptive { get; init; } = new();

    /// <summary>
    /// After-boards derived from the play choices. Producer contract: left at
    /// its default (empty boards) for cube decisions — the emptiness is not
    /// guarded here, so consumers check <see cref="IsCube"/> first.
    /// </summary>
    public PlayOutcomeData Outcome     { get; init; } = new();

    // -----------------------------------------------------------------------
    //  IDecisionFilterData
    //
    //  A derived filter view, not wire data: every member forwards into (or
    //  derives from) the category members above, which are the JSON wire
    //  form. The whole block therefore carries [JsonIgnore] — serialized, it
    //  would write top-level duplicates of the nested category data, with no
    //  read-back path (the members are get-only) and no reader
    //  (halheinrich/backgammon#14). Same rule DecisionRow applies to its
    //  derived members.
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    [JsonIgnore]
    public string Player => Descriptive.OnRollName;
    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsCube => Decision.IsCube;
    /// <inheritdoc/>
    [JsonIgnore]
    public int OnRollNeeds => Position.OnRollNeeds;
    /// <inheritdoc/>
    [JsonIgnore]
    public int OpponentNeeds => Position.OpponentNeeds;
    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsCrawford => Position.IsCrawford;
    /// <inheritdoc/>
    [JsonIgnore]
    public bool? IsJacoby => Position.IsJacoby;
    /// <inheritdoc/>
    [JsonIgnore]
    public int MatchLength => Descriptive.MatchLength;
    /// <inheritdoc/>
    [JsonIgnore]
    public int MoveNumber => Descriptive.MoveNumber;
    /// <inheritdoc/>
    [JsonIgnore]
    public bool IsStandardStart => Descriptive.IsStandardStart;
    /// <summary>
    /// Derived per the <see cref="DecisionRow.AnalysisDepth"/> convention:
    /// cube decisions report the cube analysis
    /// (<see cref="DecisionData.CubeAnalysisMode"/>); checker plays report
    /// the best-play candidate's <see cref="PlayCandidate.AnalysisMode"/>.
    /// <see cref="AnalysisMode.Unknown"/> when
    /// <see cref="DecisionData.BestPlayIndex"/> does not identify a candidate
    /// (empty <see cref="DecisionData.Plays"/>, or an out-of-range index from
    /// malformed data) — depth-not-recorded rather than a throw, since this
    /// getter runs on every filter pass and serialization.
    /// </summary>
    [JsonIgnore]
    public AnalysisMode AnalysisMode => Decision.IsCube
        ? Decision.CubeAnalysisMode
        : BestPlayCandidate?.AnalysisMode ?? AnalysisMode.Unknown;
    /// <summary>
    /// Derived from the same analysis as <see cref="AnalysisMode"/>: cube
    /// decisions report <see cref="DecisionData.CubeAnalysisLevel"/>, checker
    /// plays the best-play candidate's
    /// <see cref="PlayCandidate.AnalysisLevel"/>.
    /// <see cref="AnalysisLevel.Unknown"/> when
    /// <see cref="DecisionData.BestPlayIndex"/> does not identify a
    /// candidate.
    /// </summary>
    [JsonIgnore]
    public AnalysisLevel AnalysisLevel => Decision.IsCube
        ? Decision.CubeAnalysisLevel
        : BestPlayCandidate?.AnalysisLevel ?? AnalysisLevel.Unknown;
    /// <summary>
    /// The candidate <see cref="DecisionData.BestPlayIndex"/> identifies, or
    /// null when it identifies none — shared by the two depth-axis
    /// derivations so they always read the same candidate.
    /// </summary>
    private PlayCandidate? BestPlayCandidate =>
        Decision.BestPlayIndex >= 0 && Decision.BestPlayIndex < Decision.Plays.Count
            ? Decision.Plays[Decision.BestPlayIndex]
            : null;
    /// <summary>
    /// Forwards <see cref="DecisionData.Dice"/> in canonical unordered form
    /// (<see cref="IDecisionFilterData.Dice"/>): null for cube decisions —
    /// no dice apply — otherwise the two producer-stamped faces
    /// canonicalized by <see cref="DiceRoll"/>. Malformed stored dice (faces
    /// outside 1–6, including a checker play left at the unstamped default)
    /// fail loud in the <see cref="DiceRoll"/> constructor — so the block's
    /// <c>[JsonIgnore]</c> is load-bearing here beyond deduplication: it
    /// keeps that throwing derivation out of serialization (the
    /// <see cref="DecisionData.BestDoublerAction"/> precedent).
    /// <see cref="DecisionData.Dice"/> remains the JSON wire form.
    /// </summary>
    [JsonIgnore]
    public DiceRoll? Dice => Decision.IsCube
        ? null
        : new DiceRoll(Decision.Dice[0], Decision.Dice[1]);
    /// <inheritdoc/>
    /// <remarks>
    /// Cube decisions route to <see cref="DecisionData.UserDoubleError"/>,
    /// falling back to <see cref="DecisionData.UserTakeError"/>; checker
    /// plays to <see cref="DecisionData.UserPlayError"/>.
    /// </remarks>
    [JsonIgnore]
    public double? FilterError => Decision.IsCube
        ? Decision.UserDoubleError ?? Decision.UserTakeError
        : Decision.UserPlayError;
    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyList<int> Board => Position.Mop;
    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyList<int> AfterBestBoard => Outcome.AfterBestBoard;
    /// <inheritdoc/>
    [JsonIgnore]
    public IReadOnlyList<int> AfterPlayerBoard => Outcome.AfterPlayerBoard;

    // -----------------------------------------------------------------------
    //  Claim-layer facts that need the whole record
    //
    //  DecisionData derives the truth claim from equities alone. Whether the
    //  Too Good verdict can occur at all is a fact of the rules context —
    //  money, Jacoby, cube owner — which only this composite sees together,
    //  so it is derived here, once, and read by every consumer. Same
    //  [JsonIgnore] posture as the forwarding view: a derivation, not wire.
    // -----------------------------------------------------------------------

    /// <summary>
    /// Whether the Too Good verdict can occur at this position — the
    /// offerability fact of SPEC-scoring §3's 2026-09-02 amendment
    /// (halheinrich/backgammon#187): <see langword="false"/> exactly when the
    /// session is money (<see cref="IDecisionFilterData.IsMoneyGame"/>), the
    /// Jacoby rule is known to be in force (<see cref="IsJacoby"/> is
    /// <see langword="true"/>) and the cube is centred
    /// (<see cref="PositionData.CubeOwner"/> is
    /// <see cref="CubeOwner.Centered"/>); <see langword="true"/> otherwise.
    /// Gammons do not count under Jacoby until the cube turns, so the
    /// no-double equity never exceeds the cash there and the verdict cannot
    /// arise; a turned cube re-arms gammons, and Too Good returns.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The one derivation site of this fact in the ecosystem: a consumer
    /// that offers cube answers reads it to decide whether the Too Good pair
    /// is in the option set, and never re-derives it from the record's
    /// rules fields (the same encapsulation rule as
    /// <see cref="DecisionData.BestDoublerClaim"/>). Money is reached
    /// through the contract's single spelling of the rule, never a restated
    /// <c>MatchLength == 0</c>.
    /// </para>
    /// <para>
    /// An unknown rule is not a known Jacoby rule: <see cref="IsJacoby"/>
    /// <see langword="null"/> (a money record whose rule was never stamped,
    /// or any match record) leaves this <see langword="true"/> — the
    /// verdict is withheld only when the position's own facts rule it out,
    /// the same posture <see cref="IDecisionFilterData.IsJacoby"/> states
    /// for the filter layer. This is a fact about the position, independent
    /// of what <see cref="DecisionData.BestClaimPair"/> derives: the
    /// derivation reads equities only and would still name Too Good if the
    /// producer's numbers said so.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/> — the
    /// same guard as <see cref="DecisionData.BestClaimPair"/>; the question
    /// has no meaning on a checker play.
    /// </exception>
    [JsonIgnore]
    public bool CanBeTooGood
    {
        get
        {
            Decision.RequireCube();
            return !(((IDecisionFilterData)this).IsMoneyGame
                     && IsJacoby == true
                     && Position.CubeOwner == CubeOwner.Centered);
        }
    }
}