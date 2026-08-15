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

    // Pinned wire-contract literals. These are regression pins on the v2
    // stats document's key grammar — a change here is a wire-format break.
    private const string PinnedPlayKey = StandardBoardToken + "/7a7/1c/31";
    private const string PinnedCubeKey = StandardBoardToken + "/5a2/2o";
    private const string PinnedCrawfordPlayKey = StandardBoardToken + "/1a3cr/1c/52";
    private const string PinnedMoneyPlayKey = StandardBoardToken + "/0a0/1c/31";

    private static int[] StandardMop() =>
        [0, -2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, -5, 5, 0, 0, 0, -3, 0, -5, 0, 0, 0, 0, 2, 0];

    private static BgDecisionData PlayDecision(
        int[]? mop = null,
        int onRollNeeds = 7,
        int opponentNeeds = 7,
        bool isCrawford = false,
        int cubeSize = 1,
        CubeOwner cubeOwner = CubeOwner.Centered,
        int[]? dice = null,
        DescriptiveData? descriptive = null) => new()
    {
        Id = new XgpDecisionId("fixture.xgp"),
        Xgid = "XGID=not-consulted-by-derivation",
        Position = new PositionData
        {
            Mop = mop ?? StandardMop(),
            OnRollNeeds = onRollNeeds,
            OpponentNeeds = opponentNeeds,
            IsCrawford = isCrawford,
            CubeSize = cubeSize,
            CubeOwner = cubeOwner,
        },
        Decision = new DecisionData { IsCube = false, Dice = dice ?? [3, 1] },
        Descriptive = descriptive ?? new DescriptiveData(),
    };

    private static BgDecisionData CubeDecision(
        int[]? mop = null,
        int onRollNeeds = 5,
        int opponentNeeds = 2,
        bool isCrawford = false,
        int cubeSize = 2,
        CubeOwner cubeOwner = CubeOwner.OnRoll,
        DescriptiveData? descriptive = null) => new()
    {
        Id = new XgpDecisionId("fixture.xgp"),
        Xgid = "XGID=not-consulted-by-derivation",
        Position = new PositionData
        {
            Mop = mop ?? StandardMop(),
            OnRollNeeds = onRollNeeds,
            OpponentNeeds = opponentNeeds,
            IsCrawford = isCrawford,
            CubeSize = cubeSize,
            CubeOwner = cubeOwner,
        },
        Decision = new DecisionData { IsCube = true },
        Descriptive = descriptive ?? new DescriptiveData(),
    };

    private static ProblemKey Derive(BgDecisionData data)
    {
        Assert.True(ProblemKey.TryDerive(data, out var key));
        return key!;
    }

    private static void AssertNoKey(BgDecisionData data)
    {
        Assert.False(ProblemKey.TryDerive(data, out var key));
        Assert.Null(key);
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
            Derive(PlayDecision(onRollNeeds: 7, opponentNeeds: 7)),
            Derive(PlayDecision(onRollNeeds: 7, opponentNeeds: 5)));
    }

    [Fact]
    public void Equality_CrawfordToggle_NotEqual()
    {
        Assert.NotEqual(
            Derive(PlayDecision(onRollNeeds: 1, opponentNeeds: 3)),
            Derive(PlayDecision(onRollNeeds: 1, opponentNeeds: 3, isCrawford: true)));
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
            onRollNeeds: 5, opponentNeeds: 2, cubeSize: 2, cubeOwner: CubeOwner.OnRoll));
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
            onRollNeeds: 1, opponentNeeds: 3, isCrawford: true, dice: [5, 2]));

        Assert.Equal(PinnedCrawfordPlayKey, key.ToString());
    }

    [Fact]
    public void CanonicalForm_MoneyPlayKey_Pinned()
    {
        var key = Derive(PlayDecision(onRollNeeds: 0, opponentNeeds: 0));

        Assert.Equal(PinnedMoneyPlayKey, key.ToString());
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
        Assert.Equal(PinnedMoneyPlayKey, ProblemKey.Parse(PinnedMoneyPlayKey).ToString());
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
    [InlineData("0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0/7a7/1c/31")]        // empty board
    [InlineData("0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,3,0/7a7/1c/31")]    // 16 on-roll checkers
    [InlineData("0,-3,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]    // 16 opponent checkers
    [InlineData("1,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,1,0/7a7/1c/31")]    // on-roll checker on opponent bar
    [InlineData("0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-4,0,0,0,0,2,-1/7a7/1c/31")]   // opponent checker on on-roll bar
    [InlineData("0,-16,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31")]   // per-point out of range
    [InlineData(StandardBoardToken + "/0a7/1c/31")]         // one-sided zero away
    [InlineData(StandardBoardToken + "/7a0/1c/31")]         // one-sided zero away
    [InlineData(StandardBoardToken + "/0a0cr/1c/31")]       // crawford in money
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
        var a = PlayDecision(descriptive: new DescriptiveData
        {
            MatchLength = 7,
            OnRollName = "Alice",
            OpponentName = "Bob",
            SourceFile = "one.xg",
            Game = 1,
            MoveNumber = 4,
        });
        var b = PlayDecision(descriptive: new DescriptiveData
        {
            MatchLength = 7,
            OnRollName = "Carol",
            OpponentName = "Dave",
            SourceFile = "two.xgp",
            Game = 3,
            MoveNumber = 17,
        });

        Assert.Equal(Derive(a), Derive(b));
    }

    [Fact]
    public void TryDerive_CollapseCase_SameAwayDifferentMatchLength_EqualKeys()
    {
        // Spec §1 consequence, POSITIVE fixture: 3-away/2-away is the same
        // problem whether the match is to 7 or to 11 — match length is
        // subsumed by away scores and must not participate.
        var shortMatch = PlayDecision(onRollNeeds: 3, opponentNeeds: 2,
            descriptive: new DescriptiveData { MatchLength = 7 });
        var longMatch = PlayDecision(onRollNeeds: 3, opponentNeeds: 2,
            descriptive: new DescriptiveData { MatchLength = 11 });

        Assert.Equal(Derive(shortMatch), Derive(longMatch));
    }

    [Fact]
    public void TryDerive_CollapseCase_MirrorTurnDuplicates_EqualKeys()
    {
        // Spec §1 consequence, POSITIVE fixture: the same problem recorded
        // with the seats swapped presents identical on-roll-relative facts —
        // turn/seat is normalized away by the Mop convention, so only the
        // descriptive frame differs and the keys must unify.
        var seatsA = CubeDecision(descriptive: new DescriptiveData
        {
            OnRollName = "Alice",
            OpponentName = "Bob",
            Game = 2,
            MoveNumber = 6,
        });
        var seatsB = CubeDecision(descriptive: new DescriptiveData
        {
            OnRollName = "Bob",
            OpponentName = "Alice",
            Game = 5,
            MoveNumber = 11,
        });

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
    public void NoKey_UnstampedDiceOnPlay()
    {
        // The spec's named case: a checker play whose dice were never
        // stamped (the [0,0] default). Guessing a roll is forbidden.
        AssertNoKey(PlayDecision(dice: [0, 0]));
    }

    [Theory]
    [InlineData(new[] { 7, 1 })]
    [InlineData(new[] { 3 })]
    [InlineData(new[] { 3, 1, 2 })]
    [InlineData(new int[0])]
    public void NoKey_MalformedDiceOnPlay(int[] dice)
    {
        AssertNoKey(PlayDecision(dice: dice));
    }

    [Fact]
    public void NoKey_NullDiceListOnPlay()
    {
        // Lenient JSON input can null the dice list through init.
        var template = PlayDecision();
        AssertNoKey(new BgDecisionData
        {
            Id = template.Id,
            Position = template.Position,
            Decision = new DecisionData { IsCube = false, Dice = null! },
        });
    }

    [Fact]
    public void NoKey_EmptyBoard()
    {
        AssertNoKey(PlayDecision(mop: new int[26]));
    }

    [Fact]
    public void NoKey_MalformedBoardShape()
    {
        AssertNoKey(PlayDecision(mop: new int[25]));

        var nullMop = PlayDecision();
        AssertNoKey(new BgDecisionData
        {
            Id = nullMop.Id,
            Position = new PositionData { Mop = null! },
            Decision = nullMop.Decision,
        });
    }

    [Fact]
    public void NoKey_PerPointCountOutOfRange()
    {
        var mop = StandardMop();
        mop[6] = 16;
        AssertNoKey(PlayDecision(mop: mop));
    }

    [Fact]
    public void NoKey_MoreThanFifteenCheckersPerSide()
    {
        // Real-board posture: totals capped at 15 per side even when every
        // individual point is in range.
        var onRollHeavy = StandardMop();
        onRollHeavy[24] = 3;        // positives now 16
        AssertNoKey(PlayDecision(mop: onRollHeavy));

        var opponentHeavy = StandardMop();
        opponentHeavy[1] = -3;      // |negatives| now 16
        AssertNoKey(PlayDecision(mop: opponentHeavy));
    }

    [Fact]
    public void NoKey_CheckerOnWrongBar()
    {
        // Each bar holds only its own side's checkers: Mop[0] <= 0 (opponent
        // bar), Mop[25] >= 0 (on-roll bar). Totals kept at 15 so only the
        // bar-sign rung can reject.
        var onRollOnOpponentBar = StandardMop();
        onRollOnOpponentBar[0] = 1;
        onRollOnOpponentBar[24] = 1;
        AssertNoKey(PlayDecision(mop: onRollOnOpponentBar));

        var opponentOnOnRollBar = StandardMop();
        opponentOnOnRollBar[25] = -1;
        opponentOnOnRollBar[19] = -4;
        AssertNoKey(PlayDecision(mop: opponentOnOnRollBar));
    }

    [Theory]
    [InlineData(-1, 7)]
    [InlineData(7, -1)]
    [InlineData(0, 7)]      // one-sided zero: money is 0/0 only
    [InlineData(7, 0)]
    public void NoKey_InvalidAwayScores(int onRollNeeds, int opponentNeeds)
    {
        AssertNoKey(PlayDecision(onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds));
    }

    [Theory]
    [InlineData(0, 0)]      // crawford in a money game
    [InlineData(3, 2)]      // crawford with neither side 1-away
    public void NoKey_InconsistentCrawford(int onRollNeeds, int opponentNeeds)
    {
        AssertNoKey(PlayDecision(
            onRollNeeds: onRollNeeds, opponentNeeds: opponentNeeds, isCrawford: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(3)]
    [InlineData(6)]
    public void NoKey_InvalidCubeSize(int cubeSize)
    {
        AssertNoKey(CubeDecision(cubeSize: cubeSize));
    }

    [Fact]
    public void NoKey_UndefinedCubeOwner()
    {
        AssertNoKey(CubeDecision(cubeOwner: (CubeOwner)99));
    }

    [Fact]
    public void NoKey_NullCategoryMembers()
    {
        // Lenient JSON input can null the category members through init.
        var template = PlayDecision();
        AssertNoKey(new BgDecisionData
        {
            Id = template.Id,
            Position = null!,
            Decision = template.Decision,
        });
        AssertNoKey(new BgDecisionData
        {
            Id = template.Id,
            Position = template.Position,
            Decision = null!,
        });
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
        // The v2 stats document keys its per-problem map by ProblemKey — the
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
                    onRollNeeds: 1, opponentNeeds: 3, isCrawford: true, dice: [5, 2]))
                    .ToString());

            Assert.Equal(ProblemKey.Parse(PinnedPlayKey), Derive(PlayDecision()));
            Assert.Equal(PinnedCubeKey, ProblemKey.Parse(PinnedCubeKey).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
