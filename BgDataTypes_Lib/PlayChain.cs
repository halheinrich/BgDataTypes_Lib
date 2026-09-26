namespace BgDataTypes_Lib;

/// <summary>
/// One chain of a <see cref="CanonicalPlay"/>: a route from a source point to
/// a landing point, which the play's notation writes as one <c>from/to</c>.
/// It joins consecutive moves where one ends and the next begins, eliding
/// the touch-down points between, and stops where those moves stop or at a
/// hit point whose mark it carries. A chain is therefore not a checker's
/// whole trajectory for the turn: an intermediate hit splits one trajectory
/// into two chains (13/10*/8 is written 13/10* 10/8).
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
/// 8/1* with 5-2). A chain may span several moves (13/10 followed by 10/8
/// collapses to the chain 13/8). A hit can only ever sit at a chain's
/// endpoint, and each hit point's mark sits on exactly one chain, its
/// carrier: canonicalization never joins the carrier across the point it
/// hits, so the hit stays visible — see <see cref="CanonicalPlay"/> for the
/// collapse and hit-attribution rules.
///
/// The record-struct equality serves display grouping (the "(2)" of
/// <see cref="CanonicalPlay.ToString"/> for identical chains) and is not play
/// identity, which is <see cref="BoardState.IsSamePlay"/>.
/// </summary>
internal readonly record struct PlayChain(int FrPt, int ToPt);
