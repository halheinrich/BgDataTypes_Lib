using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

/// <summary>
/// A player's seat in a session, as its headers name the players: player 1 or
/// player 2 (XG's bottom and top player). A header states its facts by seat —
/// <see cref="GameStanding"/>'s <c>1</c> and <c>2</c>,
/// <see cref="IMatchInfo.Player1"/> and <see cref="IMatchInfo.Player2"/> —
/// while a record states them from the player on roll's side;
/// <see cref="Session.Create"/> turns the one into the other, given the seat
/// on roll (halheinrich/backgammon#273, the umbrella's review of 2026-09-26).
/// </summary>
/// <remarks>
/// No record holds a seat: a record is the player on roll's view whatever
/// seat that player sits in. Like every enum here the type bundles the strict
/// string token (<see cref="StrictJsonStringEnumConverter{TEnum}"/>), so a
/// producer that writes one writes its name, and reads only a name back.
/// </remarks>
[JsonConverter(typeof(StrictJsonStringEnumConverter<Seat>))]
public enum Seat
{
    /// <summary>Player 1: the first player a header names (XG's bottom player).</summary>
    Player1,

    /// <summary>Player 2: the second player a header names (XG's top player).</summary>
    Player2,
}
