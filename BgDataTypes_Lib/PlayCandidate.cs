using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// One analysed candidate play of a checker-play decision — one row of the
/// producing analyser's move list, carried in <see cref="CheckerPlayDecisionData.Plays"/>.
/// Which candidate is the user's play is recorded on the parent
/// (<see cref="CheckerPlayDecisionData.UserPlayIndex"/>), not flagged
/// per-candidate. Which is the best, and what each gives up against it, a
/// ranking decides over all the candidates
/// (<see cref="CheckerPlayDecisionData.RankedBy"/>), so neither is a
/// candidate's own fact. The nullable probabilities' <see langword="null"/> means
/// the candidate was not evaluated, and each nullable depth fact's that none
/// was recorded; every other stored member is <c>required</c>, per the wire
/// rule stated on <see cref="BgDataTypesJsonContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>The depth is typed facts.</b> The record stores the mode, the level, the
/// rollout trial count, the book edition and an unrecognized level's raw
/// code; the label, the abbreviation and the rank are derived from them
/// (<see cref="Depth"/>, <see cref="DepthAbbreviation"/>, <see cref="DepthRank"/>).
/// </para>
/// <para>
/// A document still stating a retired member — <c>MoveNotation</c>, derived
/// from <see cref="Play"/>; <c>EquityLoss</c>, derived on the parent;
/// <c>DepthRank</c>, <c>Depth</c>, <c>DepthAbbreviation</c> and
/// <c>LosePct</c>, derived here — reads with it ignored, as every member this
/// category does not have is.
/// </para>
/// </remarks>
public class PlayCandidate
{
    // True while the candidate is read from a document (see the serializer's
    // constructor below): each rule then refuses as a JsonException.
    private readonly bool _read;
    private readonly double _equity;

    // The typed depth facts; the mode and level null only while construction
    // is still stating them (`required` guarantees both by the end), so each
    // setter holds the facts stated so far to the taxonomy's rules.
    private readonly AnalysisMode? _mode;
    private readonly AnalysisLevel? _level;
    private readonly int? _rolloutTrials;
    private readonly BookEdition? _bookEdition;
    private readonly int? _unrecognizedLevelCode;

    /// <summary>Creates a candidate; its members are set by the initializer.</summary>
    public PlayCandidate()
    {
    }

    /// <summary>
    /// The serializer's constructor. It binds <paramref name="play"/>, its
    /// first member, only because a serializer constructor must bind one; why
    /// the pattern exists is stated once, on <see cref="BgDataTypesJsonContext"/>
    /// ("The serializer constructors").
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

    /// <summary>
    /// Analysis depth label for this candidate, e.g. "3-ply", "XG Roller++",
    /// "Rollout: 1296 trials. 3-ply", "Book V2". Rendered in the Depth column
    /// of the move-decision play panel. Derived from the typed depth facts —
    /// <see cref="AnalysisMode"/>, <see cref="AnalysisLevel"/>,
    /// <see cref="RolloutTrials"/>, <see cref="BookEdition"/>,
    /// <see cref="UnrecognizedLevelCode"/> — and never stored (the grammar is
    /// stated on the internal <c>DepthTaxonomy</c>). <see langword="null"/>
    /// when no depth is recorded; never empty.
    /// </summary>
    [JsonIgnore]
    public string? Depth => DepthTaxonomy.Label(
        AnalysisMode, AnalysisLevel, RolloutTrials, BookEdition, UnrecognizedLevelCode);

    /// <summary>
    /// Compact display form of the analysis depth, e.g. "3-ply", "R++",
    /// "3p1296", "B4_12960". Rendered in the Depth column of the move-decision
    /// play panel. Derived from the same facts as <see cref="Depth"/>, never
    /// stored; <see langword="null"/> when no depth is recorded.
    /// </summary>
    [JsonIgnore]
    public string? DepthAbbreviation => DepthTaxonomy.Abbreviation(
        AnalysisMode, AnalysisLevel, RolloutTrials, UnrecognizedLevelCode);

    /// <summary>
    /// Ordinal ranking of the analysis depth; higher = deeper / more
    /// rigorous, and only the ordering means anything. Derived from
    /// <see cref="AnalysisMode"/> and <see cref="AnalysisLevel"/>, which
    /// determine it, so never stored (the grid is stated on the internal
    /// <c>DepthTaxonomy</c>). <see langword="null"/> when the
    /// depth is not recorded — the mode <see cref="AnalysisMode.Unknown"/>,
    /// or an evaluation's level <see cref="AnalysisLevel.Unknown"/> — never a
    /// floor rank standing for it.
    /// </summary>
    [JsonIgnore]
    public int? DepthRank => DepthTaxonomy.Rank(AnalysisMode, AnalysisLevel);

    /// <summary>How this candidate's numbers were produced — the mode axis of
    /// the two-axis depth taxonomy behind the <see cref="Depth"/> /
    /// <see cref="DepthAbbreviation"/> / <see cref="DepthRank"/> display
    /// forms, used for depth filtering together with
    /// <see cref="AnalysisLevel"/>. Producer-stamped;
    /// <see cref="AnalysisMode.Unknown"/> when the producer did not record
    /// it.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when a depth fact already stated does not belong to the
    /// mode (a trial count off a rollout or a book hit, an edition off a book hit).
    /// </exception>
    public required AnalysisMode AnalysisMode
    {
        get => _mode.GetValueOrDefault();
        init
        {
            DepthStated(value, _level, _rolloutTrials, _bookEdition, _unrecognizedLevelCode, nameof(AnalysisMode));
            _mode = value;
        }
    }

    /// <summary>Evaluation level of the analysis behind this candidate — the
    /// level axis paired with <see cref="AnalysisMode"/>. For a rollout this
    /// is the inner moves level (checker rows never carry Roller-family
    /// rollout levels — see <see cref="BgDataTypes_Lib.AnalysisMode"/>).
    /// Producer-stamped; <see cref="AnalysisLevel.Unknown"/> when the
    /// producer did not record it.</summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when <see cref="UnrecognizedLevelCode"/> is already
    /// stated and the value is a level this library recognizes.
    /// </exception>
    public required AnalysisLevel AnalysisLevel
    {
        get => _level.GetValueOrDefault();
        init
        {
            DepthStated(_mode, value, _rolloutTrials, _bookEdition, _unrecognizedLevelCode, nameof(AnalysisLevel));
            _level = value;
        }
    }

    /// <summary>
    /// The number of games the rollout behind this candidate played — an
    /// explicit rollout's, or a book hit's where the book's rollout
    /// parameters were recovered. <see langword="null"/> when none is
    /// recorded, and always for an analysis that is neither.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is less than 1, or when
    /// <see cref="AnalysisMode"/> is already stated and is neither
    /// <see cref="AnalysisMode.Rollout"/> nor <see cref="AnalysisMode.BookRollout"/>.
    /// </exception>
    public int? RolloutTrials
    {
        get => _rolloutTrials;
        init
        {
            DepthStated(_mode, _level, value, _bookEdition, _unrecognizedLevelCode, nameof(RolloutTrials));
            _rolloutTrials = value;
        }
    }

    /// <summary>
    /// The opening-book edition of a book hit (<see cref="AnalysisMode.BookRollout"/>);
    /// <see langword="null"/> when none is recorded, and always for any other
    /// analysis.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is stated and <see cref="AnalysisMode"/>
    /// is already stated and is not <see cref="AnalysisMode.BookRollout"/>.
    /// </exception>
    public BookEdition? BookEdition
    {
        get => _bookEdition;
        init
        {
            DepthStated(_mode, _level, _rolloutTrials, value, _unrecognizedLevelCode, nameof(BookEdition));
            _bookEdition = value;
        }
    }

    /// <summary>
    /// The producing analyser's raw code for a level this library does not
    /// recognize — stated only with <see cref="AnalysisLevel"/>
    /// <see cref="AnalysisLevel.Unknown"/>, so an unrecognized level still
    /// reads as itself ("level-{code}") rather than as nothing.
    /// <see langword="null"/> otherwise.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown on init when the value is stated and <see cref="AnalysisLevel"/>
    /// is already stated and is a recognized level.
    /// </exception>
    public int? UnrecognizedLevelCode
    {
        get => _unrecognizedLevelCode;
        init
        {
            DepthStated(_mode, _level, _rolloutTrials, _bookEdition, value, nameof(UnrecognizedLevelCode));
            _unrecognizedLevelCode = value;
        }
    }

    /// <summary>
    /// Primary equity value, displayed top-right in the analysis panel. A
    /// finite number: every ranking orders the candidates by equity
    /// (<see cref="CheckerPlayDecisionData.RankedBy"/>), which a non-number
    /// would leave undefined.
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
    // predates these fields). Win is the total win probability, and the total
    // loss is derived from it; the gammon and backgammon fields are XG's G/B
    // breakdown figures, source data.

    /// <summary>Probability the on-roll player wins with this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? WinPct { get; init; }
    /// <summary>XG's gammon-win figure (the "G" of its W/G/B breakdown) for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? WinGammonPct { get; init; }
    /// <summary>XG's backgammon-win figure (the "B" of its W/G/B breakdown) for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? WinBgPct { get; init; }
    /// <summary>
    /// Probability the on-roll player loses with this play: <c>1 − </c><see cref="WinPct"/>,
    /// derived and never stored — every game is won or lost.
    /// <see langword="null"/> when the candidate was not evaluated. XG's
    /// stored figure equals the derivation within 9.5e-7 over the 273,592
    /// candidates of the local corpus (measured by the umbrella, 2026-09-26),
    /// so it was a copy.
    /// </summary>
    [JsonIgnore]
    public double? LosePct => 1.0 - WinPct;
    /// <summary>XG's gammon-loss figure for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? LoseGammonPct { get; init; }
    /// <summary>XG's backgammon-loss figure for this play. Fraction in [0, 1]; null when not evaluated.</summary>
    public double? LoseBgPct { get; init; }

    /// <summary>
    /// Holds the depth facts stated so far, <paramref name="member"/>'s value
    /// among them, to the taxonomy's rules (<see cref="DepthTaxonomy.Fault"/>).
    /// </summary>
    private void DepthStated(
        AnalysisMode? mode, AnalysisLevel? level, int? trials, BookEdition? edition, int? code, string member)
    {
        try
        {
            if (DepthTaxonomy.Fault(mode, level, trials, edition, code) is { } fault)
                throw new ArgumentException(fault, member);
        }
        catch (ArgumentException fault) when (_read)
        {
            throw DocumentRefusal.Of(fault);
        }
    }
}
