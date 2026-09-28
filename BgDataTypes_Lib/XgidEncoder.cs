using System.Globalization;
using System.Numerics;
using System.Text;

namespace BgDataTypes_Lib;

/// <summary>
/// Writes a decision's XGID — the position string eXtreme Gammon reads —
/// from the record's own members: the one derivation of
/// <see cref="BgDecisionData.Xgid"/>. Moved here from
/// <c>ConvertXgToJson_Lib</c>, whose copy stored the string on every record
/// (halheinrich/backgammon#273, the match-context leg); the record states
/// every fact the string spells, so the string is derived and never stored.
/// </summary>
/// <remarks>
/// <para>
/// <b>The format</b>, ten fields after <c>XGID=</c>, colon-separated:
/// </para>
/// <code>
/// position : cube : cubePos : turn : dice : score1 : score2 : crawfordJacoby : matchLength : maxCube
/// </code>
/// <list type="bullet">
/// <item><description><b>position</b> — 26 characters, one per slot of the
/// board from the player on roll's side (<see cref="PositionData.Mop"/>'s
/// layout: slot 0 the opponent's bar, 25 the player on roll's): <c>-</c>
/// empty, <c>A</c>–<c>O</c> that many of the player on roll's checkers,
/// <c>a</c>–<c>o</c> the opponent's.</description></item>
/// <item><description><b>cube</b> — the exponent of
/// <see cref="PositionData.CubeSize"/> (0 for 1, 1 for 2, …).</description></item>
/// <item><description><b>cubePos</b> — 1 when the player on roll owns the
/// cube, 0 centred, -1 the opponent.</description></item>
/// <item><description><b>turn</b> — 1: the string is always written from the
/// player on roll's side, as the record is.</description></item>
/// <item><description><b>dice</b> — a checker play's roll, high die first
/// (<c>31</c>: <see cref="DiceRoll"/>'s canonical token, which is how XG
/// writes it — none of the 80,497 distinct XGIDs in the local corpus is
/// low-first — whatever order the record states them rolled in);
/// <c>00</c> for a cube decision, made before the roll.</description></item>
/// <item><description><b>score1</b>, <b>score2</b> — the points each player
/// has won, the player on roll's first: for a match, its length less each
/// away score; for money, the session's scores
/// (<see cref="MoneySession.OnRollScore"/>,
/// <see cref="MoneySession.OpponentScore"/>).</description></item>
/// <item><description><b>crawfordJacoby</b> — for a match, 1 in the Crawford
/// game and 0 otherwise; for money, 1 for the Jacoby rule plus 2 for the
/// beaver rule.</description></item>
/// <item><description><b>matchLength</b> — a match's length; 0 for money, the
/// format's own spelling of money.</description></item>
/// <item><description><b>maxCube</b> — an exponent, the highest cube value
/// being <c>2^maxCube</c>: for money, the exponent of the session's
/// <see cref="MoneyTerms.CubeLimit"/>; for a match, the constant 10
/// (<see cref="MatchMaxCubeField"/>, which states why), since the record
/// states no Max Cube for a match.</description></item>
/// </list>
/// <para>
/// The format's 0s for money (the match length, and the away scores it
/// implies) are the format's, written here and nowhere else: no record
/// member holds one. Numbers are written with the invariant culture — a
/// culture that writes its minus sign as U+2212 (<c>sv-SE</c> under ICU)
/// would otherwise change the opponent's <c>-1</c> cube position.
/// </para>
/// </remarks>
internal static class XgidEncoder
{
    /// <summary>The prefix every XGID starts with.</summary>
    internal const string Prefix = "XGID=";

    /// <summary>
    /// The maxCube field this encoder writes for a match: 10 (<c>2^10</c>),
    /// the value XG typically writes there for a match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A constant because the record states none</b> (Hal's ruling of
    /// 2026-09-27 on halheinrich/backgammon#273, which withdrew the earlier
    /// assumption that XG writes 10 for every match). No record member, and no
    /// <see cref="MatchTerms"/> member, holds a Max Cube for a match, so there
    /// is nothing of the record's for this field to spell, and 10 is the value
    /// XG typically writes. Legitimate XG-authored files state other values
    /// too — 3 for a 5-point match and 4 for a 9-point one among them — so the
    /// XGID derived for such a match differs from XG's own in this field.
    /// </para>
    /// <para>
    /// What the constant does not claim: that a match has no cube limit, or
    /// that its length bounds what its cube can win — whether a match's Max
    /// Cube is a match-domain rule of its own is not established
    /// (halheinrich/backgammon#289); nor any relation to the match's length.
    /// The two values above happen to be the smallest exponent whose cube
    /// value covers the length, but XG does not follow that consistently, so
    /// nothing here encodes it.
    /// </para>
    /// </remarks>
    internal const int MatchMaxCubeField = 10;

    /// <summary>
    /// The XGID of a decision made at <paramref name="position"/>: a checker
    /// play with <paramref name="dice"/>, or a cube decision when
    /// <paramref name="dice"/> is <see langword="null"/>.
    /// </summary>
    internal static string Encode(PositionData position, DiceRoll? dice)
    {
        var (score1, score2, crawfordJacoby, matchLength, maxCube) = position.Session.Match(
            static money => (money.OnRollScore, money.OpponentScore,
                (money.Terms.IsJacoby ? 1 : 0) + (money.Terms.IsBeaver ? 2 : 0), 0, Exponent(money.Terms.CubeLimit)),
            static match => (match.Terms.Length - match.OnRollNeeds, match.Terms.Length - match.OpponentNeeds,
                match.IsCrawford ? 1 : 0, match.Terms.Length, MatchMaxCubeField));

        var xgid = new StringBuilder(Prefix, 64);
        for (int slot = 0; slot < BoardPosition.SlotCount; slot++)
            xgid.Append(Point(position.Mop[slot]));

        var invariant = CultureInfo.InvariantCulture;
        xgid.Append(':').Append(Exponent(position.CubeSize).ToString(invariant))
            .Append(':').Append(CubePosition(position.CubeOwner).ToString(invariant))
            .Append(":1:")
            .Append(dice is { } roll ? roll.ToString() : "00")
            .Append(':').Append(score1.ToString(invariant))
            .Append(':').Append(score2.ToString(invariant))
            .Append(':').Append(crawfordJacoby.ToString(invariant))
            .Append(':').Append(matchLength.ToString(invariant))
            .Append(':').Append(maxCube.ToString(invariant));
        return xgid.ToString();
    }

    /// <summary>A slot's character: <c>-</c> empty, a capital for the player on roll's checkers, a small letter for the opponent's.</summary>
    private static char Point(int count) => count switch
    {
        0 => '-',
        > 0 => (char)('A' + count - 1),
        < 0 => (char)('a' - count - 1),
    };

    /// <summary>The cube's owner as the format spells it, from the player on roll's side.</summary>
    private static int CubePosition(CubeOwner owner) => owner switch
    {
        CubeOwner.OnRoll => 1,
        CubeOwner.Centered => 0,
        CubeOwner.Opponent => -1,
        // Unreachable: PositionData refuses an undefined owner.
        _ => throw new ArgumentOutOfRangeException(nameof(owner), owner, SessionRules.CubeOwnerMessage),
    };

    /// <summary>The exponent of a positive power of two, as the format states a cube value.</summary>
    private static int Exponent(int powerOfTwo) => BitOperations.Log2((uint)powerOfTwo);
}
