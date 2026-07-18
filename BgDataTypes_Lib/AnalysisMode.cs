using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// How the numbers behind an XG analysis were produced — the mode axis of the
/// two-axis depth taxonomy, always paired with <see cref="AnalysisLevel"/>
/// (the evaluation level). The pair is the machine-usable taxonomy behind the
/// display forms (<see cref="PlayCandidate.Depth"/> /
/// <see cref="PlayCandidate.DepthAbbreviation"/> /
/// <see cref="PlayCandidate.DepthRank"/> and their cube counterparts on
/// <see cref="DecisionData"/>), and replaces the retired flat
/// <c>AnalysisDepthClass</c>. Classification is producer-side —
/// <c>ConvertXgToJson_Lib</c> stamps both axes when building decisions; this
/// enum owns the category set and the display labels.
/// </summary>
/// <remarks>
/// <para>
/// The axes are orthogonal because a mode does not determine a level. For
/// <see cref="Evaluation"/> the paired <see cref="AnalysisLevel"/> is the
/// level of the evaluation itself. For the rollout-family modes
/// (<see cref="Rollout"/> and <see cref="BookRollout"/>) it is the level of
/// the inner evaluations the rollout played its games with — and a single
/// rollout can use different inner levels for checker moves and for cube
/// actions, so checker rows carry the inner moves level and cube rows the
/// inner cube level. Which level a given row gets stamped with is the
/// producer's concern; the semantics are owned here because this library owns
/// the taxonomy.
/// </para>
/// <para>
/// Rollout-family modes never pair with a Roller-family level on checker
/// rows, but can on cube rows — the shipped opening-book database contains
/// entries whose cube rollout level is XG Roller.
/// </para>
/// <para>
/// <see cref="Unknown"/> is deliberately the zero value: any construction
/// site the producer has not yet stamped, and JSON written before the
/// two-axis pair existed (including JSON stamped with the retired flat depth
/// class — an unrecognized property is ignored on read), deserializes to it.
/// It means "mode not recorded", not an error.
/// </para>
/// <para>
/// The UI renders mode choices in declaration order. Every member carries a
/// <see cref="DescriptionAttribute"/> — the UI-facing label. Display text
/// belongs to the type owner; downstream label readers (e.g.
/// <c>XgFilter_Lib</c>'s <c>EnumLabel.ToLabel</c>) treat a missing
/// <c>[Description]</c> as an error.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisMode
{
    /// <summary>Mode not recorded — unstamped or legacy data (including JSON
    /// stamped with the retired flat depth class), or an XG level code the
    /// producer does not recognize.</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Direct evaluation of the position at the paired
    /// <see cref="AnalysisLevel"/> — a ply search or an XG Roller-family
    /// evaluation, with no game playout.</summary>
    [Description("Evaluation")]
    Evaluation,

    /// <summary>Full rollout recorded in the source file; the paired
    /// <see cref="AnalysisLevel"/> is the inner evaluation level. Trial count
    /// stays in the display label (<see cref="PlayCandidate.Depth"/> /
    /// <see cref="DecisionData.CubeDepth"/>) — it is not a taxonomy
    /// axis.</summary>
    [Description("Rollout")]
    Rollout,

    /// <summary>Opening-book hit — XG's opening book is rollout-derived, so a
    /// book hit is a cached rollout whose parameters live in the book
    /// database rather than the source file. The paired
    /// <see cref="AnalysisLevel"/> is the book entry's inner level when the
    /// book database supplied it; <see cref="AnalysisLevel.Unknown"/> is the
    /// graceful-degradation stamp (no book database available at conversion
    /// time, or a V1-book hit whose entry records no levels).</summary>
    [Description("Book rollout")]
    BookRollout
}
