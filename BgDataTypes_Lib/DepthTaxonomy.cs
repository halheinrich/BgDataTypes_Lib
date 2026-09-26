using System.Globalization;

namespace BgDataTypes_Lib;

/// <summary>
/// The depth of an analysis, derived: its rank, its label and its
/// abbreviation, from the typed facts a record stores — the
/// <see cref="AnalysisMode"/> × <see cref="AnalysisLevel"/> pair, the rollout
/// trial count, the opening-book edition and an unrecognized level's raw code
/// — and the rules those facts keep together. The one home of the taxonomy's
/// derivations for <see cref="PlayCandidate"/> and
/// <see cref="CubeDecisionData"/> alike.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why here.</b> Each of the three is determined by the facts, so a record
/// stores the facts and derives them — no stored copy of a derivable value.
/// The rank grid and the text grammar moved here from the producer
/// (<c>ConvertXgToJson_Lib</c>'s <c>LevelInfo</c>, <c>ResolveDepthInfo</c>,
/// <c>InnerLevelToken</c> and <c>DepthAbbreviationFormat</c>), which produced
/// the same values alongside each record; this library owns the taxonomy
/// they spell. The numbers and the text are unchanged.
/// </para>
/// <para>
/// <b>The rank grid.</b> An evaluation ranks by its level, in XG's own
/// analysis-level menu order — the ply family and the XG Roller family
/// interleave, on a decade grid with the in-between levels at midpoints:
/// 1-ply 10, 2-ply 20, 3-ply Red 25, 3-ply 30, XG Roller 35, 4-ply 40,
/// XG Roller+ 45, 5-ply 50, 6-ply 60, 7-ply 70, XG Roller++ 75 (the user's
/// ruling of 2026-08-28, the order <see cref="AnalysisLevel"/> declares). An
/// opening-book hit (<see cref="AnalysisMode.BookRollout"/>) ranks 99 whatever
/// its level: a cached rollout, above every evaluation, below every rollout
/// the source file carries. An explicit rollout ranks 100 plus its inner
/// level's grid rank — a 3-ply rollout 130 — so rollouts order among
/// themselves by the grid and all outrank the book; a rollout whose inner
/// level is not recorded ranks at the rollout floor, 100, still a rank: the
/// rollout itself is recorded. An analysis whose mode is not recorded, and an
/// evaluation whose level is not, have no rank (<see langword="null"/>; the
/// producer wrote the grid's floor, 0, a number standing for "none").
/// </para>
/// <para>
/// <b>The label.</b> An evaluation is its level's label ("3-ply",
/// "XG Roller+"); an explicit rollout "Rollout: {trials} trials. {inner
/// level's label}", or "Rollout" where no trial count is recorded; a book hit
/// "Book V2", or "Book V2: {trials} trials. {inner level's label}" where the
/// book's rollout parameters were recovered ("Book" where the edition is not
/// recorded). An unrecognized level reads as "level-{code}", its raw code.
/// </para>
/// <para>
/// <b>The abbreviation</b> is the compact form for a narrow cell. An
/// evaluation's is its level's ("3-ply", "R+"); a rollout's is the inner
/// level's token, <c>p</c>, then the trial count ("3p1296", "Rp1296"), or
/// "Ro" with no trial count; a book hit's is <c>B</c>, the inner level's
/// token, <c>_</c>, then the trial count ("B4_12960"), or "Book" with none.
/// A level's token is its ply number ("3" for both 3-ply and 3-ply Red — the
/// abbreviation is the lossy form; the label keeps the distinction), or its
/// XG Roller abbreviation ("R", "R+", "R++"), or "level-{code}". The rollout
/// and book forms differ in prefix and separator only, by the user's ruling
/// of 2026-09-16 (halheinrich/backgammon#240).
/// </para>
/// <para>
/// <b>The facts' rules.</b> A trial count is a positive number of games and
/// belongs to a rollout or a book hit; a book edition belongs to a book hit;
/// a raw level code is stated only for a level this library does not
/// recognize (<see cref="AnalysisLevel.Unknown"/>). Where no code is stated,
/// an unrecognized inner level reads as its <see cref="AnalysisLevel.Unknown"/>
/// label, "Unknown", with the token "?" — a shape no producer writes today.
/// </para>
/// </remarks>
internal static class DepthTaxonomy
{
    // -----------------------------------------------------------------------
    //  The rank
    // -----------------------------------------------------------------------

    /// <summary>
    /// The rank of an analysis produced by <paramref name="mode"/> at
    /// <paramref name="level"/>, or <see langword="null"/> when the depth is
    /// not recorded.
    /// </summary>
    internal static int? Rank(AnalysisMode mode, AnalysisLevel level) => mode switch
    {
        AnalysisMode.Evaluation => GridRank(level),
        AnalysisMode.BookRollout => 99,
        AnalysisMode.Rollout => 100 + (GridRank(level) ?? 0),
        _ => null,
    };

    /// <summary>A level's place on the evaluation grid; <see langword="null"/> for a level not recorded.</summary>
    private static int? GridRank(AnalysisLevel level) => level switch
    {
        AnalysisLevel.Ply1 => 10,
        AnalysisLevel.Ply2 => 20,
        AnalysisLevel.Ply3Red => 25,
        AnalysisLevel.Ply3 => 30,
        AnalysisLevel.XgRoller => 35,
        AnalysisLevel.Ply4 => 40,
        AnalysisLevel.XgRollerPlus => 45,
        AnalysisLevel.Ply5 => 50,
        AnalysisLevel.Ply6 => 60,
        AnalysisLevel.Ply7 => 70,
        AnalysisLevel.XgRollerPlusPlus => 75,
        _ => null,
    };

    // -----------------------------------------------------------------------
    //  The label and the abbreviation
    // -----------------------------------------------------------------------

    /// <summary>
    /// The label of an analysis with these facts, or <see langword="null"/>
    /// when none is recorded — the mode and the level unknown, and no raw code.
    /// </summary>
    internal static string? Label(
        AnalysisMode mode, AnalysisLevel level, int? trials, BookEdition? edition, int? code) => mode switch
    {
        AnalysisMode.Evaluation => LevelLabel(level) ?? CodeText(code),
        AnalysisMode.Rollout => trials is int rolled
            ? Invariant($"Rollout: {rolled} trials. {InnerLabel(level, code)}")
            : "Rollout",
        AnalysisMode.BookRollout => trials is int bookTrials
            ? Invariant($"{EditionText(edition)}: {bookTrials} trials. {InnerLabel(level, code)}")
            : EditionText(edition),
        _ => CodeText(code),
    };

    /// <summary>
    /// The compact form of <see cref="Label"/>'s label, or
    /// <see langword="null"/> when none is recorded.
    /// </summary>
    internal static string? Abbreviation(
        AnalysisMode mode, AnalysisLevel level, int? trials, int? code) => mode switch
    {
        AnalysisMode.Evaluation => LevelAbbreviation(level) ?? CodeText(code),
        AnalysisMode.Rollout => trials is int rolled
            ? Invariant($"{Token(level, code)}p{rolled}")
            : "Ro",
        AnalysisMode.BookRollout => trials is int bookTrials
            ? Invariant($"B{Token(level, code)}_{bookTrials}")
            : "Book",
        _ => CodeText(code),
    };

    /// <summary>
    /// A level's label — what its <see cref="System.ComponentModel.DescriptionAttribute"/>
    /// reads, which a test holds this table to — or <see langword="null"/> for
    /// a level not recorded.
    /// </summary>
    internal static string? LevelLabel(AnalysisLevel level) => level switch
    {
        AnalysisLevel.Ply1 => "1-ply",
        AnalysisLevel.Ply2 => "2-ply",
        AnalysisLevel.Ply3Red => "3-ply Red",
        AnalysisLevel.Ply3 => "3-ply",
        AnalysisLevel.XgRoller => "XG Roller",
        AnalysisLevel.Ply4 => "4-ply",
        AnalysisLevel.XgRollerPlus => "XG Roller+",
        AnalysisLevel.Ply5 => "5-ply",
        AnalysisLevel.Ply6 => "6-ply",
        AnalysisLevel.Ply7 => "7-ply",
        AnalysisLevel.XgRollerPlusPlus => "XG Roller++",
        _ => null,
    };

    /// <summary>A level's own abbreviation: a ply label kept whole, the XG Roller family shortened.</summary>
    private static string? LevelAbbreviation(AnalysisLevel level) => level switch
    {
        AnalysisLevel.XgRoller => "R",
        AnalysisLevel.XgRollerPlus => "R+",
        AnalysisLevel.XgRollerPlusPlus => "R++",
        _ => LevelLabel(level),
    };

    /// <summary>An inner level's token in the trial-bearing abbreviations: its ply number, or its own abbreviation.</summary>
    private static string Token(AnalysisLevel level, int? code) => level switch
    {
        AnalysisLevel.Ply1 => "1",
        AnalysisLevel.Ply2 => "2",
        AnalysisLevel.Ply3Red or AnalysisLevel.Ply3 => "3",
        AnalysisLevel.Ply4 => "4",
        AnalysisLevel.Ply5 => "5",
        AnalysisLevel.Ply6 => "6",
        AnalysisLevel.Ply7 => "7",
        _ => LevelAbbreviation(level) ?? CodeText(code) ?? "?",
    };

    private static string InnerLabel(AnalysisLevel level, int? code) =>
        LevelLabel(level) ?? CodeText(code) ?? "Unknown";

    private static string EditionText(BookEdition? edition) => edition switch
    {
        BookEdition.V1 => "Book V1",
        BookEdition.V2 => "Book V2",
        _ => "Book",
    };

    private static string? CodeText(int? code) => code is int raw ? Invariant($"level-{raw}") : null;

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    // -----------------------------------------------------------------------
    //  The facts' rules
    // -----------------------------------------------------------------------

    /// <summary>
    /// The first way the facts stated so far break the rules, or
    /// <see langword="null"/> when they keep them. A fact not yet stated is
    /// <see langword="null"/> — the members set in any order, so each setter
    /// checks what is known.
    /// </summary>
    internal static string? Fault(AnalysisMode? mode, AnalysisLevel? level, int? trials, BookEdition? edition, int? code)
    {
        if (trials is < 1)
            return $"RolloutTrials is a number of games rolled, at least 1 (got {trials}).";
        if (trials is not null && mode is not (null or AnalysisMode.Rollout or AnalysisMode.BookRollout))
            return $"RolloutTrials belongs to a rollout or a book hit; an analysis by {mode} has none.";
        if (edition is not null && mode is not (null or AnalysisMode.BookRollout))
            return $"BookEdition belongs to a book hit; an analysis by {mode} has none.";
        if (code is not null && level is not (null or AnalysisLevel.Unknown))
            return $"UnrecognizedLevelCode is stated only for a level this library does not recognize; {level} is recognized.";
        return null;
    }
}
