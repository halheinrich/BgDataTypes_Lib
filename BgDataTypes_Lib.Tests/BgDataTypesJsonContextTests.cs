using System.Reflection;
using System.Text.Json;
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

        return new BgDecisionData
        {
            Id = new XgDecisionId("match.xg", Game: 4, MoveNumber: 22, IsCube: false),
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:64:0:0:0:0:10",
            Position = new PositionData
            {
                Mop = mop,
                OnRollNeeds = 3,
                OpponentNeeds = 5,
                OnRollPipCount = 131,
                OpponentPipCount = 144,
                CubeSize = 2,
                CubeOwner = CubeOwner.OnRoll,
                IsCrawford = true
            },
            Decision = new DecisionData
            {
                Dice = [6, 4],
                Plays =
                [
                    new PlayCandidate
                    {
                        MoveNotation = "24/18 13/9",
                        Play = [new(24, 18), new(13, 9)],
                        Depth = "Rollout: 1296 trials. 3-ply",
                        DepthAbbreviation = "3p1296",
                        DepthRank = 7,
                        AnalysisMode = AnalysisMode.Rollout,
                        AnalysisLevel = AnalysisLevel.Ply3,
                        Equity = 0.211,
                        WinPct = 0.481,
                        WinGammonPct = 0.112,
                        WinBgPct = 0.004,
                        LosePct = 0.519,
                        LoseGammonPct = 0.143,
                        LoseBgPct = 0.006
                    },
                    new PlayCandidate
                    {
                        MoveNotation = "24/18 24/20*",
                        Play = [new(24, 18), new(24, -20)],
                        Depth = "3-ply",
                        DepthAbbreviation = "3-ply",
                        DepthRank = 4,
                        AnalysisMode = AnalysisMode.Evaluation,
                        AnalysisLevel = AnalysisLevel.Ply3Red,
                        Equity = 0.198,
                        EquityLoss = 0.013
                    }
                ],
                BestPlayIndex = 0,
                UserPlayIndex = 1,
                UserPlayError = 0.013,
                IsCube = false
            },
            Descriptive = new DescriptiveData
            {
                MatchLength = 9,
                OnRollName = "Mochy",
                OpponentName = "Falafel",
                Title = "Final",
                Date = new DateOnly(2024, 11, 15),
                Event = "Monte Carlo 2024",
                SourceFile = "mochy-falafel.xg",
                Game = 4,
                MoveNumber = 22,
                IsStandardStart = true,
                Comment = "Blitz or prime?",
                Flagged = true
            },
            Outcome = new PlayOutcomeData
            {
                AfterBestBoard = afterBest,
                AfterPlayerBoard = afterPlayer
            }
        };
    }

    private static BgDecisionData FullCubeDecision() => new()
    {
        Id = new XgDecisionId("session.xg", Game: 2, MoveNumber: 7, IsCube: true),
        Xgid = "XGID=-b----E-C---eE---c-e----B-:1:1:1:00:0:0:1:0:10",
        Position = new PositionData
        {
            Mop = new int[26],
            OnRollPipCount = 92,
            OpponentPipCount = 108,
            CubeSize = 2,
            CubeOwner = CubeOwner.Centered,
            IsJacoby = true
        },
        Decision = new DecisionData
        {
            Dice = [0, 0],
            IsCube = true,
            CubeDepth = "Rollout: 1296 trials. 3-ply",
            CubeDepthAbbreviation = "3p1296",
            CubeDepthRank = 7,
            CubeAnalysisMode = AnalysisMode.BookRollout,
            CubeAnalysisLevel = AnalysisLevel.XgRoller,
            NoDoubleEquity = 0.312,
            DoubleTakeEquity = 0.287,
            CubelessNoDoubleEquity = 0.205,
            CubelessDoubleTakeEquity = 0.198,
            WinPctAfterNoDouble = 0.621,
            GammonPctAfterNoDouble = 0.183,
            BgPctAfterNoDouble = 0.012,
            LosePctAfterNoDouble = 0.379,
            LoseGammonPctAfterNoDouble = 0.091,
            LoseBgPctAfterNoDouble = 0.003,
            WinPctAfterDoubleTake = 0.618,
            GammonPctAfterDoubleTake = 0.181,
            BgPctAfterDoubleTake = 0.011,
            LosePctAfterDoubleTake = 0.382,
            LoseGammonPctAfterDoubleTake = 0.093,
            LoseBgPctAfterDoubleTake = 0.004,
            ProbOfOpponentErrorJustifyingDouble = 0.078,
            UserDoubleError = 0.025,
            UserTakeError = 0.011,
            UserDoublerAction = CubeAction.Double,
            UserTakerAction = CubeAction.Take
        },
        Descriptive = new DescriptiveData
        {
            MatchLength = 0,
            OnRollName = "Hal",
            OpponentName = "Bot",
            SourceFile = "hal-bot.xg",
            Game = 2,
            MoveNumber = 7
        }
    };

    private static BgDecisionData MinimalDecision() => new()
    {
        Id = new XgpDecisionId("minimal.xgp")
    };

    private static DecisionRow FullDecisionRow()
    {
        var board = new int[26];
        board[1] = 2; board[6] = -5;
        var after = new int[26];
        after[2] = 2; after[6] = -5;

        return new DecisionRow
        {
            Id = new XgDecisionId("match.xg", Game: 3, MoveNumber: 14, IsCube: false),
            Xgid = "XGID=-b----E-C---eE---c-e----B-:0:0:1:52:0:0:0:0:10",
            Error = 0.045,
            MatchLength = 7,
            Player = "Mochy",
            SourceFile = "match.xg",
            Game = 3,
            MoveNumber = 14,
            IsStandardStart = true,
            Roll = 52,
            AnalysisDepth = "3-ply",
            AnalysisMode = AnalysisMode.Evaluation,
            AnalysisLevel = AnalysisLevel.Ply3,
            Equity = -0.118,
            OnRollNeeds = 4,
            OpponentNeeds = 2,
            IsCrawford = true,
            IsJacoby = null,
            Board = board,
            AfterBestBoard = after,
            AfterPlayerBoard = after
        };
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
    public void Enums_ContextMatchesReflection()
    {
        AssertContextMatchesReflection(AnalysisMode.BookRollout);
        AssertContextMatchesReflection(AnalysisLevel.Ply3Red);
        AssertContextMatchesReflection(CubeAction.Pass);
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
        // metadata comes from the context.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayCandidate>(
            "{\"MoveNotation\":\"8/5 6/1\",\"AnalysisMode\":2}", ContextOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PlayCandidate>(
            "{\"MoveNotation\":\"8/5 6/1\",\"AnalysisLevel\":4}", ContextOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PositionData>(
            "{\"CubeOwner\":1}", ContextOptions));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionData>(
            "{\"IsCube\":true,\"UserDoublerAction\":1}", ContextOptions));
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
            typeof(ProblemKey), typeof(DiceRoll),
            typeof(AnalysisMode), typeof(AnalysisLevel),
            typeof(CubeAction), typeof(CubeOwner)
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
