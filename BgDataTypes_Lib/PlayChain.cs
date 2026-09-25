namespace BgDataTypes_Lib;

/// <summary>
/// One chain of a <see cref="CanonicalPlay"/>: a single checker's collapsed
/// trajectory for the turn — its source point and final landing point, with
/// intermediate touch-down points elided.
///
/// Sibling encoding to <see cref="Move"/>:
///   FrPt: source point (1-24 on board, 25 for bar entry).
///   ToPt: destination point, sign-encoded:
///     Positive (1-24): regular landing on that point.
///     Zero: bear off (checker removed from board).
///     Negative (-1 to -24): hit — land on |ToPt| and send opponent blot to bar.
///
/// A <see cref="Move"/> is one hop of one checker — usually a single die, but
/// an encoding may also carry a multi-die move (XG data stores some, such as
/// 8/1* with 5-2). A chain joins consecutive moves of one checker where they
/// meet, so it may span several of them (13/10 followed by 10/8 collapses to
/// the chain 13/8). A hit can only ever sit at a chain's endpoint, and each hit
/// point's mark sits on exactly one chain, its carrier: canonicalization
/// never joins the carrier across the point it hits, so the hit stays
/// visible — see <see cref="CanonicalPlay"/> for the collapse and
/// hit-attribution rules.
///
/// The record-struct equality serves display grouping (the formatter's "(2)"
/// for identical chains) and is not play identity, which is
/// <see cref="BoardState.IsSamePlay"/>.
/// </summary>
public readonly record struct PlayChain(int FrPt, int ToPt);
