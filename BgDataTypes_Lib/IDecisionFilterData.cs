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
}