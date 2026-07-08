namespace BgDataTypes_Lib;

public class DescriptiveData
{
    public int MatchLength { get; init; }
    public string OnRollName { get; init; } = string.Empty;
    public string OpponentName { get; init; } = string.Empty;
    public string? Title { get; init; }
    public DateOnly? Date { get; init; }
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