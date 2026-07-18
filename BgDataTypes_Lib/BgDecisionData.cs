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
/// category members.
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
    // -----------------------------------------------------------------------

    /// <inheritdoc/>
    public string Player => Descriptive.OnRollName;
    /// <inheritdoc/>
    public bool IsCube => Decision.IsCube;
    /// <inheritdoc/>
    public int OnRollNeeds => Position.OnRollNeeds;
    /// <inheritdoc/>
    public int OpponentNeeds => Position.OpponentNeeds;
    /// <inheritdoc/>
    public bool IsCrawford => Position.IsCrawford;
    /// <inheritdoc/>
    public int MatchLength => Descriptive.MatchLength;
    /// <inheritdoc/>
    public int MoveNumber => Descriptive.MoveNumber;
    /// <inheritdoc/>
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
    /// <inheritdoc/>
    /// <remarks>
    /// Cube decisions route to <see cref="DecisionData.UserDoubleError"/>,
    /// falling back to <see cref="DecisionData.UserTakeError"/>; checker
    /// plays to <see cref="DecisionData.UserPlayError"/>.
    /// </remarks>
    public double? FilterError => Decision.IsCube
        ? Decision.UserDoubleError ?? Decision.UserTakeError
        : Decision.UserPlayError;
    /// <inheritdoc/>
    public IReadOnlyList<int> Board => Position.Mop;
    /// <inheritdoc/>
    public IReadOnlyList<int> AfterBestBoard => Outcome.AfterBestBoard;
    /// <inheritdoc/>
    public IReadOnlyList<int> AfterPlayerBoard => Outcome.AfterPlayerBoard;
}