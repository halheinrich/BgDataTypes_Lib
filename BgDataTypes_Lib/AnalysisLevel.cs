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
/// <b>Declaration order is contractual.</b> Every member after
/// <see cref="Unknown"/> is declared in ascending rigor, in the order XG's
/// own analysis-level menu presents them: the ply family and the XG Roller
/// family <i>interleave</i> — <see cref="Ply3"/> then <see cref="XgRoller"/>
/// then <see cref="Ply4"/> then <see cref="XgRollerPlus"/> then
/// <see cref="Ply5"/> — they are not two separate blocks. Reordering the
/// members, or inserting a new one anywhere but its true rigor position, is a
/// breaking change: consumers read the order, not just the membership. The
/// order and the interleave are the user's ruling of 2026-08-28, on the
/// authority of XG's own menu, amended the same day to give
/// <see cref="Ply3Red"/> its own identity.
/// </para>
/// <para>
/// The promotion from "informational" to contractual is what the live
/// consumers already need: the error-diagram's level floor compares levels by
/// rigor, and the filter panel's and quiz's level dropdowns present them in
/// this order as a rigor ladder. A rendering convenience they may not rely on
/// would leave both silently wrong after any reorder.
/// </para>
/// <para>
/// <see cref="Unknown"/> sits <i>outside</i> the rigor scale — clause (a) of
/// the same ruling. It is not "the least rigorous level"; it means the level
/// was not recorded. It is therefore never excluded by a rigor floor and
/// never offered as a rigor threshold. Its position at the head of the
/// declaration is the zero-value requirement below, not a rank.
/// </para>
/// <para>
/// The enum's order is a rigor ordering over levels alone. Consumers that
/// need to compare whole analyses — across the mode axis as well — still use
/// <see cref="PlayCandidate.DepthRank"/> /
/// <see cref="DecisionData.CubeDepthRank"/>, which rank the
/// <see cref="AnalysisMode"/> × <see cref="AnalysisLevel"/> pair.
/// </para>
/// <para>
/// <see cref="Unknown"/> is deliberately the zero value: any construction
/// site the producer has not yet stamped, and JSON written before the
/// two-axis pair existed, deserializes to it. It means "level not recorded",
/// not an error — in particular
/// <see cref="AnalysisMode.BookRollout"/> + <see cref="Unknown"/> is the
/// graceful-degradation stamp for a book hit whose levels the producer could
/// not recover.
/// </para>
/// <para>
/// The members serialize as their names, not their numbers
/// (<see cref="JsonStringEnumConverter"/>, bundled on the type). That is what
/// makes the ruled order safe to adopt and <see cref="Ply3Red"/> safe to
/// insert: existing JSON carries tokens, so renumbering moves no wire value.
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
    /// <see cref="AnalysisMode.BookRollout"/>). Outside the rigor scale: not
    /// a floor, not a threshold, never "least rigorous".</summary>
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>1-ply search.</summary>
    [Description("1-ply")]
    Ply1,

    /// <summary>2-ply search.</summary>
    [Description("2-ply")]
    Ply2,

    /// <summary>XG's "3-ply Red" setting — its reduced-variance 3-ply
    /// search, which XG's own menu ranks below a full <see cref="Ply3"/>.
    /// Its own member, not a label variant of <see cref="Ply3"/> (ruled
    /// 2026-08-28).</summary>
    [Description("3-ply Red")]
    Ply3Red,

    /// <summary>3-ply search.</summary>
    [Description("3-ply")]
    Ply3,

    /// <summary>XG Roller evaluation — above <see cref="Ply3"/> and below
    /// <see cref="Ply4"/> in XG's own ordering.</summary>
    [Description("XG Roller")]
    XgRoller,

    /// <summary>4-ply search.</summary>
    [Description("4-ply")]
    Ply4,

    /// <summary>XG Roller+ evaluation — above <see cref="Ply4"/> and below
    /// <see cref="Ply5"/> in XG's own ordering.</summary>
    [Description("XG Roller+")]
    XgRollerPlus,

    /// <summary>5-ply search.</summary>
    [Description("5-ply")]
    Ply5,

    /// <summary>6-ply search.</summary>
    [Description("6-ply")]
    Ply6,

    /// <summary>7-ply search.</summary>
    [Description("7-ply")]
    Ply7,

    /// <summary>XG Roller++ evaluation — the most rigorous level XG
    /// offers.</summary>
    [Description("XG Roller++")]
    XgRollerPlusPlus
}
