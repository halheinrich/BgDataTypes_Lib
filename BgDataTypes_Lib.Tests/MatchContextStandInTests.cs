using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Each stand-in money used to be spelled with is no longer expressible
/// (halheinrich/backgammon#273, Hal's ruling of 2026-09-26): a match length
/// of 0, away scores of 0, a Crawford flag always false, a Jacoby fact on a
/// match, and a money predicate derived from a 0. Pinned through the public
/// surface: no member of the records, their categories or the filter
/// contract can hold one, and the match kind refuses the values that spelled
/// money. The flat row keeps a column per fact, typed to be empty — never 0 —
/// where the other kind's; its read-back pins are in
/// <see cref="DecisionRowSerializationTests"/>.
/// </summary>
public class MatchContextStandInTests
{
    private static readonly BindingFlags Public = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    /// <summary>Every public member name of <paramref name="type"/>, its interfaces' included.</summary>
    private static HashSet<string> MembersOf(Type type) =>
        [.. type.GetMembers(Public).Select(m => m.Name)
            .Concat(type.GetInterfaces().SelectMany(i => i.GetMembers(Public)).Select(m => m.Name))];

    public static TheoryData<Type> Surfaces =>
    [
        typeof(PositionData), typeof(DescriptiveData),
        typeof(BgDecisionData), typeof(CheckerPlayDecision), typeof(CubeDecision),
        typeof(IDecisionFilterData), typeof(IMatchInfo), typeof(IGameInfo),
    ];

    [Theory]
    [MemberData(nameof(Surfaces))]
    public void NoSurface_HasAFlatMatchContextMember(Type surface)
    {
        // The flat members that spelled money with 0s — and the Jacoby fact
        // a match record could carry — are gone from every record surface and
        // from the skip-early contracts: a session, the terms or the standing
        // is the one member stating the match context at each scope.
        var members = MembersOf(surface);

        foreach (var retired in new[]
                 {
                     "MatchLength", "OnRollNeeds", "OpponentNeeds", "IsCrawford", "IsJacoby", "IsMoneyGame",
                     "Away1", "Away2", "IsCrawfordGame",
                 })
            Assert.DoesNotContain(retired, members);
    }

    [Fact]
    public void AMatchLengthOfZero_CannotBeStated()
    {
        // Not on the descriptive category, where it spelled money, and not
        // on a match, whose length is at least 1; a money session has no
        // length at all.
        Assert.Null(typeof(DescriptiveData).GetProperty("MatchLength"));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(length: 0, onRollNeeds: 1, opponentNeeds: 1));
        Assert.Null(typeof(MoneySession).GetProperty("Length"));
    }

    [Fact]
    public void AwayScoresOfZero_CannotBeStated()
    {
        // Not on the position, where 0/0 spelled money, and not on a match;
        // a money session has no away scores.
        Assert.Null(typeof(PositionData).GetProperty("OnRollNeeds"));
        Assert.Null(typeof(PositionData).GetProperty("OpponentNeeds"));
        Assert.Throws<ArgumentOutOfRangeException>(() => TestRecords.MatchSession(onRollNeeds: 0, opponentNeeds: 0));
        Assert.Null(typeof(MoneySession).GetProperty("OnRollNeeds"));
        Assert.Null(typeof(MoneySession).GetProperty("OpponentNeeds"));
    }

    [Fact]
    public void AMoneySessionsCrawfordFlag_CannotBeStated()
    {
        // Money has no Crawford game: the flag that was always false there is
        // not a money member, and a money document carrying it is refused.
        Assert.Null(typeof(MoneySession).GetProperty("IsCrawford"));
        Assert.Null(typeof(PositionData).GetProperty("IsCrawford"));

        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        document["IsCrawford"] = false;
        WirePaths.AssertRefused<Session>(document.ToJsonString());
    }

    [Fact]
    public void AMatchsJacobyFact_CannotBeStated()
    {
        // The Jacoby rule means something in money only: a match has no member
        // for it, and a match document carrying one is refused rather than
        // read with it ignored, as a stamp on a match record used to be.
        Assert.Null(typeof(MatchSession).GetProperty("IsJacoby"));
        Assert.Null(typeof(PositionData).GetProperty("IsJacoby"));

        var document = WirePaths.Document<Session>(TestRecords.MatchSession());
        document["IsJacoby"] = true;
        WirePaths.AssertRefused<Session>(document.ToJsonString());
    }

    [Fact]
    public void AMoneySessionUnderAnUnknownRule_CannotBeStated()
    {
        // The rule is a required bool: two values, no null for "not stamped".
        // Rewritten for the composed session: the rule is the terms'.
        var isJacoby = typeof(MoneyTerms).GetProperty(nameof(MoneyTerms.IsJacoby))!;
        Assert.Equal(typeof(bool), isJacoby.PropertyType);

        var document = WirePaths.Document<Session>(TestRecords.MoneySession());
        var terms = document["Terms"]!.AsObject();
        terms["IsJacoby"] = null;
        WirePaths.AssertRefused<Session>(document.ToJsonString());
        terms.Remove("IsJacoby");
        WirePaths.AssertRefused<Session>(document.ToJsonString());
    }

    [Fact]
    public void TheRowsSessionColumns_AreEmptyNeverZero_ForTheOtherKind()
    {
        // The flat row keeps one column per fact; each is nullable, and the
        // other kind's is empty — the money row's MatchLength is null, never 0.
        foreach (var column in new[]
                 {
                     "MatchLength", "OnRollNeeds", "OpponentNeeds", "IsCrawford",
                     "IsJacoby", "IsBeaver", "CubeLimit", "OnRollScore", "OpponentScore",
                 })
            Assert.NotNull(Nullable.GetUnderlyingType(typeof(DecisionRow).GetProperty(column)!.PropertyType));

        var money = TestRecords.Row(TestRecords.CheckerPlay(position: TestRecords.Position(session: TestRecords.MoneySession())));
        Assert.Equal((null, null, null, null), (money.MatchLength, money.OnRollNeeds, money.OpponentNeeds, money.IsCrawford));
        var match = TestRecords.Row();
        Assert.Equal((null, null, null, null, null), (match.IsJacoby, match.IsBeaver, match.CubeLimit, match.OnRollScore, match.OpponentScore));
        Assert.Null(typeof(DecisionRow).GetProperty("IsMoneyGame"));
    }

    [Fact]
    public void MoneyIsTheSessionsKind_NeverReadOffAZero()
    {
        // Every surface that says money says it by the kind: the record, its
        // view and its row, from code and read back.
        var record = TestRecords.Cube(position: TestRecords.Position(session: TestRecords.MoneySession(isJacoby: false)));

        Assert.Equal(SessionKind.Money, record.Session.Kind);
        Assert.Equal(SessionKind.Money, record.ViewFor(PlayRanking.Equity).Session.Kind);
        Assert.Equal(SessionKind.Money, TestRecords.Row(record).SessionKind);
        foreach (var (_, options) in WirePaths.Both)
            Assert.Equal(SessionKind.Money, JsonSerializer.Deserialize<BgDecisionData>(
                JsonSerializer.Serialize<BgDecisionData>(record, options), options)!.Session.Kind);
    }
}
