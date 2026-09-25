using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// The source-generation gate (halheinrich/backgammon#129 leg 1): the
/// context changes the mechanism, never the bytes. Every wire type must
/// serialize byte-identically through <see cref="BgDataTypesJsonContext"/>
/// and through the reflection resolver, the bundled converters must be
/// honored on the source-generated path, and the context must cover the
/// full wire closure.
/// </summary>
public class BgDataTypesJsonContextTests
{
    // The reflection path — what every consumer runs today.
    private static readonly JsonSerializerOptions ReflectionOptions = new();

    // The context-only path: type resolution goes through the
    // source-generated metadata alone, no reflection fallback — what a
    // trimmed consumer runs.
    private static readonly JsonSerializerOptions ContextOptions = new()
    {
        TypeInfoResolver = BgDataTypesJsonContext.Default
    };

    // -----------------------------------------------------------------------
    //  Fixtures — one rich instance per wire shape, every field populated
    //  away from its default wherever the shape allows, so a per-property
    //  mechanism difference cannot hide behind an omitted or default value.
    // -----------------------------------------------------------------------

    private static BgDecisionData FullPlayDecision()
    {
        var mop = new int[26];
        mop[1] = 2; mop[6] = -5; mop[13] = 5; mop[24] = -2; mop[25] = 1;
        var afterBest = new int[26];
        afterBest[4] = 2; afterBest[6] = -5; afterBest[20] = -2;
        var afterPlayer = new int[26];
        afterPlayer[5] = 2; afterPlayer[6] = -5; afterPlayer[19] = -2;

        return TestRecords.Record(
            id: new XgDecisionId("match.xg", Game: 4, MoveNumber: 22, IsCube: false),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:64:0:0:0:0:10",
            position: TestRecords.Position(
                mop: new BoardPosition(mop),
                onRollNeeds: 3,
                opponentNeeds: 5,
                onRollPipCount: 131,
                opponentPipCount: 144,
                cubeSize: 2,
                cubeOwner: CubeOwner.OnRoll,
                isCrawford: true),
            decision: TestRecords.Decision(
                dice: [6, 4],
                plays: [
                    TestRecords.Candidate(
                        moveNotation: "24/18 13/9",
                        play: [new(24, 18), new(13, 9)],
                        depth: "Rollout: 1296 trials. 3-ply",
                        depthAbbreviation: "3p1296",
                        depthRank: 7,
                        analysisMode: AnalysisMode.Rollout,
                        analysisLevel: AnalysisLevel.Ply3,
                        equity: 0.211,
                        winPct: 0.481,
                        winGammonPct: 0.112,
                        winBgPct: 0.004,
                        losePct: 0.519,
                        loseGammonPct: 0.143,
                        loseBgPct: 0.006),
                    TestRecords.Candidate(
                        moveNotation: "24/18 24/20*",
                        play: [new(24, 18), new(24, -20)],
                        depth: "3-ply",
                        depthAbbreviation: "3-ply",
                        depthRank: 4,
                        analysisMode: AnalysisMode.Evaluation,
                        analysisLevel: AnalysisLevel.Ply3Red,
                        equity: 0.198,
                        equityLoss: 0.013)
                ],
                bestPlayIndex: 0,
                userPlayIndex: 1,
                userPlayError: 0.013,
                isCube: false),
            descriptive: TestRecords.Descriptive(
                matchLength: 9,
                onRollName: "Mochy",
                opponentName: "Falafel",
                title: "Final",
                date: new DateOnly(2024, 11, 15),
                @event: "Monte Carlo 2024",
                sourceFile: "mochy-falafel.xg",
                game: 4,
                moveNumber: 22,
                isStandardStart: true,
                comment: "Blitz or prime?",
                flagged: true),
            outcome: TestRecords.Outcome(
                afterBestBoard: new BoardPosition(afterBest),
                afterPlayerBoard: new BoardPosition(afterPlayer)));
    }

    private static BgDecisionData FullCubeDecision() => TestRecords.Record(
        id: new XgDecisionId("session.xg", Game: 2, MoveNumber: 7, IsCube: true),
        xgid: "XGID=-b----E-C---eE---c-e----B-:1:1:1:00:0:0:1:0:10",
        position: TestRecords.Position(
            mop: BoardPosition.Empty,
            onRollPipCount: 92,
            opponentPipCount: 108,
            cubeSize: 2,
            cubeOwner: CubeOwner.Centered,
            isJacoby: true),
        decision: TestRecords.Decision(
            dice: [0, 0],
            isCube: true,
            cubeDepth: "Rollout: 1296 trials. 3-ply",
            cubeDepthAbbreviation: "3p1296",
            cubeDepthRank: 7,
            cubeAnalysisMode: AnalysisMode.BookRollout,
            cubeAnalysisLevel: AnalysisLevel.XgRoller,
            noDoubleEquity: 0.312,
            doubleTakeEquity: 0.287,
            cubelessNoDoubleEquity: 0.205,
            cubelessDoubleTakeEquity: 0.198,
            winPctAfterNoDouble: 0.621,
            gammonPctAfterNoDouble: 0.183,
            bgPctAfterNoDouble: 0.012,
            losePctAfterNoDouble: 0.379,
            loseGammonPctAfterNoDouble: 0.091,
            loseBgPctAfterNoDouble: 0.003,
            winPctAfterDoubleTake: 0.618,
            gammonPctAfterDoubleTake: 0.181,
            bgPctAfterDoubleTake: 0.011,
            losePctAfterDoubleTake: 0.382,
            loseGammonPctAfterDoubleTake: 0.093,
            loseBgPctAfterDoubleTake: 0.004,
            probOfOpponentErrorJustifyingDouble: 0.078,
            userDoubleError: 0.025,
            userTakeError: 0.011,
            userDoublerAction: CubeAction.Double,
            userTakerAction: CubeAction.Take),
        descriptive: TestRecords.Descriptive(
            matchLength: 0,
            onRollName: "Hal",
            opponentName: "Bot",
            sourceFile: "hal-bot.xg",
            game: 2,
            moveNumber: 7),
        outcome: TestRecords.Outcome());   // a cube decision: both after-boards absent

    // Every member at the value it defaulted to before the wire rule
    // (halheinrich/backgammon#222) — the record that used to be spelled by
    // its Id alone, now stated in full by the builder.
    private static BgDecisionData MinimalDecision() =>
        TestRecords.Record(id: new XgpDecisionId("minimal.xgp"));

    private static DecisionRow FullDecisionRow()
    {
        var board = new int[26];
        board[1] = 2; board[6] = -5;
        var after = new int[26];
        after[2] = 2; after[6] = -5;

        return TestRecords.Row(
            id: new XgDecisionId("match.xg", Game: 3, MoveNumber: 14, IsCube: false),
            xgid: "XGID=-b----E-C---eE---c-e----B-:0:0:1:52:0:0:0:0:10",
            error: 0.045,
            matchLength: 7,
            player: "Mochy",
            sourceFile: "match.xg",
            game: 3,
            moveNumber: 14,
            isStandardStart: true,
            roll: 52,
            analysisDepth: "3-ply",
            analysisMode: AnalysisMode.Evaluation,
            analysisLevel: AnalysisLevel.Ply3,
            equity: -0.118,
            onRollNeeds: 4,
            opponentNeeds: 2,
            isCrawford: true,
            isJacoby: null,
            board: new BoardPosition(board),
            afterBestBoard: new BoardPosition(after),
            afterPlayerBoard: new BoardPosition(after));
    }

    // A pinned canonical key (ProblemKeyTests' grammar pins own the format;
    // this suite only needs one valid spelling).
    private const string CanonicalProblemKey =
        "0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0/7a7/1c/31";

    // -----------------------------------------------------------------------
    //  Byte identity — the invariant of the whole halheinrich/backgammon#129
    //  arc: source generation changes the mechanism, never the bytes.
    // -----------------------------------------------------------------------

    private static void AssertContextMatchesReflection<T>(T value)
    {
        var reflectionJson = JsonSerializer.Serialize(value, ReflectionOptions);

        // Chained-resolver path (how a downstream combines contexts) and
        // direct-context path (how a leaf consumer serializes) must both
        // match the reflection bytes.
        var contextJson = JsonSerializer.Serialize(value, ContextOptions);
        var directJson = JsonSerializer.Serialize(
            value, typeof(T), BgDataTypesJsonContext.Default);

        Assert.Equal(reflectionJson, contextJson);
        Assert.Equal(reflectionJson, directJson);

        // And the read side: context-deserialized state re-serializes to the
        // same bytes — a full context-path round-trip.
        var restored = JsonSerializer.Deserialize<T>(contextJson, ContextOptions);
        Assert.Equal(contextJson, JsonSerializer.Serialize(restored, ContextOptions));
    }

    [Fact]
    public void BgDecisionData_PlayDecision_ContextMatchesReflection()
        => AssertContextMatchesReflection(FullPlayDecision());

    [Fact]
    public void BgDecisionData_CubeDecision_ContextMatchesReflection()
        => AssertContextMatchesReflection(FullCubeDecision());

    [Fact]
    public void BgDecisionData_Minimal_ContextMatchesReflection()
        => AssertContextMatchesReflection(MinimalDecision());

    [Fact]
    public void DecisionRow_ContextMatchesReflection()
        => AssertContextMatchesReflection(FullDecisionRow());

    [Fact]
    public void Play_ContextMatchesReflection()
        => AssertContextMatchesReflection<Play>([new(13, 10), new(10, -8), new(25, 24), new(6, 0)]);

    [Fact]
    public void Play_Empty_ContextMatchesReflection()
        => AssertContextMatchesReflection<Play>([]);

    [Fact]
    public void Move_ContextMatchesReflection()
        => AssertContextMatchesReflection(new Move(24, -18));

    [Fact]
    public void DecisionId_BothSubtypes_ContextMatchesReflection()
    {
        AssertContextMatchesReflection<DecisionId>(new XgpDecisionId("file.xgp"));
        AssertContextMatchesReflection<DecisionId>(
            new XgDecisionId("file.xg", Game: 4, MoveNumber: 22, IsCube: true));
    }

    [Fact]
    public void ProblemKey_ContextMatchesReflection()
        => AssertContextMatchesReflection(ProblemKey.Parse(CanonicalProblemKey));

    [Fact]
    public void DiceRoll_ContextMatchesReflection()
        => AssertContextMatchesReflection(new DiceRoll(3, 1));

    [Fact]
    public void BoardPosition_ContextMatchesReflection()
    {
        AssertContextMatchesReflection(BoardPosition.Standard);
        AssertContextMatchesReflection(BoardPosition.Empty);
    }

    [Fact]
    public void Enums_ContextMatchesReflection()
    {
        AssertContextMatchesReflection(AnalysisMode.BookRollout);
        AssertContextMatchesReflection(AnalysisLevel.Ply3Red);
        AssertContextMatchesReflection(CubeAction.Pass);
        AssertContextMatchesReflection(CubeClaim.TooGood);
        AssertContextMatchesReflection(CubeOwner.Opponent);
    }

    // -----------------------------------------------------------------------
    //  Converter respect on the source-generated path — each bundled
    //  converter's wire form, produced with the context alone.
    // -----------------------------------------------------------------------

    [Fact]
    public void ContextPath_PlaySerializesAsMoveArray()
    {
        var json = JsonSerializer.Serialize(FullPlayDecision(), ContextOptions);

        Assert.Contains("\"Play\":[{\"FrPt\":24,\"ToPt\":18},{\"FrPt\":13,\"ToPt\":9}]", json);
        Assert.Contains("\"Play\":[{\"FrPt\":24,\"ToPt\":18},{\"FrPt\":24,\"ToPt\":-20}]", json);
    }

    [Fact]
    public void ContextPath_DecisionIdSerializesAsCanonicalString()
    {
        var json = JsonSerializer.Serialize(FullCubeDecision(), ContextOptions);

        Assert.Contains("\"Id\":\"session.xg:g2:m7:cube\"", json);
    }

    [Fact]
    public void ContextPath_EnumsSerializeAsDeclaredNames()
    {
        var json = JsonSerializer.Serialize(FullCubeDecision(), ContextOptions);

        Assert.Contains("\"CubeOwner\":\"Centered\"", json);
        Assert.Contains("\"CubeAnalysisMode\":\"BookRollout\"", json);
        Assert.Contains("\"CubeAnalysisLevel\":\"XgRoller\"", json);
        Assert.Contains("\"UserDoublerAction\":\"Double\"", json);
        Assert.Contains("\"UserTakerAction\":\"Take\"", json);
    }

    [Fact]
    public void ContextPath_BoardsSerializeAsCountArrays_AndAnAbsentAfterBoardAsNull()
    {
        var play = JsonSerializer.Serialize(FullPlayDecision(), ContextOptions);
        var cube = JsonSerializer.Serialize(FullCubeDecision(), ContextOptions);

        Assert.Contains("\"Mop\":[0,2,0,0,0,0,-5,0,0,0,0,0,0,5,0,0,0,0,0,0,0,0,0,0,-2,1]", play);
        Assert.Contains("\"AfterBestBoard\":[0,0,0,0,2,0,-5,0,0,0,0,0,0,0,0,0,0,0,0,0,-2,0,0,0,0,0]", play);
        Assert.Contains("\"AfterBestBoard\":null,\"AfterPlayerBoard\":null", cube);
        Assert.Equal(
            "[0,-2,0,0,0,0,5,0,3,0,0,0,-5,5,0,0,0,-3,0,-5,0,0,0,0,2,0]",
            JsonSerializer.Serialize(BoardPosition.Standard, ContextOptions));
    }

    [Fact]
    public void ContextPath_ProblemKeyAndDiceRollSerializeAsTokens()
    {
        Assert.Equal(
            $"\"{CanonicalProblemKey}\"",
            JsonSerializer.Serialize(ProblemKey.Parse(CanonicalProblemKey), ContextOptions));
        Assert.Equal("\"31\"", JsonSerializer.Serialize(new DiceRoll(3, 1), ContextOptions));
    }

    [Fact]
    public void ContextPath_StrictEnumConverters_RejectNumericOrdinals()
    {
        // The halheinrich/backgammon#164 strictness must survive the
        // mechanism change: numeric enum tokens stay rejected when the
        // metadata comes from the context. Rewritten onto full documents:
        // every other member is required (halheinrich/backgammon#222), so a
        // partial document would be refused for its absent members and pass
        // vacuously. Each full document loads unaltered (the control); only
        // its enum turned numeric is refused.
        AssertOnlyTheNumericTokenIsRefused(TestRecords.Candidate(), "AnalysisMode", 2);
        AssertOnlyTheNumericTokenIsRefused(TestRecords.Candidate(), "AnalysisLevel", 4);
        AssertOnlyTheNumericTokenIsRefused(TestRecords.Position(), "CubeOwner", 1);
        AssertOnlyTheNumericTokenIsRefused(
            TestRecords.Decision(isCube: true, userDoublerAction: CubeAction.Double), "UserDoublerAction", 1);

        // CubeClaim has no embedding document in this library yet (it is a
        // declared root ahead of its first downstream document — the
        // halheinrich/backgammon#86 arc's consumer legs), so its strictness
        // through the context is pinned on the bare token.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CubeClaim>(
            "2", ContextOptions));
    }

    private static void AssertOnlyTheNumericTokenIsRefused<T>(T full, string member, int ordinal)
    {
        var document = JsonNode.Parse(JsonSerializer.Serialize(full, ContextOptions))!.AsObject();
        Assert.NotNull(JsonSerializer.Deserialize<T>(document.ToJsonString(), ContextOptions));

        document[member] = ordinal;
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<T>(document.ToJsonString(), ContextOptions));
    }

    // -----------------------------------------------------------------------
    //  Completeness — the halheinrich/backgammon#144 intersection pattern:
    //  two independent enumerations of one fact, kept agreeing by a test.
    //  Side A is the wire closure derived from the types themselves by
    //  reflection; side B is the context's coverage. A wire property added
    //  to any document type lands in side A automatically and fails here
    //  until the context resolves it.
    // -----------------------------------------------------------------------

    [Fact]
    public void Context_CoversTheFullWireClosure()
    {
        var uncovered = WireClosure()
            .Where(t => BgDataTypesJsonContext.Default.GetTypeInfo(t) is null)
            .Select(t => t.ToString())
            .Order()
            .ToList();

        Assert.True(uncovered.Count == 0,
            "Wire types not covered by BgDataTypesJsonContext: "
            + string.Join(", ", uncovered));
    }

    private static HashSet<Type> WireClosure()
    {
        // Roots: the wire units — the document roots, and the types that
        // define their own wire token via a bundled converter. Move is a
        // root because no property walk can reach it: Play's converter
        // stops the walk at Play yet emits Move elements by resolving them
        // through the active options (PlayJsonConverter's contract).
        Type[] roots =
        [
            typeof(BgDecisionData), typeof(DecisionRow),
            typeof(Play), typeof(Move), typeof(DecisionId),
            typeof(ProblemKey), typeof(DiceRoll), typeof(BoardPosition),
            typeof(AnalysisMode), typeof(AnalysisLevel),
            typeof(CubeAction), typeof(CubeClaim), typeof(CubeOwner)
        ];

        var closure = new HashSet<Type>();
        var pending = new Queue<Type>(roots);
        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            if (!closure.Add(type))
                continue;

            // Leaves the serializer handles wholesale. Nullable<T> stays
            // unexpanded deliberately: the wrapper is what a property
            // declares and what resolution asks for; its converter reaches
            // the underlying type internally, not through the resolver.
            if (type.IsPrimitive || type.IsEnum || type == typeof(string)
                || Nullable.GetUnderlyingType(type) is not null)
                continue;

            // A bundled custom converter owns the type's wire form; the
            // serializer never walks its properties, so neither does the
            // closure. (What a converter emits internally is invisible to
            // reflection — hence Move among the roots.)
            if (type.GetCustomAttribute<JsonConverterAttribute>() is not null)
                continue;

            // Collections serialize their element type, not properties.
            var element = ElementType(type);
            if (element is not null)
            {
                pending.Enqueue(element);
                continue;
            }

            // Everything else is an object shape: its serialized properties
            // are the public instance getters not excluded by [JsonIgnore].
            foreach (var property in type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetMethod is null)
                    continue;
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
                    continue;
                pending.Enqueue(property.PropertyType);
            }
        }

        // DateOnly (DescriptiveData.Date's underlying) rides in as
        // Nullable<DateOnly>; nothing else needs special casing.
        return closure;
    }

    private static Type? ElementType(Type type)
    {
        static bool IsEnumerable(Type i) =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        if (type.IsInterface && IsEnumerable(type))
            return type.GetGenericArguments()[0];

        return type.GetInterfaces().FirstOrDefault(IsEnumerable)
            ?.GetGenericArguments()[0];
    }

    // -----------------------------------------------------------------------
    //  The composition pattern — a consumer context combined with this one.
    //  The chain is load-bearing, not ceremonial: the consumer's own
    //  generator stops at Play (bundled converter) and so never reaches
    //  Move, exactly as this library's does. Alone, the consumer context
    //  cannot serialize a populated Play; chained after
    //  BgDataTypesJsonContext it can, byte-identically to reflection. This
    //  is the shape every downstream leg of halheinrich/backgammon#129
    //  repeats.
    // -----------------------------------------------------------------------

    [Fact]
    public void ConsumerContextAlone_CannotResolveConverterEmittedMove()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = ConsumerContext.Default
        };

        var document = new ConsumerDocument { Decisions = [FullPlayDecision()] };

        // PlayJsonConverter asks the active options for Move's metadata,
        // which the consumer's own generator never emitted (Play's bundled
        // converter stops its graph walk, same as ours).
        Assert.Throws<NotSupportedException>(
            () => JsonSerializer.Serialize(document, options));
    }

    [Fact]
    public void ConsumerContextChainedWithBgDataTypesContext_MatchesReflection()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                ConsumerContext.Default, BgDataTypesJsonContext.Default)
        };

        var document = new ConsumerDocument
        {
            Decisions = [FullPlayDecision(), FullCubeDecision(), MinimalDecision()]
        };

        var reflectionJson = JsonSerializer.Serialize(document, ReflectionOptions);
        var chainedJson = JsonSerializer.Serialize(document, options);
        Assert.Equal(reflectionJson, chainedJson);

        var restored = JsonSerializer.Deserialize<ConsumerDocument>(chainedJson, options)!;
        Assert.Equal(chainedJson, JsonSerializer.Serialize(restored, options));
        Assert.Equal(3, restored.Decisions.Count);
    }
}

/// <summary>
/// A stand-in downstream document embedding this library's wire unit — the
/// shape ConvertXgToJson_Lib's leg will own for real.
/// </summary>
public sealed class ConsumerDocument
{
    /// <summary>The embedded decisions.</summary>
    public List<BgDecisionData> Decisions { get; init; } = [];
}

/// <summary>
/// A stand-in downstream context: declares only the consumer's own document
/// root, exactly as the halheinrich/backgammon#129 pattern prescribes, and
/// combines with <see cref="BgDataTypesJsonContext"/> at the options.
/// Metadata-only generation is part of the pattern — a fast-path handler
/// would bind nested resolution to this context's own private options and
/// bypass the chain (see <see cref="BgDataTypesJsonContext"/>'s docs; the
/// chained test fails without this line).
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ConsumerDocument))]
internal sealed partial class ConsumerContext : JsonSerializerContext
{
}
