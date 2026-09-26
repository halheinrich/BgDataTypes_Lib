namespace BgDataTypes_Lib;

/// <summary>
/// Match-level metadata contract — the shape a consumer needs to decide
/// whether to skip an entire match (or money session) before any of its
/// decisions are produced. Parallel in spirit to
/// <see cref="IDecisionFilterData"/>: producers (e.g. the XG parser's
/// match-info type) implement it, and filter layers consume it without
/// referencing any producer's concrete types. Minimal by design — members are
/// added on demand, not mirrored wholesale from a producer.
/// See <see cref="IGameInfo"/> for the game-scope companion.
/// </summary>
/// <remarks>
/// <b>Money versus match is the terms' kind</b> (halheinrich/backgammon#273,
/// Hal's ruling of 2026-09-26): <see cref="Terms"/> is a money session's
/// rules or a match's length. The match length of 0 that spelled money, and
/// the <c>IsMoneyGame</c> derived from it, are gone: a consumer matches on
/// the terms' kind (<see cref="SessionTerms.Match{TResult}"/>), and a
/// producer states which kind the header is — XG's raw money sentinel (a
/// length of 99999) is read at the parse boundary as money terms, never
/// surfaced as a length.
/// </remarks>
public interface IMatchInfo
{
    /// <summary>Name of player 1 (bottom player in XG).</summary>
    string Player1 { get; }

    /// <summary>Name of player 2 (top player in XG).</summary>
    string Player2 { get; }

    /// <summary>
    /// The terms the session is played on: a money session's rules
    /// (<see cref="MoneyTerms"/> — the Jacoby and beaver rules and the cube
    /// limit) or a match's length (<see cref="MatchTerms"/>).
    /// </summary>
    SessionTerms Terms { get; }
}
