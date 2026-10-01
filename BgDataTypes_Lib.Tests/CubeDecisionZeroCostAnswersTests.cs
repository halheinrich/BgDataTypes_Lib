using System.Reflection;
using System.Text.Json;
using BgDataTypes_Lib;

namespace BgDataTypes_Lib.Tests;

/// <summary>
/// Pins the answers that cost zero at the display precision,
/// <see cref="CubeDecision.ZeroCostAnswers"/> (halheinrich/backgammon#326,
/// step 3a; SPEC-scoring §3, "The tie": the review's Best line lists every
/// answer whose cost counts as zero). N is the no-double equity and T the
/// double/take equity, the cash +1.
/// </summary>
/// <remarks>
/// Each exact case sits on values safely outside the zero threshold, so no
/// other cost falls under it, except the threshold cases, which sit either
/// side of it on purpose, below the cash and above it. Every decision is a
/// full record (<see cref="CubeAt"/>): the gammon fact is the position's.
/// </remarks>
public class CubeDecisionZeroCostAnswersTests
{
    private const CubeAnswer ND = CubeAnswer.NoDouble;
    private const CubeAnswer DT = CubeAnswer.DoubleTake;
    private const CubeAnswer DP = CubeAnswer.DoublePass;
    private const CubeAnswer Fourth = CubeAnswer.NoDoublePass;

    private static readonly bool[] BothGammonStates = [true, false];

    private static void AssertSet(CubeDecision cube, params CubeAnswer[] expected) =>
        Assert.Equal(expected, cube.ZeroCostAnswers);

    // =====================================================================
    //  Every row of both of SPEC-scoring §3's cost tables
    // =====================================================================

    // Where gammons are possible: each row's one zero-cost cell, and the
    // tie's three.
    [Theory]
    [InlineData(0.30, 0.60, new[] { DT })]                // Double / Take is right
    [InlineData(0.50, 0.40, new[] { ND })]                // No double / Take is right, below the cash
    [InlineData(1.1711, 0.6004, new[] { ND })]            // ... and above it
    [InlineData(0.50, 1.20, new[] { DP })]                // Double / Pass is right
    [InlineData(1.30, 1.50, new[] { Fourth })]            // Too good is right
    [InlineData(1.0, 1.20, new[] { ND, DP, Fourth })]     // the tie
    public void GammonsPossible_EachRowOfTheTable(double n, double t, CubeAnswer[] expected)
    {
        AssertSet(CubeAt.GammonsPossible(n, t), expected);
    }

    // Where gammons are not possible: No double costs its no-double error, 0
    // wherever the fourth answer is right, so that row has two.
    [Theory]
    [InlineData(0.30, 0.60, new[] { DT })]                // Double / Take is right
    [InlineData(0.50, 0.40, new[] { ND })]                // No double / Take is right, below the cash
    [InlineData(1.1711, 0.6004, new[] { ND })]            // ... and above it
    [InlineData(0.50, 1.20, new[] { DP })]                // Double / Pass is right
    [InlineData(1.30, 1.50, new[] { ND, Fourth })]        // No double / Pass is right
    [InlineData(1.0, 1.20, new[] { ND, DP, Fourth })]     // the tie
    public void GammonsNotPossible_EachRowOfTheTable(double n, double t, CubeAnswer[] expected)
    {
        AssertSet(CubeAt.GammonsNotPossible(n, t), expected);
    }

    // =====================================================================
    //  The ties and the named pairs
    // =====================================================================

    // halheinrich/backgammon#293: N = 1 with a pass. No double, Double / Pass
    // and the fourth answer, under either label; the Shimodaira equities among
    // them.
    [Theory]
    [InlineData(true, 1.20)]
    [InlineData(true, 2.0267)]
    [InlineData(false, 1.20)]
    [InlineData(false, 2.0267)]
    public void TheTie_HoldsNoDoubleDoublePassAndTheFourth(bool gammonsPossible, double t)
    {
        AssertSet(CubeAt.Decision(gammonsPossible, 1.0, t), ND, DP, Fourth);
    }

    // N = T = 1: every answer costs nothing, so all four, in the offered order.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AtNAndTBothOne_AllFour(bool gammonsPossible)
    {
        AssertSet(CubeAt.Decision(gammonsPossible, 1.0, 1.0), ND, DT, DP, Fourth);
    }

    // Where gammons are not possible and the fourth answer is the truth, N
    // well above 1: exactly No double and the fourth answer, though doubling
    // and not doubling do not tie.
    [Theory]
    [InlineData(1.30, 1.50)]
    [InlineData(1.20, 1.00)]
    [InlineData(2.0, 3.0)]
    public void NoGammons_TheFourthIsTheTruth_NoDoubleCostsNothingToo(double n, double t)
    {
        var cube = CubeAt.GammonsNotPossible(n, t);

        Assert.Equal(Fourth, cube.Decision.BestAnswer);
        AssertSet(cube, ND, Fourth);
    }

    // T = 1 with N < 1, away from the cash: the take and the pass both cost
    // nothing, so exactly Double / Take and Double / Pass.
    [Theory]
    [InlineData(true, 0.50)]
    [InlineData(false, 0.50)]
    [InlineData(true, -0.20)]
    [InlineData(false, -0.20)]
    public void AtTheTakePoint_DoubleTakeAndDoublePass(bool gammonsPossible, double n)
    {
        AssertSet(CubeAt.Decision(gammonsPossible, n, 1.0), DT, DP);
    }

    // T = N < 1, away from the cash: doubling and not doubling tie, so
    // exactly No double and Double / Take.
    [Theory]
    [InlineData(true, 0.50)]
    [InlineData(false, 0.50)]
    [InlineData(true, -0.20)]
    [InlineData(false, -0.20)]
    public void WhereDoublingTies_NoDoubleAndDoubleTake(bool gammonsPossible, double equity)
    {
        AssertSet(CubeAt.Decision(gammonsPossible, equity, equity), ND, DT);
    }

    // =====================================================================
    //  The threshold, either side, below the cash and above it
    // =====================================================================

    // Below the cash, with a pass: No double and the fourth answer each cost
    // 1 − N. At 0.00004 that counts as zero and joins Double / Pass; at
    // 0.00006 it does not.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BelowTheCash_ACostUnderTheThreshold_Joins(bool gammonsPossible)
    {
        AssertSet(CubeAt.Decision(gammonsPossible, 0.99996, 1.5), ND, DP, Fourth);
        AssertSet(CubeAt.Decision(gammonsPossible, 0.99994, 1.5), DP);
    }

    // Above the cash, with a pass: Double / Pass costs N − 1. At 0.00004 that
    // counts as zero and joins the truth's answers (the fourth, and No double
    // too where gammons are not possible); at 0.00006 it does not.
    [Fact]
    public void AboveTheCash_ACostUnderTheThreshold_Joins()
    {
        AssertSet(CubeAt.GammonsPossible(1.00004, 1.5), DP, Fourth);
        AssertSet(CubeAt.GammonsPossible(1.00006, 1.5), Fourth);

        AssertSet(CubeAt.GammonsNotPossible(1.00004, 1.5), ND, DP, Fourth);
        AssertSet(CubeAt.GammonsNotPossible(1.00006, 1.5), ND, Fourth);
    }

    // A cost that counts as zero is not 0: the set judges it, and the cost
    // stays exact.
    [Fact]
    public void AJoiningCost_StaysExact()
    {
        var cube = CubeAt.GammonsPossible(1.00004, 1.5);

        Assert.Contains(DP, cube.ZeroCostAnswers);
        Assert.NotEqual(0.0, cube.CostOf(DP).Total);
    }

    // =====================================================================
    //  Over a grid holding every boundary and both sides of the threshold
    // =====================================================================

    private static readonly double[] Grid =
    [
        -1.0, -0.30, 0.0, 0.40, 0.50, 0.60, 0.6004, 0.9829, 0.99994, 0.99996, 0.999,
        1.0, 1.0000015, 1.00004, 1.00006, 1.001, 1.1711, 1.50, 2.0267,
    ];

    private static IEnumerable<CubeDecision> EveryGridDecision()
    {
        foreach (bool gammonsPossible in BothGammonStates)
            foreach (var n in Grid)
                foreach (var t in Grid)
                    yield return CubeAt.Decision(gammonsPossible, n, t);
    }

    private static string At(CubeDecision cube) =>
        $"N {cube.Decision.NoDoubleEquity}, T {cube.Decision.DoubleTakeEquity}, gammons {(cube.GammonsPossible ? "possible" : "not possible")}";

    // Never empty, and the truth always in it.
    [Fact]
    public void NeverEmpty_AndTheTruthIsAlwaysIn()
    {
        foreach (var cube in EveryGridDecision())
        {
            var answers = cube.ZeroCostAnswers;
            Assert.True(answers.Count > 0, At(cube));
            Assert.True(answers.Contains(cube.Decision.BestAnswer), At(cube));
        }
    }

    // Exactly the answers whose whole cost counts as zero: each answer is in
    // it exactly when the zero rule passes its total, and no part decides it.
    [Fact]
    public void Membership_IsTheZeroRuleOnTheWholeCost()
    {
        foreach (var cube in EveryGridDecision())
        {
            var answers = cube.ZeroCostAnswers;
            foreach (var answer in Enum.GetValues<CubeAnswer>())
                Assert.True(
                    answers.Contains(answer) == EquityDisplay.CountsAsZero(cube.CostOf(answer).Total),
                    $"{answer} at {At(cube)}");
        }
    }

    // In the offered order, CubeAnswer's declaration order, each answer once.
    [Fact]
    public void InTheOfferedOrder()
    {
        CubeAnswer[] offered = Enum.GetValues<CubeAnswer>();
        Assert.Equal(new[] { ND, DT, DP, Fourth }, offered);

        foreach (var cube in EveryGridDecision())
        {
            var answers = cube.ZeroCostAnswers;
            for (int i = 1; i < answers.Count; i++)
                Assert.True(
                    Array.IndexOf(offered, answers[i - 1]) < Array.IndexOf(offered, answers[i]),
                    $"{string.Join(", ", answers)} at {At(cube)}");
        }
    }

    // =====================================================================
    //  No caller can change what the producer reports
    // =====================================================================

    // The result is read-only through every interface it implements, and is
    // not an array a cast could write to.
    [Fact]
    public void TheResult_CannotBeMutated()
    {
        var cube = CubeAt.Decision(true, 1.0, 1.0);
        var answers = cube.ZeroCostAnswers;

        Assert.IsNotType<CubeAnswer[]>(answers);
        Assert.IsNotType<List<CubeAnswer>>(answers);
        if (answers is ICollection<CubeAnswer> collection)
        {
            Assert.True(collection.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => collection.Add(DT));
            Assert.Throws<NotSupportedException>(() => collection.Remove(ND));
            Assert.Throws<NotSupportedException>(collection.Clear);
        }
        if (answers is IList<CubeAnswer> list)
        {
            Assert.Throws<NotSupportedException>(() => list[0] = Fourth);
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list.Insert(0, Fourth));
        }
        if (answers is System.Collections.IList untyped)
        {
            Assert.True(untyped.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => untyped[0] = Fourth);
        }

        AssertSet(cube, ND, DT, DP, Fourth);
    }

    // Each read is its own: no two share a collection, so one caller's
    // result says nothing about storage another read reports from, and a
    // later read reports the same set.
    [Fact]
    public void EachRead_IsItsOwn_AndReportsTheSameSet()
    {
        var cube = CubeAt.GammonsNotPossible(1.30, 1.50);
        var first = cube.ZeroCostAnswers;
        var second = cube.ZeroCostAnswers;

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        AssertSet(cube, ND, Fourth);
    }

    // The decision keeps no collection the set could expose: it is derived
    // on each read, and the member is a read-only list with no setter.
    [Fact]
    public void TheDecision_KeepsNoCollection()
    {
        var property = typeof(CubeDecision).GetProperty(nameof(CubeDecision.ZeroCostAnswers))!;

        Assert.Equal(typeof(IReadOnlyList<CubeAnswer>), property.PropertyType);
        Assert.Null(property.SetMethod);
        Assert.DoesNotContain(
            typeof(CubeDecision).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly),
            field => field.FieldType != typeof(string)
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(field.FieldType));
    }

    // =====================================================================
    //  A derivation, not wire
    // =====================================================================

    [Fact]
    public void ZeroCostAnswers_IsNotSerialised_BothPaths()
    {
        foreach (var (_, options) in WirePaths.Both)
        {
            string json = JsonSerializer.Serialize<BgDecisionData>(CubeAt.GammonsPossible(1.0, 1.0), options);
            Assert.DoesNotContain("ZeroCostAnswers", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
