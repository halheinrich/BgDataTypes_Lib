namespace BgDataTypes_Lib;

/// <summary>
/// One analysed candidate play of a checker-play decision — one row of the
/// producing analyser's move list, carried in <see cref="DecisionData.Plays"/>.
/// Which candidate is the best or the user's play is recorded on the parent
/// (<see cref="DecisionData.BestPlayIndex"/> / <see cref="DecisionData.UserPlayIndex"/>),
/// not flagged per-candidate.
/// </summary>
public class PlayCandidate
{
    /// <summary>Move notation, e.g. "8/5(2) 6/3(2)".</summary>
    public string MoveNotation { get; init; } = string.Empty;

    /// <summary>
    /// Structural play — the sequence of (FrPt, ToPt) moves that produces
    /// this candidate. Complements <see cref="MoveNotation"/>: the notation
    /// is for display, the <see cref="Play"/> for structural comparison and
    /// downstream consumers (e.g. submitted-play grading). Empty
    /// (<c>Count == 0</c>) when not populated.
    /// </summary>
    public Play Play { get; init; }

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

    /// <summary>How this candidate's numbers were produced — the mode axis of
    /// the two-axis depth taxonomy behind the <see cref="Depth"/> /
    /// <see cref="DepthAbbreviation"/> / <see cref="DepthRank"/> display
    /// forms, used for depth filtering together with
    /// <see cref="AnalysisLevel"/>. Producer-stamped;
    /// <see cref="AnalysisMode.Unknown"/> when not set (including JSON
    /// written before the two-axis pair existed).</summary>
    public AnalysisMode AnalysisMode { get; init; }

    /// <summary>Evaluation level of the analysis behind this candidate — the
    /// level axis paired with <see cref="AnalysisMode"/>. For a rollout this
    /// is the inner moves level (checker rows never carry Roller-family
    /// rollout levels — see <see cref="BgDataTypes_Lib.AnalysisMode"/>).
    /// Producer-stamped; <see cref="AnalysisLevel.Unknown"/> when not
    /// set.</summary>
    public AnalysisLevel AnalysisLevel { get; init; }

    /// <summary>Primary equity value, displayed top-right in the analysis panel.</summary>
    public double Equity { get; init; }

    /// <summary>
    /// Equity loss vs. best-equity play, in match-equity units. <c>0.0</c> means
    /// this candidate is itself a best play — multiple candidates may share zero
    /// loss when they produce structurally equivalent (or tied-equity) positions.
    /// <see cref="DecisionData.BestPlayIndex"/> names a canonical single best
    /// when one representative is needed; <c>EquityLoss == 0.0</c> is the valid
    /// test for "is this a best play" / membership in the best-equity equivalence
    /// class. Defaults to <c>0.0</c>.
    /// </summary>
    public double EquityLoss { get; init; }

    // Outcome probabilities of this candidate, on-roll POV, fractions in
    // [0, 1] despite the Pct suffix, surfaced verbatim from XG's evaluation
    // vector. Null when the candidate was not evaluated (or the source
    // predates these fields). Win/Lose are total win/loss probabilities; the
    // gammon and backgammon fields are XG's G/B breakdown figures.

    /// <summary>Probability the on-roll player wins with this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? WinPct { get; init; }
    /// <summary>XG's gammon-win figure (the "G" of its W/G/B breakdown) for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? WinGammonPct { get; init; }
    /// <summary>XG's backgammon-win figure (the "B" of its W/G/B breakdown) for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? WinBgPct { get; init; }
    /// <summary>Probability the on-roll player loses with this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? LosePct { get; init; }
    /// <summary>XG's gammon-loss figure for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? LoseGammonPct { get; init; }
    /// <summary>XG's backgammon-loss figure for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? LoseBgPct { get; init; }
}