namespace BgDataTypes_Lib;

/// <summary>
/// Game states derived from the play choices of a decision: the board after
/// the best play and the board after the player's actual play.
///
/// <para>
/// Both boards use the 26-element layout of <see cref="PositionData.Mop"/>,
/// but with POV flipped — the opponent is on roll after a play, so the
/// decision-maker's checkers are negative in these arrays and the opponent's
/// are positive.
/// </para>
///
/// <para>
/// For cube decisions no play is made; both lists are empty. Consumers must
/// check <see cref="DecisionData.IsCube"/> before interpreting these boards.
/// </para>
/// </summary>
public class PlayOutcomeData
{
    /// <summary>Board after the best play. Empty for cube decisions.</summary>
    public IReadOnlyList<int> AfterBestBoard { get; init; } = [];

    /// <summary>Board after the player's actual play. Empty for cube decisions.</summary>
    public IReadOnlyList<int> AfterPlayerBoard { get; init; } = [];
}
