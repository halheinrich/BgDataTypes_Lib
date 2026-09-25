using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A single checker move. Immutable value type — no heap allocation.
///
/// FrPt: source point (1-24 on board, 25 for bar entry).
/// ToPt: destination point, sign-encoded:
///   Positive (1-24): regular move to that point.
///   Zero: bear off (checker removed from board).
///   Negative (-1 to -24): hit — land on |ToPt| and send opponent blot to bar.
///
/// The encoding stores everything needed to undo the move.
///
/// On the wire both members must be present (<see cref="JsonRequiredAttribute"/>,
/// the wire rule stated on <see cref="BgDataTypesJsonContext"/>): an absent
/// <c>ToPt</c> would otherwise read as 0, a bear-off.
/// </summary>
public readonly record struct Move(
    [property: JsonRequired] int FrPt,
    [property: JsonRequired] int ToPt);
