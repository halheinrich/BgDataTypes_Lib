namespace BgDataTypes_Lib;

/// <summary>
/// Game-level metadata contract — the shape a consumer needs to decide
/// whether to skip an entire game before any of its decisions are produced.
/// The game-scope companion of <see cref="IMatchInfo"/>: producers implement
/// it, filter layers consume it without referencing any producer's concrete
/// types, and members are added on demand rather than mirrored wholesale
/// from a producer.
/// </summary>
/// <remarks>
/// <b>Money versus match is the standing's kind</b>
/// (halheinrich/backgammon#273, Hal's ruling of 2026-09-26):
/// <see cref="Standing"/> is a money session's scores or a match's away
/// scores and Crawford flag. The money convention it replaces — away scores
/// of 0 and a Crawford flag always false, which a consumer read money off —
/// is gone with the members it held.
/// </remarks>
public interface IGameInfo
{
    /// <summary>
    /// True if the game starts from the standard backgammon opening position.
    /// False if started from a saved or custom position.
    /// Used to filter for opening move decisions.
    /// </summary>
    bool IsStandardStart { get; }

    /// <summary>
    /// Where the players stand as the game begins, player 1 and player 2: in a
    /// money session the points each has won (<see cref="MoneyStanding"/>),
    /// in a match what each still needs and whether this is the Crawford game
    /// (<see cref="MatchStanding"/>).
    /// </summary>
    GameStanding Standing { get; }
}
