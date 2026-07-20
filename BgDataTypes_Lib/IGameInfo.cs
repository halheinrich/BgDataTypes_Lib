namespace BgDataTypes_Lib;

/// <summary>
/// Game-level metadata contract — the shape a consumer needs to decide
/// whether to skip an entire game before any of its decisions are produced.
/// The game-scope companion of <see cref="IMatchInfo"/>: producers implement
/// it, filter layers consume it without referencing any producer's concrete
/// types, and members are added on demand rather than mirrored wholesale
/// from a producer.
/// Money sessions: <see cref="Away1"/> = 0, <see cref="Away2"/> = 0,
/// <see cref="IsCrawfordGame"/> = false.
/// </summary>
public interface IGameInfo
{
    /// <summary>
    /// True if the game starts from the standard backgammon opening position.
    /// False if started from a saved or custom position.
    /// Used to filter for opening move decisions.
    /// </summary>
    bool IsStandardStart { get; }

    /// <summary>
    /// Points still needed by player 1 to win the match.
    /// 0 for money sessions.
    /// </summary>
    int Away1 { get; }

    /// <summary>
    /// Points still needed by player 2 to win the match.
    /// 0 for money sessions.
    /// </summary>
    int Away2 { get; }

    /// <summary>True if the Crawford rule applies to this game.</summary>
    bool IsCrawfordGame { get; }
}
