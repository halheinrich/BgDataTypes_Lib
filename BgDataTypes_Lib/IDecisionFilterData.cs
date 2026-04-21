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
    /// Error magnitude for this decision (≥ 0).
    /// For checker plays: equity loss vs best play.
    /// For cube decisions: equity loss from doubling or take/drop decision.
    /// Null if not applicable or not recorded.
    /// </summary>
    double? FilterError { get; }

    /// <summary>
    /// Board as a 26-element array from the on-roll player's perspective.
    /// [0] = opponent bar, [1–24] = points, [25] = player bar.
    /// Positive = on-roll player's checkers; negative = opponent's.
    /// </summary>
    IReadOnlyList<int> Board { get; }

    /// <summary>
    /// Board after the best play, with POV flipped — opponent is now on roll.
    /// Same 26-element layout as <see cref="Board"/>: [0] = on-roll (opponent) bar,
    /// [1–24] = points, [25] = opponent's (decision-maker's) bar. In this POV the
    /// decision-maker's checkers are negative and the opponent's are positive.
    /// <para>
    /// Empty list for cube decisions (<see cref="IsCube"/> == true); after-boards
    /// are only meaningful for checker decisions. Consumers must check
    /// <see cref="IsCube"/> before using.
    /// </para>
    /// </summary>
    IReadOnlyList<int> AfterBestBoard { get; }

    /// <summary>
    /// Board after the player's actual play, with POV flipped — opponent is now on
    /// roll. Same layout and sign convention as <see cref="AfterBestBoard"/>.
    /// <para>
    /// Empty list for cube decisions (<see cref="IsCube"/> == true). Consumers
    /// must check <see cref="IsCube"/> before using.
    /// </para>
    /// </summary>
    IReadOnlyList<int> AfterPlayerBoard { get; }
}