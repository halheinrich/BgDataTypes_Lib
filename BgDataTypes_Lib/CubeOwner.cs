using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// Who may next use the doubling cube. On-roll-relative, matching the
/// <c>PositionData.Mop</c> POV convention — ownership is expressed relative
/// to the player on roll, not to a fixed seat or color. Serializes as the
/// member name string via the bundled converter.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CubeOwner
{
    /// <summary>The player on roll owns the cube (only they may double).</summary>
    OnRoll,

    /// <summary>The opponent owns the cube (the on-roll player may not double).</summary>
    Opponent,

    /// <summary>Nobody owns the cube yet — its start-of-game state; either player may make the first double.</summary>
    Centered
}
