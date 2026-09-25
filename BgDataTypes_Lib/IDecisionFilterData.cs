namespace BgDataTypes_Lib;

/// <summary>
/// Common filtering contract shared by <see cref="DecisionRow"/> and <see cref="BgDecisionData"/>.
/// </summary>
public interface IDecisionFilterData
{
    /// <summary>Name of the player who made the decision.</summary>
    string Player { get; }

    /// <summary>True if this is a cube decision; false if a checker play.</summary>
    bool IsCube { get; }

    /// <summary>Away score for the player on roll. 0 for money games.</summary>
    int OnRollNeeds { get; }

    /// <summary>Away score for the opponent. 0 for money games.</summary>
    int OpponentNeeds { get; }

    /// <summary>True if this is the Crawford game.</summary>
    bool IsCrawford { get; }

    /// <summary>Match length (0 = unlimited/money).</summary>
    int MatchLength { get; }

    /// <summary>
    /// True for an unlimited (money) session. This default implementation is
    /// the contract's single spelling of the money-game rule — derived from
    /// <see cref="MatchLength"/>, the same rule <see cref="IMatchInfo.IsMoneyGame"/>
    /// states at match scope. An implementation redeclares it only to surface
    /// the predicate on its concrete type (see
    /// <see cref="DecisionRow.IsMoneyGame"/>), never to change the derivation.
    /// </summary>
    bool IsMoneyGame => MatchLength == 0;

    /// <summary>
    /// Whether the Jacoby rule was in force, in the tri-state contract
    /// <see cref="PositionData.IsJacoby"/> owns and states — money records
    /// carry the fact, match records carry <see langword="null"/> because the
    /// question does not arise there, and <see langword="null"/> on a money
    /// record means the rule was never stamped.
    /// <para>
    /// Filter-layer consequence of that last rung: a money record whose fact
    /// is <see langword="null"/> matches <em>neither</em> money score token —
    /// not <c>moneyJ</c> and not <c>moneyNJ</c>. An unknown rule is never
    /// guessed into one of them. Consumers spell that conjunction themselves
    /// as <c>IsMoneyGame &amp;&amp; IsJacoby == true</c> / <c>== false</c>;
    /// the near-miss spellings <c>!= false</c> and <c>!= true</c> silently
    /// admit the unknown record into one side, which is the thing a
    /// consumer's tests must pin against.
    /// </para>
    /// </summary>
    bool? IsJacoby { get; }

    /// <summary>1-based move number within the game.</summary>
    int MoveNumber { get; }

    /// <summary>True if the game started from the canonical opening position.
    /// Move-number filtering is only meaningful when this is true; non-standard starts
    /// (custom problem positions, Bg960, etc.) automatically fail move-number filters.</summary>
    bool IsStandardStart { get; }

    /// <summary>
    /// How the analysis behind this decision was produced — the mode axis of
    /// the two-axis depth taxonomy: the cube analysis for cube decisions, the
    /// best-play candidate's analysis for checker plays (mirroring the
    /// <see cref="DecisionRow.AnalysisDepth"/> convention).
    /// <see cref="BgDataTypes_Lib.AnalysisMode.Unknown"/> when the depth was
    /// never stamped (legacy data).
    /// </summary>
    AnalysisMode AnalysisMode { get; }

    /// <summary>
    /// Evaluation level of the analysis behind this decision — the level axis
    /// paired with <see cref="AnalysisMode"/> (for rollout-family modes, the
    /// inner level), drawn from the same analysis
    /// <see cref="AnalysisMode"/> reports.
    /// <see cref="BgDataTypes_Lib.AnalysisLevel.Unknown"/> when the depth was
    /// never stamped (legacy data).
    /// </summary>
    AnalysisLevel AnalysisLevel { get; }

    /// <summary>
    /// The dice rolled for this decision, in canonical unordered form.
    /// Null for cube decisions (<see cref="IsCube"/> == true) — a cube is
    /// offered before the on-roll player rolls, so no dice apply; the
    /// null-when-inapplicable convention shared with <see cref="FilterError"/>.
    /// </summary>
    DiceRoll? Dice { get; }

    /// <summary>
    /// Error magnitude for this decision (≥ 0).
    /// For checker plays: equity loss vs best play.
    /// For cube decisions: equity loss from doubling or take/drop decision.
    /// Null if not applicable or not recorded.
    /// </summary>
    double? FilterError { get; }

    /// <summary>
    /// The board at the moment of the decision. <b>Frame: the player on
    /// roll's</b>, the decision-maker's: slot 0 is the opponent's bar, 1–24
    /// the points, 25 the on-roll player's bar; positive counts are the
    /// on-roll player's checkers, negative the opponent's
    /// (<see cref="PositionData.Mop"/>).
    /// </summary>
    BoardPosition Board { get; }

    /// <summary>
    /// The board after the best play. <b>Frame: the next mover's</b> — the
    /// position the play reaches, flipped as <see cref="BoardState.ApplyPlay"/>
    /// leaves it: the opponent is on roll, so slot 25 is the opponent's bar
    /// and their checkers are positive, while the decision-maker's checkers
    /// are negative and slot 0 is the decision-maker's bar.
    /// <para>
    /// <see langword="null"/> when absent: always for a cube decision
    /// (<see cref="IsCube"/> == true), and on a checker play whose boards the
    /// producer could not compute (<see cref="PlayOutcomeData"/>). Consumers
    /// test for <see langword="null"/>.
    /// </para>
    /// </summary>
    BoardPosition? AfterBestBoard { get; }

    /// <summary>
    /// The board after the player's actual play, in the same frame as
    /// <see cref="AfterBestBoard"/> — the next mover's.
    /// <para>
    /// <see langword="null"/> when absent, as for <see cref="AfterBestBoard"/>.
    /// </para>
    /// </summary>
    BoardPosition? AfterPlayerBoard { get; }
}