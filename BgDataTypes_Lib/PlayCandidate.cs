using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// One analysed candidate play of a checker-play decision — one row of the
/// producing analyser's move list, carried in <see cref="CheckerPlayDecisionData.Plays"/>.
/// Which candidate is the best or the user's play is recorded on the parent
/// (<see cref="CheckerPlayDecisionData.BestPlayIndex"/> / <see cref="CheckerPlayDecisionData.UserPlayIndex"/>),
/// not flagged per-candidate, and so is what a candidate gives up against the
/// best (<see cref="CheckerPlayDecisionData.EquityLoss"/>), which needs the
/// other candidates. The nullable probabilities' <see langword="null"/> means
/// the candidate was not evaluated; every other stored member is
/// <c>required</c>, per the wire rule stated on
/// <see cref="BgDataTypesJsonContext"/>.
/// </summary>
/// <remarks>
/// A document still stating a retired member — <c>MoveNotation</c>, derived
/// from <see cref="Play"/>; <c>EquityLoss</c>, derived on the parent;
/// <c>DepthRank</c>, derived from the depth taxonomy — reads with it ignored,
/// as every member this category does not have is.
/// </remarks>
public class PlayCandidate
{
    // True while the candidate is read from a document (see the serializer's
    // constructor below): each rule then refuses as a JsonException.
    private readonly bool _read;
    private readonly double _equity;

    /// <summary>Creates a candidate; its members are set by the initializer.</summary>
    public PlayCandidate()
    {
    }

    /// <summary>
    /// The serializer's constructor, for a candidate read from a document: it
    /// marks the candidate as read before any member is set, so every rule
    /// refuses a breach as a <see cref="System.Text.Json.JsonException"/> (the
    /// wire rule on <see cref="BgDataTypesJsonContext"/>). It takes
    /// <paramref name="play"/> only because a serializer constructor must bind
    /// a member.
    /// </summary>
    [JsonConstructor]
    internal PlayCandidate(Play play)
    {
        _read = true;
        Play = play;
    }

    /// <summary>
    /// The candidate's play — the sequence of (FrPt, ToPt) moves that
    /// produces it, and the one stored form of it. It is applied and matched
    /// (submitted-play grading finds a submitted play among the candidates
    /// with <see cref="BoardState.IndexOfSamePlay"/> from the decision's
    /// position), and it is displayed through <see cref="Notation"/>, which
    /// is derived from it.
    /// </summary>
    public required Play Play { get; init; }

    /// <summary>
    /// The candidate's play in standard notation, e.g. <c>"8/5(2) 6/3(2)"</c>:
    /// the <see cref="Play"/>'s own <see cref="Play.ToNotation"/>, so it can
    /// never disagree with the play. Derived on each read and never stored
    /// (halheinrich/backgammon#273). The empty string for a pass.
    /// </summary>
    [JsonIgnore]
    public string Notation => Play.ToNotation();

    /// <summary>Analysis depth label for this candidate, e.g. "3-ply",
    /// "XG Roller++", "Rollout: 1296 trials. 3-ply". Rendered in the
    /// Depth column of the move-decision play panel. Empty when the producer
    /// recorded no label.</summary>
    public required string Depth { get; init; }

    /// <summary>Compact display form of the analysis depth, e.g.
    /// "3-ply", "R++", "3p1296". Rendered in the Depth column of the
    /// move-decision play panel. Empty when the producer recorded no
    /// label.</summary>
    public required string DepthAbbreviation { get; init; }

    /// <summary>
    /// Ordinal ranking of the analysis depth; higher = deeper / more
    /// rigorous, and only the ordering means anything. Derived from
    /// <see cref="AnalysisMode"/> and <see cref="AnalysisLevel"/>, which
    /// determine it, so never stored (the grid is stated on the internal
    /// <c>AnalysisDepthRank</c>). Used by BackgammonDiagram_Lib to flag
    /// out-of-order analysis depths across sorted-by-equity plays. 0 is the
    /// floor: the mode, or an evaluation's level, not recorded.
    /// </summary>
    [JsonIgnore]
    public int DepthRank => AnalysisDepthRank.Of(AnalysisMode, AnalysisLevel);

    /// <summary>How this candidate's numbers were produced — the mode axis of
    /// the two-axis depth taxonomy behind the <see cref="Depth"/> /
    /// <see cref="DepthAbbreviation"/> / <see cref="DepthRank"/> display
    /// forms, used for depth filtering together with
    /// <see cref="AnalysisLevel"/>. Producer-stamped;
    /// <see cref="AnalysisMode.Unknown"/> when the producer did not record
    /// it.</summary>
    public required AnalysisMode AnalysisMode { get; init; }

    /// <summary>Evaluation level of the analysis behind this candidate — the
    /// level axis paired with <see cref="AnalysisMode"/>. For a rollout this
    /// is the inner moves level (checker rows never carry Roller-family
    /// rollout levels — see <see cref="BgDataTypes_Lib.AnalysisMode"/>).
    /// Producer-stamped; <see cref="AnalysisLevel.Unknown"/> when the
    /// producer did not record it.</summary>
    public required AnalysisLevel AnalysisLevel { get; init; }

    /// <summary>
    /// Primary equity value, displayed top-right in the analysis panel. A
    /// finite number: the best play is the candidate of the highest equity
    /// (<see cref="CheckerPlayDecisionData.BestPlayIndex"/>), which a
    /// non-number would leave undefined.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown on init when the value is not finite.</exception>
    public required double Equity
    {
        get => _equity;
        init
        {
            try
            {
                if (!double.IsFinite(value))
                    throw new ArgumentOutOfRangeException(nameof(Equity), value, "A candidate's equity is a finite number.");
            }
            catch (ArgumentException fault) when (_read)
            {
                throw DocumentRefusal.Of(fault);
            }
            _equity = value;
        }
    }

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
