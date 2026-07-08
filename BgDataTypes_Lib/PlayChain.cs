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
/// Unlike a <see cref="Move"/>, which is always a single-die hop, a chain may
/// span several dice (13/10 followed by 10/8 collapses to the chain 13/8). A
/// hit can only ever sit at a chain's endpoint: canonicalization splits a
/// trajectory at any intermediate hit so the hit stays visible — see
/// <see cref="CanonicalPlay"/> for the collapse rules.
/// </summary>
public readonly record struct PlayChain(int FrPt, int ToPt);
