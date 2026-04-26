namespace BgDataTypes_Lib;

public class DescriptiveData
{
    public int MatchLength { get; init; }
    public string OnRollName { get; init; } = string.Empty;
    public string OpponentName { get; init; } = string.Empty;
    public string? Title { get; init; }
    public DateOnly? Date { get; init; }
    public string? Event { get; init; }
    public string? SourceFile { get; init; }

    /// <summary>1-based move number within the game.</summary>
    public int MoveNumber { get; init; }

    /// <summary>True if the game started from the canonical opening position.
    /// False for non-standard starts (custom positions, problem setups, Bg960 variants).</summary>
    public bool IsStandardStart { get; init; }
}