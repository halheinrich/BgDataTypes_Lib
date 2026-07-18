using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// The evaluation level behind an XG analysis — the level axis of the
/// two-axis depth taxonomy, always paired with <see cref="AnalysisMode"/>
/// (how the numbers were produced). For <see cref="AnalysisMode.Evaluation"/>
/// this is the level of the evaluation itself; for the rollout-family modes
/// it is the inner evaluation level — checker rows carry the inner moves
/// level, cube rows the inner cube level (see <see cref="AnalysisMode"/> for
/// the full pairing semantics). Classification is producer-side; this enum
/// owns the category set and the display labels.
/// </summary>
/// <remarks>
/// <para>
/// Members are declared in ascending-rigor order — plies below the XG Roller
/// family — and the UI renders level choices in declaration order. That
/// ordering is informational, not contractual: depth filtering works by
/// membership, and <see cref="PlayCandidate.DepthRank"/> /
/// <see cref="DecisionData.CubeDepthRank"/> remain the ordering surface for
/// consumers that compare depths.
/// </para>
/// <para>
/// <see cref="Unknown"/> is deliberately the zero value: any construction
/// site the producer has not yet stamped, and JSON written before the
/// two-axis pair existed, deserializes to it. It means "level not recorded",
/// not an error — in particular
/// <see cref="AnalysisMode.BookRollout"/> + <see cref="Unknown"/> is the
/// graceful-degradation stamp for a book hit whose levels the producer could
/// not recover. Variants that share a level keep their finer identity only in
/// the label strings ("3-ply red" is <see cref="Ply3"/>).
/// </para>
/// <para>
/// Every member carries a <see cref="DescriptionAttribute"/> — the UI-facing
/// label. Display text belongs to the type owner; downstream label readers
/// (e.g. <c>XgFilter_Lib</c>'s <c>EnumLabel.ToLabel</c>) treat a missing
/// <c>[Description]</c> as an error.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisLevel
{
    /// <summary>Level not recorded — unstamped or legacy data, an XG level
    /// code the producer does not recognize, or a book hit whose levels the
    /// book database could not supply (see
    /// <see cref="AnalysisMode.BookRollout"/>).</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>1-ply search.</summary>
    [Description("1-ply")]
    Ply1,

    /// <summary>2-ply search.</summary>
    [Description("2-ply")]
    Ply2,

    /// <summary>3-ply search; includes XG's reduced-variance "3-ply red".</summary>
    [Description("3-ply")]
    Ply3,

    /// <summary>4-ply search.</summary>
    [Description("4-ply")]
    Ply4,

    /// <summary>5-ply search.</summary>
    [Description("5-ply")]
    Ply5,

    /// <summary>6-ply search.</summary>
    [Description("6-ply")]
    Ply6,

    /// <summary>7-ply search.</summary>
    [Description("7-ply")]
    Ply7,

    /// <summary>XG Roller evaluation.</summary>
    [Description("XG Roller")]
    XgRoller,

    /// <summary>XG Roller+ evaluation.</summary>
    [Description("XG Roller+")]
    XgRollerPlus,

    /// <summary>XG Roller++ evaluation.</summary>
    [Description("XG Roller++")]
    XgRollerPlusPlus
}
