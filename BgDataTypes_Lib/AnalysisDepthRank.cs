namespace BgDataTypes_Lib;

/// <summary>
/// The ordinal rank of an analysis — <see cref="PlayCandidate.DepthRank"/>'s
/// and <see cref="CubeDecisionData.DepthRank"/>'s one derivation from the
/// <see cref="AnalysisMode"/> × <see cref="AnalysisLevel"/> pair that
/// determines it. Higher is deeper; only the ordering means anything, and
/// consumers compare ranks with each other, never with a constant.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why here.</b> The rank is determined by the pair, so a record stores
/// the pair and derives the rank — no stored copy of a derivable value. The
/// grid moved here from the producer (<c>ConvertXgToJson_Lib</c>'s
/// <c>LevelInfo</c> / <c>ResolveDepthInfo</c>), which produced the same
/// numbers alongside each record; this library owns the taxonomy the grid
/// ranks. Its numbers are unchanged.
/// </para>
/// <para>
/// <b>The grid.</b> An evaluation ranks by its level, in XG's own
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
/// level is not recorded ranks at the rollout floor, 100, which is still a
/// rank: the rollout itself is recorded.
/// </para>
/// <para>
/// <b>Not recorded is <see langword="null"/>.</b> An analysis whose mode is
/// not recorded, and an evaluation whose level is not, have no rank. The
/// producer spelled that 0, the grid's floor; a number standing for "none"
/// is a stand-in, so the rank is <see langword="null"/> there instead.
/// </para>
/// </remarks>
internal static class AnalysisDepthRank
{
    /// <summary>
    /// The rank of an analysis produced by <paramref name="mode"/> at
    /// <paramref name="level"/>, or <see langword="null"/> when the depth is
    /// not recorded.
    /// </summary>
    internal static int? Of(AnalysisMode mode, AnalysisLevel level) => mode switch
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
}
