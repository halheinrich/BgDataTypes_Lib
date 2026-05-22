using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A player's response to the doubling cube — the four cube actions
/// available in a standard backgammon match.
/// </summary>
/// <remarks>
/// Beaver and raccoon are deliberately not yet members. Beaver is a likely
/// future addition and raccoon a possible one; both can be appended without
/// disturbing the existing members or their serialized string forms.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CubeAction
{
    /// <summary>The player on roll declines to double and proceeds to roll.</summary>
    NoDouble,

    /// <summary>The player on roll offers the doubling cube to the opponent.</summary>
    Double,

    /// <summary>The player facing a double accepts it, continuing play at the raised stake.</summary>
    Take,

    /// <summary>The player facing a double declines it, conceding the game at the current stake.</summary>
    Pass
}
