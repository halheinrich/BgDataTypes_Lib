using System.Globalization;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

public class ProblemKeyTests
{
    // -----------------------------------------------------------------------
    //  Fixtures
    //
    //  The standard start, on-roll relative (BoardState.Standard's Mop shape),
    //  and record builders whose provenance/descriptive fields are deliberate
    //  junk — the key must derive from Position/Decision facts alone.
    // -----------------------------------------------------------------------

    private const string StandardBoardToken =
        "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0";

    // Boards a decision position refuses (halheinrich/backgammon#273, Hal's
    // ruling of 2026-09-27): each is a well-formed position, and each side
    // borne off is a terminal one XG data can hold.
    private const string EmptyBoardToken =
        "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0";
    private const string OnRollBorneOffToken =
        "0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,-3,-3,-3,-2,-2,-2,0";
    private const string OpponentBorneOffToken =
        "0,3,3,3,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0";

    // Pinned wire-contract literals. These are regression pins on the
    // stats document's key grammar — a change here is a wire-format break.
    private const string PinnedPlayKey = StandardBoardToken + "/7a7/1c/31";
    private const string PinnedCubeKey = StandardBoardToken + "/5a2/2o";
    private const string PinnedCrawfordPlayKey = StandardBoardToken + "/1a3cr/1c/52";

    // Money keys carry the v3 Jacoby suffix; both values are spelled, so the
    // v2 money spelling below is simply not in the grammar (halheinrich/backgammon#120).
    private const string PinnedMoneyPlayKeyJacoby = StandardBoardToken + "/0a0j/1c/31";
    private const string PinnedMoneyPlayKeyNoJacoby = StandardBoardToken + "/0a0nj/1c/31";
    private const string PinnedMoneyCubeKey = StandardBoardToken + "/0a0j/2o";

    // The retired v2 money spelling. Pinned as a REJECTION: a v2 money key
    // must not parse under the v3 grammar (the stats document's schema
    // version retires it — SPEC-stats-identity.md §3).
    private const string RetiredV2MoneyPlayKey = StandardBoardToken + "/0a0/1c/31";

    private static int[] StandardMop() =>
        [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0];

    // Rewritten for the session kinds (halheinrich/backgammon#273): a fixture
    // states its session — a match at a standing, or money under a rule —
    // never away scores of 0 standing for money.
    private static BgDecisionData PlayDecision(
        int[]? mop = null,
        Session? session = null,
        int cubeSize = 1,
        CubeOwner cubeOwner = CubeOwner.Centered,
        int[]? dice = null,
        DescriptiveData? descriptive = null,
        DecisionId? id = null) => TestRecords.CheckerPlay(
        id: id ?? new XgDecisionId("fixture.xg", Game: 1, MoveNumber: 1, IsCube: false),
        position: TestRecords.Position(
            mop: new BoardPosition(mop ?? StandardMop()),
            cubeSize: cubeSize,
            cubeOwner: cubeOwner,
            session: session ?? Match(7, 7)),
        // The key reads the roll, never the candidates: one pass, which is
        // valid from every board a fixture here states.
        decision: TestRecords.CheckerPlayData(dice: dice ?? [3, 1], plays: [TestRecords.Candidate(play: [])]),
        descriptive: descriptive);

    private static BgDecisionData CubeDecision(
        int[]? mop = null,
        Session? session = null,
        int cubeSize = 2,
        CubeOwner cubeOwner = CubeOwner.OnRoll,
        DescriptiveData? descriptive = null,
        DecisionId? id = null) => TestRecords.Cube(
        id: id ?? new XgDecisionId("fixture.xg", Game: 1, MoveNumber: 2, IsCube: true),
        position: TestRecords.Position(
            mop: new BoardPosition(mop ?? StandardMop()),
            cubeSize: cubeSize,
            cubeOwner: cubeOwner,
            session: session ?? Match(5, 2)),
        decision: TestRecords.CubeData(),
        descriptive: descriptive);

    /// <summary>
    /// A match at this standing. Its length, which no key reads, is the larger
    /// of 7 and the away scores.
    /// </summary>
    private static MatchSession Match(int onRollNeeds, int opponentNeeds, bool isCrawford = false) =>
        TestRecords.MatchSession(
            length: Math.Max(7, Math.Max(onRollNeeds, opponentNeeds)),
            onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds, isCrawford: isCrawford);

    private static MoneySession Money(bool isJacoby) => TestRecords.MoneySession(isJacoby: isJacoby);

    /// <summary>
    /// A money checker play under a stated Jacoby rule — the shape whose key
    /// the v3 money suffix spells.
    /// </summary>
    private static BgDecisionData MoneyPlay(bool isJacoby, int[]? dice = null) =>
        PlayDecision(session: Money(isJacoby), dice: dice);

    private static ProblemKey Derive(BgDecisionData data)
    {
        Assert.True(ProblemKey.TryDerive(data, out var key));
        return key!;
    }

    // -----------------------------------------------------------------------
    //  Equality + hash code
    // -----------------------------------------------------------------------

    [Fact]
    public void Equality_SameFacts_PlayKeysEqual()
    {
        var a = Derive(PlayDecision());
        var b = Derive(PlayDecision());

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_SameFacts_CubeKeysEqual()
    {
        var a = Derive(CubeDecision());
        var b = Derive(CubeDecision());

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_BoardPerturbed_NotEqual()
    {
        // Move one checker (24-point to 23-point) — totals unchanged.
        var mop = StandardMop();
        mop[24] = 1;
        mop[23] = 1;

        Assert.NotEqual(Derive(PlayDecision()), Derive(PlayDecision(mop: mop)));
    }

    [Fact]
    public void Equality_AwayScoresPerturbed_NotEqual()
    {
        Assert.NotEqual(
            Derive(PlayDecision(session: Match(7, 7))),
            Derive(PlayDecision(session: Match(7, 5))));
    }

    [Fact]
    public void Equality_CrawfordToggle_NotEqual()
    {
        Assert.NotEqual(
            Derive(PlayDecision(session: Match(1, 3))),
            Derive(PlayDecision(session: Match(1, 3, isCrawford: true))));
    }

    [Fact]
    public void Equality_CubeSizePerturbed_NotEqual()
    {
        Assert.NotEqual(
            Derive(CubeDecision(cubeSize: 2)),
            Derive(CubeDecision(cubeSize: 4)));
    }

    [Fact]
    public void Equality_CubeOwnerPerturbed_NotEqual()
    {
        // Cube state participates in checker-play identity too (ruled) —
        // perturb the owner on a play key.
        Assert.NotEqual(
            Derive(PlayDecision(cubeSize: 2, cubeOwner: CubeOwner.OnRoll)),
            Derive(PlayDecision(cubeSize: 2, cubeOwner: CubeOwner.Opponent)));
    }

    [Fact]
    public void Equality_DicePerturbed_NotEqual()
    {
        Assert.NotEqual(
            Derive(PlayDecision(dice: [3, 1])),
            Derive(PlayDecision(dice: [4, 1])));
    }

    [Fact]
    public void Equality_PlayAndCubeKeys_NeverEqual()
    {
        // Same position facts; the kind discriminant (dice presence) splits them.
        var play = Derive(PlayDecision(
            session: Match(5, 2), cubeSize: 2, cubeOwner: CubeOwner.OnRoll));
        var cube = Derive(CubeDecision());

        Assert.NotEqual(play, cube);
    }

    [Fact]
    public void Equality_NullHandling()
    {
        var key = Derive(PlayDecision());

        Assert.False(key.Equals(null));
        Assert.False(key == null);
        Assert.False(null == key);
        Assert.True((ProblemKey?)null == null);
        Assert.True(key != null);
    }

    // -----------------------------------------------------------------------
    //  Canonical string form — pinned wire-contract literals
    // -----------------------------------------------------------------------

    [Fact]
    public void CanonicalForm_PlayKey_Pinned()
    {
        Assert.Equal(PinnedPlayKey, Derive(PlayDecision()).ToString());
    }

    [Fact]
    public void CanonicalForm_CubeKey_Pinned()
    {
        Assert.Equal(PinnedCubeKey, Derive(CubeDecision()).ToString());
    }

    [Fact]
    public void CanonicalForm_CrawfordPlayKey_Pinned()
    {
        var key = Derive(PlayDecision(
            session: Match(1, 3, isCrawford: true), dice: [5, 2]));

        Assert.Equal(PinnedCrawfordPlayKey, key.ToString());
    }

    [Fact]
    public void CanonicalForm_MoneyPlayKey_Pinned()
    {
        var jacoby = Derive(MoneyPlay(isJacoby: true));
        var noJacoby = Derive(MoneyPlay(isJacoby: false));

        Assert.Equal(PinnedMoneyPlayKeyJacoby, jacoby.ToString());
        Assert.Equal(PinnedMoneyPlayKeyNoJacoby, noJacoby.ToString());
    }

    [Fact]
    public void CanonicalForm_MoneyCubeKey_Pinned()
    {
        // The suffix rides the score field, so it is orthogonal to the kind
        // discriminant: a money cube key carries it and still has no dice.
        var key = Derive(CubeDecision(session: Money(true)));

        Assert.Equal(PinnedMoneyCubeKey, key.ToString());
        Assert.True(key.IsCubeDecision);
    }

    [Fact]
    public void CanonicalForm_DiceStampedInRolledOrder_Canonicalized()
    {
        // Producers stamp dice in rolled order; the key carries them
        // canonically unordered — 1-3 and 3-1 are the same problem.
        Assert.Equal(Derive(PlayDecision(dice: [1, 3])), Derive(PlayDecision(dice: [3, 1])));
    }

    [Fact]
    public void CanonicalForm_KindDiscriminant()
    {
        Assert.False(Derive(PlayDecision()).IsCubeDecision);
        Assert.True(Derive(CubeDecision()).IsCubeDecision);
    }

    // -----------------------------------------------------------------------
    //  Parse / TryParse — round-trips
    // -----------------------------------------------------------------------

    [Fact]
    public void RoundTrip_PlayKey()
    {
        var derived = Derive(PlayDecision());
        var parsed = ProblemKey.Parse(derived.ToString());

        Assert.Equal(derived, parsed);
        Assert.Equal(derived.ToString(), parsed.ToString());
        Assert.False(parsed.IsCubeDecision);
    }

    [Fact]
    public void RoundTrip_CubeKey()
    {
        var derived = Derive(CubeDecision());
        var parsed = ProblemKey.Parse(derived.ToString());

        Assert.Equal(derived, parsed);
        Assert.Equal(derived.ToString(), parsed.ToString());
        Assert.True(parsed.IsCubeDecision);
    }

    [Fact]
    public void RoundTrip_CrawfordAndMoneyKeys()
    {
        Assert.Equal(PinnedCrawfordPlayKey, ProblemKey.Parse(PinnedCrawfordPlayKey).ToString());
        Assert.Equal(
            PinnedMoneyPlayKeyJacoby, ProblemKey.Parse(PinnedMoneyPlayKeyJacoby).ToString());
        Assert.Equal(
            PinnedMoneyPlayKeyNoJacoby, ProblemKey.Parse(PinnedMoneyPlayKeyNoJacoby).ToString());
        Assert.Equal(PinnedMoneyCubeKey, ProblemKey.Parse(PinnedMoneyCubeKey).ToString());
    }

    [Fact]
    public void RoundTrip_MoneyKeys_DeriveParseDerive()
    {
        // Full circuit for both Jacoby values: derive → string → parse →
        // equal key, same kind.
        foreach (bool jacoby in new[] { true, false })
        {
            var derived = Derive(MoneyPlay(isJacoby: jacoby));
            var parsed = ProblemKey.Parse(derived.ToString());

            Assert.Equal(derived, parsed);
            Assert.Equal(derived.ToString(), parsed.ToString());
            Assert.False(parsed.IsCubeDecision);
        }
    }

    [Fact]
    public void RoundTrip_SpanOverloadsMatchStringOverloads()
    {
        var fromString = ProblemKey.Parse(PinnedPlayKey);
        var fromSpan = ProblemKey.Parse(PinnedPlayKey.AsSpan());

        Assert.Equal(fromString, fromSpan);
        Assert.True(ProblemKey.TryParse(PinnedCubeKey.AsSpan(), null, out var spanTry));
        Assert.Equal(ProblemKey.Parse(PinnedCubeKey), spanTry);
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => ProblemKey.Parse((string)null!));
    }

    [Fact]
    public void TryParse_Null_False()
    {
        Assert.False(ProblemKey.TryParse(null, null, out var key));
        Assert.Null(key);
    }

    [Fact]
    public void Parse_Invalid_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ProblemKey.Parse("not-a-key"));
    }

    // -----------------------------------------------------------------------
    //  Strict parse — exactly one spelling per value
    //
    //  Pinned contract: the parse door accepts only the exact canonical
    //  spelling (deliberate divergence from DiceRoll's lenient parse), and
    //  applies the same fact validation as TryDerive.
    // -----------------------------------------------------------------------

    [Theory]
    // Field shape
    [InlineData("")]
    [InlineData(StandardBoardToken)]                        // board only
    [InlineData(StandardBoardToken + "/7a7")]               // no cube field
    [InlineData(StandardBoardToken + "/7a7/1c/31/x")]       // fifth field
    [InlineData(StandardBoardToken + "/7a7/1c/")]           // empty dice field
    [InlineData(StandardBoardToken + "/7a7/1c/31 ")]        // trailing whitespace
    [InlineData(" " + StandardBoardToken + "/7a7/1c/31")]   // leading whitespace
    // Board spelling
    [InlineData("0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2/7a7/1c/31")]     // 25 entries
    [InlineData("0,0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")] // 27 entries
    [InlineData("0,-2,0,0,0,0,05,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]  // leading zero
    [InlineData("0,-2,0,0,0,0,+5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]  // explicit plus
    [InlineData("0,-2,0,0,0,0, 5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]  // internal space
    // Score spelling
    [InlineData(StandardBoardToken + "/77/1c/31")]          // missing 'a'
    [InlineData(StandardBoardToken + "/7A7/1c/31")]         // uppercase separator
    [InlineData(StandardBoardToken + "/07a7/1c/31")]        // leading zero
    [InlineData(StandardBoardToken + "/-1a7/1c/31")]        // negative away
    [InlineData(StandardBoardToken + "/1a3CR/1c/52")]       // uppercase crawford
    // Cube spelling
    [InlineData(StandardBoardToken + "/7a7/c1/31")]         // owner-first
    [InlineData(StandardBoardToken + "/7a7/1x/31")]         // unknown owner letter
    [InlineData(StandardBoardToken + "/7a7/1C/31")]         // uppercase owner
    [InlineData(StandardBoardToken + "/7a7/c/31")]          // no size digits
    // Dice spelling
    [InlineData(StandardBoardToken + "/7a7/1c/13")]         // low-first spelling
    [InlineData(StandardBoardToken + "/7a7/1c/3")]          // one digit
    [InlineData(StandardBoardToken + "/7a7/1c/315")]        // three digits
    [InlineData(StandardBoardToken + "/7a7/1c/07")]         // invalid faces
    public void TryParse_NonCanonicalSpelling_Rejected(string input)
    {
        Assert.False(ProblemKey.TryParse(input, null, out _));
    }

    [Theory]
    // Fact validation at the string door — same rungs as TryDerive.
    [InlineData(EmptyBoardToken + "/7a7/1c/31")]            // empty board
    [InlineData("0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,3,0/7a7/1c/31")]    // 16 on-roll checkers
    [InlineData("0,-3,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]    // 16 opponent checkers
    [InlineData("1,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,1,0/7a7/1c/31")]    // on-roll checker on opponent bar
    [InlineData("0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-4,0,0,0,0,2,-1/7a7/1c/31")]   // opponent checker on on-roll bar
    [InlineData("0,-16,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]   // per-point out of range
    [InlineData(StandardBoardToken + "/0a7/1c/31")]         // one-sided zero away
    [InlineData(StandardBoardToken + "/7a0/1c/31")]         // one-sided zero away
    [InlineData(StandardBoardToken + "/0a0crj/1c/31")]      // crawford in money
    [InlineData(StandardBoardToken + "/3a2cr/1c/31")]       // crawford with no 1-away side
    [InlineData(StandardBoardToken + "/7a7/0c/31")]         // cube size zero
    [InlineData(StandardBoardToken + "/7a7/3c/31")]         // cube size not a power of two
    public void TryParse_InvalidFacts_Rejected(string input)
    {
        Assert.False(ProblemKey.TryParse(input, null, out _));
    }

    // -----------------------------------------------------------------------
    //  Derivation from decision-record facts
    // -----------------------------------------------------------------------

    [Fact]
    public void TryDerive_IgnoresProvenanceXgidAndDescriptive()
    {
        // The game and move number are the Id's (halheinrich/backgammon#124),
        // so the Id is what differs in them now.
        var a = PlayDecision(
            id: new XgDecisionId("one.xg", Game: 1, MoveNumber: 4, IsCube: false),
            descriptive: TestRecords.Descriptive(
                onRollName: "Alice",
                opponentName: "Bob"));
        var b = PlayDecision(
            id: new XgpDecisionId("two.xgp"),
            descriptive: TestRecords.Descriptive(
                onRollName: "Carol",
                opponentName: "Dave",
                isStandardStart: null));

        Assert.Equal(Derive(a), Derive(b));
    }

    [Fact]
    public void TryDerive_CollapseCase_SameAwayDifferentMatchLength_EqualKeys()
    {
        // Spec §1 consequence, POSITIVE fixture: 3-away/2-away is the same
        // problem whether the match is to 7 or to 11 — match length is
        // subsumed by away scores and must not participate. Rewritten: the
        // length is the match session's own now, not the descriptive
        // category's.
        var shortMatch = PlayDecision(
            session: TestRecords.MatchSession(length: 7, onRollNeeds: 3, opponentNeeds: 2));
        var longMatch = PlayDecision(
            session: TestRecords.MatchSession(length: 11, onRollNeeds: 3, opponentNeeds: 2));

        Assert.Equal(Derive(shortMatch), Derive(longMatch));
    }

    [Fact]
    public void TryDerive_CollapseCase_MirrorTurnDuplicates_EqualKeys()
    {
        // Spec §1 consequence, POSITIVE fixture: the same problem recorded
        // with the seats swapped presents identical on-roll-relative facts —
        // turn/seat is normalized away by the Mop convention, so only the
        // descriptive frame differs and the keys must unify.
        var seatsA = CubeDecision(
            id: new XgDecisionId("m.xg", Game: 2, MoveNumber: 6, IsCube: true),
            descriptive: TestRecords.Descriptive(onRollName: "Alice", opponentName: "Bob"));
        var seatsB = CubeDecision(
            id: new XgDecisionId("m.xg", Game: 5, MoveNumber: 11, IsCube: true),
            descriptive: TestRecords.Descriptive(onRollName: "Bob", opponentName: "Alice"));

        Assert.Equal(Derive(seatsA), Derive(seatsB));
    }

    [Fact]
    public void TryDerive_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ProblemKey.TryDerive(null!, out _));
    }

    // -----------------------------------------------------------------------
    //  The no-key rung — TryDerive returns false, never throws, on
    //  malformed / degenerate / inconsistent facts
    // -----------------------------------------------------------------------

    [Fact]
    public void UnstampedDiceOnPlay_CannotBeBuilt()
    {
        // Rewritten from NoKey_UnstampedDiceOnPlay. The spec's named case — a
        // checker play whose dice were never stamped, the [0,0] a cube
        // carried — no longer reaches the no-key rung: a checker play's roll
        // is two faces 1-6 by construction, so the record cannot exist and
        // the key never guesses.
        Assert.Throws<ArgumentOutOfRangeException>(() => PlayDecision(dice: [0, 0]));
    }

    [Theory]
    [InlineData(new[] { 7, 1 })]
    [InlineData(new[] { 3 })]
    [InlineData(new[] { 3, 1, 2 })]
    [InlineData(new int[0])]
    public void MalformedDiceOnPlay_CannotBeBuilt(int[] dice)
    {
        // Rewritten from NoKey_MalformedDiceOnPlay, as above.
        Assert.ThrowsAny<ArgumentException>(() => PlayDecision(dice: dice));
    }

    [Fact]
    public void NullDiceListOnPlay_IsRefused()
    {
        // Rewritten from NoKey_NullDiceListOnPlay: a present null list is
        // refused at init now, and read through the wire unit it is a
        // JsonException on both paths — so the key never meets it.
        var ex = Assert.Throws<ArgumentNullException>(() => new CheckerPlayDecisionData
        {
            Dice = null!,
            Plays = [TestRecords.Candidate()],
        });
        Assert.Equal("Dice", ex.ParamName);

        var document = WirePaths.Document<BgDecisionData>(PlayDecision());
        document["Decision"]!["Dice"] = null;
        WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
    }

    // The empty-board rung below was a TryDerive pin until a decision position
    // came to hold a checker of each side (halheinrich/backgammon#273, Hal's
    // ruling of 2026-09-27): a record can no longer stand on the empty board,
    // or on one with a side borne off, so TryDerive never meets either. The
    // key's text keeps its own rule, and the two part here: the empty board's
    // text still does not parse, while a side borne off still does, so every
    // statistics document that read before the ruling still reads.

    [Fact]
    public void EmptyBoard_CannotBeBuilt_AndItsKeyDoesNotParse()
    {
        // Rewritten from NoKey_EmptyBoard.
        foreach (var build in new Func<BgDecisionData>[] { () => PlayDecision(mop: new int[26]), () => CubeDecision(mop: new int[26]) })
            Assert.Equal("Mop", Assert.Throws<ArgumentException>(build).ParamName);

        Assert.True(ProblemKey.TryParse(StandardBoardToken + "/7a7/1c/31", null, out _));
        Assert.False(ProblemKey.TryParse(EmptyBoardToken + "/7a7/1c/31", null, out _));
        Assert.False(ProblemKey.TryParse(EmptyBoardToken + "/5a2/2o", null, out _));
    }

    [Theory]
    [InlineData(OnRollBorneOffToken)]
    [InlineData(OpponentBorneOffToken)]
    public void ASideBorneOff_CannotBeBuilt_ButItsKeyStillParses(string boardToken)
    {
        // Added (control): the record refuses the board, and the key's parse
        // door does not follow it — a key a record could produce before the
        // ruling still reads back, as a play key and as a cube key.
        int[] mop = [.. boardToken.Split(',').Select(c => int.Parse(c, CultureInfo.InvariantCulture))];
        Assert.True(BoardPosition.TryCreate(mop, out _));
        Assert.Equal("Mop", Assert.Throws<ArgumentException>(() => PlayDecision(mop: mop)).ParamName);
        Assert.Equal("Mop", Assert.Throws<ArgumentException>(() => CubeDecision(mop: mop)).ParamName);

        foreach (string key in new[] { boardToken + "/7a7/1c/31", boardToken + "/0a0j/2o" })
        {
            Assert.True(ProblemKey.TryParse(key, null, out var parsed), key);
            Assert.Equal(key, parsed.ToString());
        }
    }

    // The four malformed-board rungs below were TryDerive pins until the
    // board became a BoardPosition: a record can no longer hold a malformed
    // board, so TryDerive never meets one. Each is rewritten to pin the
    // refusal at the two doors that remain — the board's own (a record
    // cannot be built with it) and the key's parse door (a key string
    // spelling it yields no key) — against the same key with the standard
    // board, which parses.

    private static void AssertBoardRefusedAtEveryDoor(int[] mop)
    {
        Assert.False(BoardPosition.TryCreate(mop, out _));
        Assert.Throws<ArgumentException>(() => PlayDecision(mop: mop));

        string boardToken = string.Join(",", mop.Select(c => c.ToString(CultureInfo.InvariantCulture)));
        Assert.True(ProblemKey.TryParse(StandardBoardToken + "/7a7/1c/31", null, out _));
        Assert.False(ProblemKey.TryParse(boardToken + "/7a7/1c/31", null, out _));
    }

    [Fact]
    public void MalformedBoardShape_RefusedAtEveryDoor()
    {
        // Rewritten from NoKey_MalformedBoardShape. Its null-board half has
        // no successor: the board is a value type, so a record cannot hold a
        // null one.
        AssertBoardRefusedAtEveryDoor(new int[25]);
        AssertBoardRefusedAtEveryDoor([.. StandardMop(), 0]);
    }

    [Fact]
    public void PerPointCountOutOfRange_RefusedAtEveryDoor()
    {
        // Rewritten from NoKey_PerPointCountOutOfRange.
        var mop = StandardMop();
        mop[6] = 16;
        AssertBoardRefusedAtEveryDoor(mop);
    }

    [Fact]
    public void MoreThanFifteenCheckersPerSide_RefusedAtEveryDoor()
    {
        // Rewritten from NoKey_MoreThanFifteenCheckersPerSide. Real-board
        // posture: totals capped at 15 per side even when every individual
        // point is in range.
        var onRollHeavy = StandardMop();
        onRollHeavy[24] = 3;        // positives now 16
        AssertBoardRefusedAtEveryDoor(onRollHeavy);

        var opponentHeavy = StandardMop();
        opponentHeavy[1] = -3;      // |negatives| now 16
        AssertBoardRefusedAtEveryDoor(opponentHeavy);
    }

    [Fact]
    public void CheckerOnWrongBar_RefusedAtEveryDoor()
    {
        // Rewritten from NoKey_CheckerOnWrongBar. Each bar holds only its own
        // side's checkers: Mop[0] <= 0 (opponent bar), Mop[25] >= 0 (on-roll
        // bar). Totals kept at 15 so only the bar-sign rule can reject.
        var onRollOnOpponentBar = StandardMop();
        onRollOnOpponentBar[0] = 1;
        onRollOnOpponentBar[24] = 1;
        AssertBoardRefusedAtEveryDoor(onRollOnOpponentBar);

        var opponentOnOnRollBar = StandardMop();
        opponentOnOnRollBar[25] = -1;
        opponentOnOnRollBar[19] = -4;
        AssertBoardRefusedAtEveryDoor(opponentOnOnRollBar);
    }

    // The score rungs below were TryDerive pins until money and match became
    // the session's kinds (halheinrich/backgammon#273): a record can no longer
    // hold such a score, so TryDerive never meets one. Each is rewritten, as
    // the malformed boards were, to pin the refusal at the two doors that
    // remain — the session's own and the key's parse door.

    [Theory]
    [InlineData(-1, 7)]
    [InlineData(7, -1)]
    [InlineData(0, 7)]      // one-sided zero: money is its own kind, not 0/0
    [InlineData(7, 0)]
    [InlineData(0, 0)]      // the old money stand-in, in a match
    public void InvalidAwayScores_CannotBeBuilt_AndTheirKeysDoNotParse(int onRollNeeds, int opponentNeeds)
    {
        // Rewritten from NoKey_InvalidAwayScores.
        Assert.Throws<ArgumentOutOfRangeException>(() => Match(onRollNeeds, opponentNeeds));
        if (onRollNeeds != 0 || opponentNeeds != 0)
            Assert.False(ProblemKey.TryParse(StandardBoardToken + $"/{onRollNeeds}a{opponentNeeds}/1c/31", null, out _));
    }

    [Fact]
    public void CrawfordWithNoPlayerAtMatchPoint_CannotBeBuilt_AndItsKeyDoesNotParse()
    {
        // Rewritten from NoKey_InconsistentCrawford(3, 2).
        var ex = Assert.Throws<ArgumentException>(() => Match(3, 2, isCrawford: true));
        Assert.Contains("1-away", ex.Message);
        Assert.False(ProblemKey.TryParse(StandardBoardToken + "/3a2cr/1c/31", null, out _));
    }

    [Fact]
    public void CrawfordInMoney_IsNotExpressible_AndItsKeyDoesNotParse()
    {
        // Rewritten from NoKey_InconsistentCrawford(0, 0): a money session
        // has no Crawford game, so there is no member to state one in.
        Assert.Null(typeof(MoneySession).GetProperty("IsCrawford"));
        Assert.False(ProblemKey.TryParse(StandardBoardToken + "/0a0crj/1c/31", null, out _));
    }

    // The two cube rungs below were TryDerive pins until the position held
    // its cube to the rules the derived XGID needs (halheinrich/backgammon#273):
    // a record can no longer hold such a cube, so each is rewritten to pin
    // the refusal at the two doors that remain.

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(3)]
    [InlineData(6)]
    public void InvalidCubeSize_CannotBeBuilt_AndItsKeyDoesNotParse(int cubeSize)
    {
        // Rewritten from NoKey_InvalidCubeSize.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CubeDecision(cubeSize: cubeSize));
        Assert.Equal("CubeSize", ex.ParamName);
        Assert.False(ProblemKey.TryParse(StandardBoardToken + $"/5a2/{cubeSize}o", null, out _));
    }

    [Fact]
    public void UndefinedCubeOwner_CannotBeBuilt_AndNoKeySpellsIt()
    {
        // Rewritten from NoKey_UndefinedCubeOwner.
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CubeDecision(cubeOwner: (CubeOwner)99));
        Assert.Equal("CubeOwner", ex.ParamName);
        Assert.False(ProblemKey.TryParse(StandardBoardToken + "/5a2/2x", null, out _));
    }

    // -----------------------------------------------------------------------
    //  The Jacoby fact — money-only identity (SPEC-stats-identity.md §1/§2,
    //  amended 2026-08-20, halheinrich/backgammon#120)
    //
    //  Three postures, pinned: money keys spell the fact and split on it;
    //  match keys ignore it and stay byte-identical to v2; a money record
    //  that does not carry it gets no key.
    // -----------------------------------------------------------------------

    [Fact]
    public void Jacoby_MoneyKeys_ToggleSplitsIdentity()
    {
        var jacoby = Derive(MoneyPlay(isJacoby: true));
        var noJacoby = Derive(MoneyPlay(isJacoby: false));

        Assert.NotEqual(jacoby, noJacoby);
        Assert.NotEqual(jacoby.GetHashCode(), noJacoby.GetHashCode());
    }

    [Fact]
    public void Jacoby_MoneyCubeKeys_ToggleSplitsIdentity()
    {
        Assert.NotEqual(
            Derive(CubeDecision(session: Money(true))),
            Derive(CubeDecision(session: Money(false))));
    }

    [Fact]
    public void Jacoby_AMatchStatesNoJacobyFact_SoNoneCanReachItsKey()
    {
        // Rewritten from Jacoby_MatchKeys_FactIsIgnored, which pinned a stamp
        // on a match record being ignored by the key. Off money the question
        // does not arise, and a match now has no member to state it in: the
        // match session has none, and a match document carrying one is
        // refused rather than read with the stamp dropped.
        Assert.Null(typeof(MatchSession).GetProperty("IsJacoby"));
        Assert.Null(typeof(PositionData).GetProperty("IsJacoby"));

        var document = WirePaths.Document<BgDecisionData>(PlayDecision());
        document["Position"]!["Session"]!["IsJacoby"] = true;
        WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
    }

    [Fact]
    public void Jacoby_MatchKeys_ByteIdenticalToV2()
    {
        // The v3 grammar's compatibility pin: every match key's canonical
        // string is exactly what v2 emitted. The literals are the v2
        // wire-contract pins declared above. Rewritten from
        // Jacoby_MatchKeys_ByteIdenticalToV2(isJacoby): a match states no
        // Jacoby fact to vary.
        Assert.Equal(
            PinnedPlayKey,
            Derive(PlayDecision()).ToString());
        Assert.Equal(
            PinnedCubeKey,
            Derive(CubeDecision()).ToString());
        Assert.Equal(
            PinnedCrawfordPlayKey,
            Derive(PlayDecision(session: Match(1, 3, isCrawford: true), dice: [5, 2])).ToString());
    }

    [Fact]
    public void AMoneyRecordWithoutItsJacobyRule_CannotBeRead_AndTheV2MoneyKeyDoesNotParse()
    {
        // Rewritten from NoKey_MoneyRecordWithoutJacobyFact. The underivable
        // rung is gone with the state it guarded: a money session states its
        // rule (required, so omitting it does not compile), and a money
        // document without it is refused on both paths — no money record is
        // under an unknown rule, so none is left without a key for one. The
        // text a key without the rule would be is still not in the grammar.
        foreach (var record in new[] { MoneyPlay(isJacoby: true), CubeDecision(session: Money(true)) })
        {
            var document = WirePaths.Document(record);
            document["Position"]!["Session"]!["Terms"]!.AsObject().Remove("IsJacoby");
            WirePaths.AssertRefused<BgDecisionData>(document.ToJsonString());
        }
        Assert.False(ProblemKey.TryParse(RetiredV2MoneyPlayKey, null, out _));
    }

    [Fact]
    public void Jacoby_TheKeyAndTheXgid_SpellTheOneRule()
    {
        // Rewritten from Jacoby_DerivationNeverReadsTheXgidString, which pinned
        // the key ignoring a stored XGID that could contradict the record's
        // Jacoby fact. The XGID is derived from the record now
        // (halheinrich/backgammon#273), so no XGID can contradict it: the key's
        // suffix and bit 0 of the XGID's field 8 both spell the money
        // session's one rule, and a different rule changes both.
        foreach (bool isJacoby in new[] { true, false })
        {
            var record = MoneyPlay(isJacoby);
            int crawfordJacoby = int.Parse(record.Xgid.Split(':')[7], CultureInfo.InvariantCulture);

            Assert.Equal(isJacoby, (crawfordJacoby & 1) == 1);
            Assert.EndsWith(isJacoby ? "/0a0j/1c/31" : "/0a0nj/1c/31", Derive(record).ToString());
        }
    }

    [Fact]
    public void TryParse_RetiredV2MoneySpelling_Rejected()
    {
        // The v2 money key is not in the v3 grammar — it must fail parse
        // rather than read back as "Jacoby off". The stats document's schema
        // version is what retires the v2 file (SPEC-stats-identity.md §3).
        Assert.False(ProblemKey.TryParse(RetiredV2MoneyPlayKey, null, out _));
        Assert.Throws<FormatException>(() => ProblemKey.Parse(RetiredV2MoneyPlayKey));

        // …and the same for a v2 money cube key (no dice field).
        Assert.False(ProblemKey.TryParse(StandardBoardToken + "/0a0/2o", null, out _));
    }

    [Theory]
    // Match keys never carry the suffix.
    [InlineData(StandardBoardToken + "/7a7j/1c/31")]
    [InlineData(StandardBoardToken + "/7a7nj/1c/31")]
    [InlineData(StandardBoardToken + "/1a3crj/1c/52")]
    // One spelling per value, on the money suffix too.
    [InlineData(StandardBoardToken + "/0a0J/1c/31")]        // uppercase
    [InlineData(StandardBoardToken + "/0a0NJ/1c/31")]       // uppercase
    [InlineData(StandardBoardToken + "/0a0n/1c/31")]        // truncated "nj"
    [InlineData(StandardBoardToken + "/0a0jj/1c/31")]       // doubled token
    [InlineData(StandardBoardToken + "/0a0njj/1c/31")]      // both tokens
    [InlineData(StandardBoardToken + "/0a0jnj/1c/31")]      // both tokens
    [InlineData(StandardBoardToken + "/0a0jcr/1c/31")]      // token order
    [InlineData(StandardBoardToken + "/0a0 j/1c/31")]       // internal space
    public void TryParse_NonCanonicalJacobySpelling_Rejected(string input)
    {
        Assert.False(ProblemKey.TryParse(input, null, out _));
    }

    [Fact]
    public void Json_MoneyKeyRoundTripsWithSuffix()
    {
        var key = Derive(MoneyPlay(isJacoby: true));

        string json = JsonSerializer.Serialize(key);
        Assert.Equal($"\"{PinnedMoneyPlayKeyJacoby}\"", json);
        Assert.Equal(key, JsonSerializer.Deserialize<ProblemKey>(json));
    }

    // -----------------------------------------------------------------------
    //  Ordering — ordinal over the canonical string, arbitrary but stable
    // -----------------------------------------------------------------------

    [Fact]
    public void CompareTo_MatchesOrdinalStringOrder()
    {
        var play = Derive(PlayDecision());
        var cube = Derive(CubeDecision());

        Assert.Equal(
            Math.Sign(string.CompareOrdinal(play.ToString(), cube.ToString())),
            Math.Sign(play.CompareTo(cube)));
        Assert.Equal(0, play.CompareTo(Derive(PlayDecision())));
        Assert.True(play.CompareTo(null) > 0);
    }

    [Fact]
    public void CompareTo_NonGeneric_WrongTypeThrows()
    {
        IComparable key = Derive(PlayDecision());

        Assert.Equal(1, key.CompareTo(null));
        Assert.Throws<ArgumentException>(() => key.CompareTo("a string"));
    }

    [Fact]
    public void Sorting_IsDeterministic()
    {
        var keys = new List<ProblemKey>
        {
            Derive(CubeDecision()),
            Derive(PlayDecision()),
            Derive(PlayDecision(dice: [6, 5])),
        };
        var expected = keys.OrderBy(k => k.ToString(), StringComparer.Ordinal).ToList();

        keys.Sort();

        Assert.Equal(expected, keys);
    }

    // -----------------------------------------------------------------------
    //  JSON — bundled converter, no consumer-side registration
    // -----------------------------------------------------------------------

    [Fact]
    public void Json_RoundTripsAsCanonicalString()
    {
        var key = Derive(PlayDecision());

        string json = JsonSerializer.Serialize(key);
        Assert.Equal($"\"{PinnedPlayKey}\"", json);

        var back = JsonSerializer.Deserialize<ProblemKey>(json);
        Assert.Equal(key, back);
    }

    [Fact]
    public void Json_NullRoundTrips()
    {
        Assert.Null(JsonSerializer.Deserialize<ProblemKey?>("null"));
    }

    [Theory]
    [InlineData("\"not-a-key\"")]
    [InlineData("42")]
    [InlineData("{}")]
    public void Json_InvalidInput_Throws(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProblemKey>(json));
    }

    [Fact]
    public void Json_WorksAsDictionaryKey()
    {
        // The stats document keys its per-problem map by ProblemKey — the
        // converter's property-name overloads must round-trip it.
        var map = new Dictionary<ProblemKey, int>
        {
            [Derive(PlayDecision())] = 3,
            [Derive(CubeDecision())] = 5,
        };

        string json = JsonSerializer.Serialize(map);
        var back = JsonSerializer.Deserialize<Dictionary<ProblemKey, int>>(json);

        Assert.NotNull(back);
        Assert.Equal(2, back.Count);
        Assert.Equal(3, back[Derive(PlayDecision())]);
        Assert.Equal(5, back[Derive(CubeDecision())]);
    }

    // -----------------------------------------------------------------------
    //  Culture invariance — proven, not assumed
    //
    //  Tests run on Windows .NET while a consumer runs browser-wasm; the
    //  canonical form must be byte-identical under any ambient culture.
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("de-DE")]
    public void CanonicalForm_IsCultureInvariant(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Assert.Equal(PinnedPlayKey, Derive(PlayDecision()).ToString());
            Assert.Equal(PinnedCubeKey, Derive(CubeDecision()).ToString());
            Assert.Equal(
                PinnedCrawfordPlayKey,
                Derive(PlayDecision(
                    session: Match(1, 3, isCrawford: true), dice: [5, 2]))
                    .ToString());

            Assert.Equal(
                PinnedMoneyPlayKeyJacoby, Derive(MoneyPlay(isJacoby: true)).ToString());
            Assert.Equal(
                PinnedMoneyPlayKeyNoJacoby, Derive(MoneyPlay(isJacoby: false)).ToString());

            Assert.Equal(ProblemKey.Parse(PinnedPlayKey), Derive(PlayDecision()));
            Assert.Equal(PinnedCubeKey, ProblemKey.Parse(PinnedCubeKey).ToString());
            Assert.Equal(
                PinnedMoneyPlayKeyJacoby,
                ProblemKey.Parse(PinnedMoneyPlayKeyJacoby).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
