namespace BgDataTypes_Lib;

public class PlayCandidate
{
    /// <summary>Move notation, e.g. "8/5(2) 6/3(2)".</summary>
    public string MoveNotation { get; init; } = string.Empty;

    /// <summary>Analysis depth label for this candidate, e.g. "3-ply",
    /// "XG Roller++", "Rollout: 1296 trials. 3-ply". Rendered in the
    /// Depth column of the move-decision play panel. Empty when not set.</summary>
    public string Depth { get; init; } = string.Empty;

    /// <summary>Compact display form of the analysis depth, e.g.
    /// "3-ply", "R++", "3p1296". Rendered in the Depth column of the
    /// move-decision play panel. Empty when not set.</summary>
    public string DepthAbbreviation { get; init; } = string.Empty;

    /// <summary>Ordinal ranking of the analysis depth; higher = deeper /
    /// more rigorous. Semantics (category boundaries, rollout-vs-static
    /// ordering) are defined by the producer — see ConvertXgToJson_Lib's
    /// depth-resolution logic. Used by BackgammonDiagram_Lib to flag
    /// out-of-order analysis depths across sorted-by-equity plays.
    /// Defaults to 0 (treated as lowest).</summary>
    public int DepthRank { get; init; }

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