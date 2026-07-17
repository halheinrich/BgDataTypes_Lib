namespace BgDataTypes_Lib;

/// <summary>
/// The provenance-and-metadata category of a <see cref="BgDecisionData"/>:
/// who was playing, where the decision came from, and where it sits within
/// its match. Producer-supplied from the source file's headers (see
/// <c>ConvertXgToJson_Lib</c>).
/// </summary>
public class DescriptiveData
{
    /// <summary>Match length in points. 0 = unlimited / money session.</summary>
    public int MatchLength { get; init; }

    /// <summary>
    /// Name of the player on roll — the decision-maker this record scores.
    /// Surfaced as <see cref="IDecisionFilterData.Player"/> for filtering.
    /// Empty when the source recorded no name.
    /// </summary>
    public string OnRollName { get; init; } = string.Empty;

    /// <summary>Name of the opponent. Empty when the source recorded no name.</summary>
    public string OpponentName { get; init; } = string.Empty;

    /// <summary>Save/session title as the source file stored it. Null when none was recorded.</summary>
    public string? Title { get; init; }

    /// <summary>Match date as the source file stored it. Null when none was recorded.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>Event name (e.g. tournament) as the source file stored it. Null when none was recorded.</summary>
    public string? Event { get; init; }

    /// <summary>Originating file name including extension (e.g. "match.xg", "session.xgp"). No directory.</summary>
    public string? SourceFile { get; init; }

    /// <summary>Game number within the match (1-based).</summary>
    public int Game { get; init; }

    /// <summary>1-based move number within the game.</summary>
    public int MoveNumber { get; init; }

    /// <summary>True if the game started from the canonical opening position.
    /// False for non-standard starts (custom positions, problem setups, Bg960 variants).</summary>
    public bool IsStandardStart { get; init; }

    /// <summary>XG's per-decision comment text. Empty when none was recorded.</summary>
    public string Comment { get; init; } = string.Empty;

    /// <summary>True if the user flagged this decision in XG (the "flag" marker).</summary>
    public bool Flagged { get; init; }
}
