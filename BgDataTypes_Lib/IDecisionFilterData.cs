namespace BgDataTypes_Lib;

/// <summary>
/// Common filtering contract shared by <see cref="DecisionRow"/> and the
/// view of a <see cref="BgDecisionData"/> (<see cref="BgDecisionData.ViewFor"/>):
/// the decision's <see cref="Kind"/>, the members every kind has, and the
/// checker play's own members, which are <see langword="null"/> for a cube
/// decision — the one kind without them. No member holds a value standing
/// for "not applicable" (halheinrich/backgammon#273): where a fact does not
/// apply, the member is <see langword="null"/> and its documentation says so.
/// </summary>
/// <remarks>
/// <b>A view is built for one ranking</b> (SPEC-scoring §2a,
/// halheinrich/backgammon#282). The members that say best or error —
/// <see cref="PlayerResult"/>, <see cref="AnalysisMode"/>,
/// <see cref="AnalysisLevel"/>, <see cref="AfterBestBoard"/> — are
/// <see cref="Ranking"/>'s: a record's view derives them for the ranking it
/// was built for, and a row carries the values of the ranking it was built
/// for. So "erred by more than x" is expressed under either ranking by
/// filtering views built for it. A cube decision's members do not depend on
/// the ranking.
/// </remarks>
public interface IDecisionFilterData
{
    /// <summary>
    /// The ranking this view's best and error members are derived under
    /// (<see cref="PlayRanking"/>). A cube decision's members do not depend on
    /// it.
    /// </summary>
    PlayRanking Ranking { get; }

    /// <summary>
    /// The decision's kind: a checker play or a cube decision. For a record,
    /// the value form of its type (<see cref="BgDecisionData.Kind"/>); for a
    /// row, its stored column. Where the record itself is at hand, match on it
    /// exhaustively with <see cref="BgDecisionData.Match{TResult}"/>.
    /// </summary>
    DecisionKind Kind { get; }

    /// <summary>
    /// Name of the player who made the decision; <see langword="null"/> when
    /// the source recorded no name — never empty text, the one spelling of
    /// "none recorded" (<see cref="DescriptiveData.OnRollName"/>).
    /// </summary>
    string? Player { get; }

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

    /// <summary>
    /// 1-based move number within the game; <see langword="null"/> for a
    /// decision in a standalone position, which belongs to no game — "no move
    /// number", never move 1 (halheinrich/backgammon#124).
    /// </summary>
    int? MoveNumber { get; }

    /// <summary>
    /// Whether the game started from the canonical opening position;
    /// <see langword="null"/> for a decision in a standalone position, which
    /// belongs to no game (halheinrich/backgammon#124). Move-number filtering
    /// is only meaningful when this is true; non-standard starts (custom
    /// problem positions, Bg960, etc.) and standalone positions have no move
    /// number to filter.
    /// </summary>
    bool? IsStandardStart { get; }

    /// <summary>
    /// How the analysis behind this decision was produced — the mode axis of
    /// the two-axis depth taxonomy: the cube analysis for a cube decision, the
    /// best play's analysis under <see cref="Ranking"/> for a checker play
    /// (mirroring the <see cref="DecisionRow.AnalysisDepth"/> convention).
    /// <see cref="BgDataTypes_Lib.AnalysisMode.Unknown"/> when the producer did
    /// not record it.
    /// </summary>
    AnalysisMode AnalysisMode { get; }

    /// <summary>
    /// Evaluation level of the analysis behind this decision — the level axis
    /// paired with <see cref="AnalysisMode"/> (for rollout-family modes, the
    /// inner level), drawn from the same analysis
    /// <see cref="AnalysisMode"/> reports.
    /// <see cref="BgDataTypes_Lib.AnalysisLevel.Unknown"/> when the producer
    /// did not record it.
    /// </summary>
    AnalysisLevel AnalysisLevel { get; }

    /// <summary>
    /// The player's result on this decision, each case by name
    /// (<see cref="BgDataTypes_Lib.PlayerResult"/>). For a checker play, the
    /// player's result under <see cref="Ranking"/>
    /// (<see cref="RankedPlays.PlayerResult"/>): scored with its error, not
    /// scored (no error), unstated with the analyser's error, or not recorded.
    /// For a cube decision, the doubler's result or, when the record holds
    /// none, the taker's: a stated action is scored with its error
    /// (<see cref="CubeDecisionData.UserDoubleError"/>,
    /// <see cref="CubeDecisionData.UserTakeError"/>), an unstated one with an
    /// analyser's error is unstated; a cube decision is never not scored. The
    /// filter's "erred by more than x" is
    /// <c>PlayerResult.TryGetError(out var error) &amp;&amp; error &gt; x</c>.
    /// </summary>
    PlayerResult PlayerResult { get; }

    /// <summary>
    /// The board at the moment of the decision. <b>Frame: the player on
    /// roll's</b>, the decision-maker's: slot 0 is the opponent's bar, 1–24
    /// the points, 25 the on-roll player's bar; positive counts are the
    /// on-roll player's checkers, negative the opponent's
    /// (<see cref="PositionData.Mop"/>).
    /// </summary>
    BoardPosition Board { get; }

    // -----------------------------------------------------------------------
    //  The checker play's own members: null for a cube decision
    // -----------------------------------------------------------------------

    /// <summary>
    /// The dice rolled for a checker play, in canonical unordered form;
    /// <see langword="null"/> for a cube decision — a cube is offered before
    /// the on-roll player rolls, so no dice apply. Never null for a checker
    /// play (<see cref="CheckerPlayDecision.Dice"/>).
    /// </summary>
    DiceRoll? Dice { get; }

    /// <summary>
    /// The board a checker play's best play under <see cref="Ranking"/>
    /// leaves. <b>Frame: the next mover's</b> — the position the play
    /// reaches, flipped as <see cref="BoardState.ApplyPlay"/> leaves it: the
    /// opponent is on roll, so slot 25 is the opponent's bar and their
    /// checkers are positive, while the decision-maker's checkers are negative
    /// and slot 0 is the decision-maker's bar. Never null for a checker play
    /// (<see cref="CheckerPlayDecision.AfterBoardOfBest"/>);
    /// <see langword="null"/> for a cube decision, where no play is made.
    /// </summary>
    BoardPosition? AfterBestBoard { get; }

    /// <summary>
    /// The board the user's checker play leaves, in the same frame as
    /// <see cref="AfterBestBoard"/> — the next mover's.
    /// <see langword="null"/> when the user's play is not among the
    /// candidates (<see cref="CheckerPlayDecision.AfterPlayerBoard"/>), and
    /// for a cube decision.
    /// </summary>
    BoardPosition? AfterPlayerBoard { get; }
}
