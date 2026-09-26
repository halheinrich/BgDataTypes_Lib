using System.Numerics;

namespace BgDataTypes_Lib;

/// <summary>
/// The terms a session is played on, as its header states them — what
/// <see cref="IMatchInfo.Terms"/> reports before any game or decision is
/// read: a money session's rules (<see cref="MoneyTerms"/>) or a match's
/// length (<see cref="MatchTerms"/>) (halheinrich/backgammon#273, Hal's ruling
/// of 2026-09-26). Each kind carries only its own facts, so money is the
/// kind, never a match length of 0, and a match states no money rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>One of the three scopes of money versus match.</b> A session's terms
/// are the header's; a game's standing is <see cref="GameStanding"/>
/// (<see cref="IGameInfo.Standing"/>); a decision's session, from the player
/// on roll's side, is <see cref="Session"/> — the terms and the standing
/// together, which is what a record holds. The three share
/// <see cref="SessionKind"/> and the rules of the internal
/// <see cref="SessionRules"/>, stated once.
/// </para>
/// <para>
/// <b>A closed pair, a value.</b> The constructor is not reachable outside
/// this library, <see cref="Match{TResult}"/> and <see cref="Switch"/> take
/// one branch per kind, and two terms are equal exactly when they are the
/// same kind with the same facts. No JSON contract: a producer's
/// <see cref="IMatchInfo"/> is read in process, never serialized.
/// </para>
/// </remarks>
public abstract class SessionTerms : IEquatable<SessionTerms>, IEqualityOperators<SessionTerms, SessionTerms, bool>
{
    /// <summary>The kind is fixed here, by the two kinds in this library only.</summary>
    private protected SessionTerms(SessionKind kind) => Kind = kind;

    /// <summary>The session's kind: every <see cref="MoneyTerms"/> is <see cref="SessionKind.Money"/>, every <see cref="MatchTerms"/> <see cref="SessionKind.Match"/>.</summary>
    public SessionKind Kind { get; }

    /// <summary>The result of the branch for this kind — one branch per kind, so a new kind breaks every call at compile time.</summary>
    /// <typeparam name="TResult">The type both branches return.</typeparam>
    /// <param name="money">The branch for <see cref="MoneyTerms"/>.</param>
    /// <param name="match">The branch for <see cref="MatchTerms"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract TResult Match<TResult>(Func<MoneyTerms, TResult> money, Func<MatchTerms, TResult> match);

    /// <summary>Runs the branch for this kind — the statement form of <see cref="Match{TResult}"/>.</summary>
    /// <param name="money">The branch for <see cref="MoneyTerms"/>.</param>
    /// <param name="match">The branch for <see cref="MatchTerms"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when either branch is <see langword="null"/>.</exception>
    public abstract void Switch(Action<MoneyTerms> money, Action<MatchTerms> match);

    /// <summary>Whether <paramref name="other"/> is terms of this kind with the same facts.</summary>
    /// <param name="other">The terms to compare with; <see langword="null"/> is never equal.</param>
    public abstract bool Equals(SessionTerms? other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is SessionTerms other && Equals(other);

    /// <summary>A hash of the kind and its facts, consistent with <see cref="Equals(SessionTerms?)"/>.</summary>
    public abstract override int GetHashCode();

    /// <summary>Whether the two are equal terms; two nulls are equal.</summary>
    public static bool operator ==(SessionTerms? left, SessionTerms? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the two are not equal terms.</summary>
    public static bool operator !=(SessionTerms? left, SessionTerms? right) => !(left == right);
}
