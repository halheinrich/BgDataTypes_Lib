using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Depth class of an XG analysis — the machine-usable taxonomy behind the
/// display forms (<see cref="PlayCandidate.Depth"/> /
/// <see cref="PlayCandidate.DepthAbbreviation"/> /
/// <see cref="PlayCandidate.DepthRank"/> and their cube counterparts on
/// <see cref="DecisionData"/>). Classification is producer-side —
/// <c>ConvertXgToJson_Lib</c> maps XG's level codes onto these classes when
/// stamping decisions; this enum owns the category set and the display
/// labels.
/// </summary>
/// <remarks>
/// <para>
/// Members are declared in ascending-rigor order, mirroring the producer's
/// ordinal ranks (Book and unknown rank 0, N-ply 1–7, the XG Roller family
/// 20–22, rollouts 100 for unknown inner ply through 107 for a 7-ply
/// inner evaluation). That ordering is informational, not contractual:
/// depth filtering works by membership, and nothing guarantees the numeric
/// values stay stable if the taxonomy grows. <see cref="PlayCandidate.DepthRank"/>
/// remains the ordering surface for consumers that compare depths.
/// </para>
/// <para>
/// <see cref="Unknown"/> is deliberately the zero value: JSON written before
/// this field existed — and any construction site the producer has not yet
/// stamped — deserializes to it. Variants that share a class keep their finer
/// identity only in the label string ("3-ply red" is <see cref="Ply3"/>;
/// Book V1 and V2 are both <see cref="Book"/>; rollout trial counts live in
/// the label / abbreviation — trial count is not a taxonomy axis, but rollout
/// inner ply is: <see cref="RolloutPly1"/>–<see cref="RolloutPly7"/> preserve
/// it, with <see cref="Rollout"/> as the floor for a rollout whose inner ply
/// is unknown).
/// </para>
/// <para>
/// Every member carries a <see cref="DescriptionAttribute"/> — the UI-facing
/// label. Display text belongs to the type owner; downstream label readers
/// (e.g. <c>XgFilter_Lib</c>'s <c>EnumLabel.ToLabel</c>) treat a missing
/// <c>[Description]</c> as an error.
/// </para>
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisDepthClass
{
    /// <summary>Depth not recorded — unstamped or legacy data, or an XG level
    /// code the producer does not recognize.</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Static opening-book lookup (Book V1 or V2) — a table hit,
    /// not a search of the position.</summary>
    [Description("Book")]
    Book,

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
    XgRollerPlusPlus,

    /// <summary>Full rollout whose inner evaluation ply is unknown — the
    /// producer's no-context rollout sentinel (rank 100). The floor of the
    /// rollout tier; a rollout with a known inner ply classifies as
    /// <see cref="RolloutPly1"/>–<see cref="RolloutPly7"/>.</summary>
    [Description("Rollout")]
    Rollout,

    /// <summary>Full rollout with a 1-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (1-ply)")]
    RolloutPly1,

    /// <summary>Full rollout with a 2-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (2-ply)")]
    RolloutPly2,

    /// <summary>Full rollout with a 3-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (3-ply)")]
    RolloutPly3,

    /// <summary>Full rollout with a 4-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (4-ply)")]
    RolloutPly4,

    /// <summary>Full rollout with a 5-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (5-ply)")]
    RolloutPly5,

    /// <summary>Full rollout with a 6-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (6-ply)")]
    RolloutPly6,

    /// <summary>Full rollout with a 7-ply inner evaluation, at any trial count.</summary>
    [Description("Rollout (7-ply)")]
    RolloutPly7
}
