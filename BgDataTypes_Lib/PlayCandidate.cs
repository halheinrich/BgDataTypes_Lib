namespace BgDataTypes_Lib;

public class PlayCandidate
{
    /// <summary>Move notation, e.g. "8/5(2) 6/3(2)".</summary>
    public string MoveNotation { get; init; } = string.Empty;

    /// <summary>Primary equity value, displayed top-right in the analysis panel.</summary>
    public double Equity { get; init; }

    /// <summary>
    /// Equity loss relative to best play, stacked below primary equity.
    /// Null for the best play itself.
    /// </summary>
    public double? EquityLoss { get; init; }

    /// <summary>True if this is the move the user played. Renders a green checkmark.</summary>
    public bool IsUserPlay { get; init; }

    // On-roll player probabilities for this candidate move. Null when not evaluated.
    public double? WinPct { get; init; }
    public double? WinGammonPct { get; init; }
    public double? WinBgPct { get; init; }
    public double? LosePct { get; init; }
    public double? LoseGammonPct { get; init; }
    public double? LoseBgPct { get; init; }
}