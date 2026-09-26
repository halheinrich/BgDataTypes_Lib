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
/// (halheinrich/backgammon#14). Every stored member is <c>required</c>, per
/// the wire rule stated on <see cref="BgDataTypesJsonContext"/>.
///
/// <para>
/// <b>The Crawford rule binds the record</b> (halheinrich/backgammon#201).
/// Doubling is prohibited in the Crawford game, so a cube decision
/// (<see cref="DecisionData.IsCube"/>) in a Crawford position
/// (<see cref="PositionData.IsCrawford"/>) cannot exist, and the record
/// cannot be constructed: the <see cref="Position"/> and
/// <see cref="Decision"/> init setters each check the other half, so
/// whichever is set second throws <see cref="ArgumentException"/> naming
/// itself — from an object initializer in either member order and from a
/// JSON document in either property order alike, since
/// <c>System.Text.Json</c> populates init setters. The half set first never
/// throws: until it is set, the other half counts as neither cube nor
/// Crawford.
/// <see cref="ProblemKey"/>'s grammar still accepts a Crawford cube key,
/// because stats documents written before this guard hold such keys and
/// must keep loading; those keys are inert rather than orphaned — stats are
/// looked up per pooled problem (<c>BgGame_Lib</c>'s
/// <c>MixedProblemSetSource</c>), and nothing but the document writer's
/// ordering walks the whole document.
/// </para>
/// </summary>
public class BgDecisionData : IDecisionFilterData
{
    // Null only until the half is set; `required` guarantees neither is null
    // once construction ends (see the comment above Position).
    private readonly PositionData? _position;
    private readonly DecisionData? _decision;

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
    public required string Xgid { get; init; }

    // Both halves keep their non-nullable declaration honest on every path.
    // They are required, so an absent half is a compile error in an object
    // initializer and a JsonException on the wire, through the reflection
    // path and the source-generated context alike
    // (halheinrich/backgammon#222); and an explicit null — an
    // initializer's, or a JSON document's `"Position":null` — is rejected
    // at init with ArgumentNullException (halheinrich/backgammon#221). The
    // backing fields are therefore null only while construction is still
    // setting them: the Crawford guard below reads an unset other half as
    // "not cube, not Crawford", and `required` guarantees both are set by
    // the time construction ends.

    /// <summary>
    /// Board, score context and cube state at the moment of the decision.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown on init when the value is <see langword="null"/> — the member
    /// is declared non-nullable and keeps it; a JSON <c>null</c> for it is
    /// a malformed document, not a record without a position
    /// (halheinrich/backgammon#221).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the incoming position is Crawford and the
    /// already-set <see cref="Decision"/> is a cube decision — the Crawford
    /// rule, see the class summary (<see cref="CrawfordRule"/> is the one
    /// spelling of the rule).
    /// </exception>
    public required PositionData Position
    {
        get => _position!;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Position));
            CrawfordRule.ThrowIfCrawfordCube(value.IsCrawford, _decision?.IsCube == true, nameof(Position));
            _position = value;
        }
    }

    /// <summary>
    /// The analysis and how the user's choice scored — see
    /// <see cref="DecisionData"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown on init when the value is <see langword="null"/> — the member
    /// is declared non-nullable and keeps it; a JSON <c>null</c> for it is
    /// a malformed document, not a record without a decision
    /// (halheinrich/backgammon#221).
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the incoming decision is a cube decision and the
    /// already-set <see cref="Position"/> is Crawford — the Crawford rule,
    /// see the class summary (<see cref="CrawfordRule"/> is the one spelling
    /// of the rule).
    /// </exception>
    public required DecisionData Decision
    {
        get => _decision!;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Decision));
            CrawfordRule.ThrowIfCrawfordCube(_position?.IsCrawford == true, value.IsCube, nameof(Decision));
            _decision = value;
        }
    }

    /// <summary>Provenance and metadata: players, source file, position within the match.</summary>
    public required DescriptiveData Descriptive { get; init; }

    /// <summary>
    /// After-boards derived from the play choices. Producer contract: both
    /// boards <see langword="null"/> for cube decisions — not guarded here;
    /// consumers test each board for <see langword="null"/> (see
    /// <see cref="PlayOutcomeData"/>).
    /// </summary>
    public required PlayOutcomeData Outcome { get; init; }

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
    /// <summary>
    /// The 1-based number of the game this decision was played in, within
    /// its source file; <see langword="null"/> for a decision in a standalone
    /// position (an <c>.xgp</c> file), which belongs to no game — not a
    /// stamped 1 (halheinrich/backgammon#124). Derived from
    /// <see cref="Id"/>, the one place it is stored
    /// (<see cref="XgDecisionId.Game"/>), so it cannot disagree with the
    /// identifier.
    /// </summary>
    [JsonIgnore]
    public int? Game => Id.GameInFile;
    /// <inheritdoc/>
    /// <remarks>
    /// Derived from <see cref="Id"/>, as <see cref="Game"/> is
    /// (<see cref="XgDecisionId.MoveNumber"/>).
    /// </remarks>
    [JsonIgnore]
    public int? MoveNumber => Id.MoveInGame;
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
    public BoardPosition Board => Position.Mop;
    /// <inheritdoc/>
    [JsonIgnore]
    public BoardPosition? AfterBestBoard => Outcome.AfterBestBoard;
    /// <inheritdoc/>
    [JsonIgnore]
    public BoardPosition? AfterPlayerBoard => Outcome.AfterPlayerBoard;

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