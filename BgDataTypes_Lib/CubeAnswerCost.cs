using System.Numerics;

namespace BgDataTypes_Lib;

/// <summary>
/// What a cube answer costs at a decision, in its two parts: the cost
/// SPEC-scoring §3 rules (amended 2026-09-30 and 2026-10-01 on
/// halheinrich/backgammon#326), as <see cref="CubeDecision.CostOf"/> derives
/// it. Only this library builds one, so every cost is the decision's: no
/// caller can state a part, and the parts add up to the
/// <see cref="Total"/> by construction.
/// </summary>
/// <remarks>
/// <para>
/// The parts are exact, computed from the stored equities as they are:
/// nothing rounds a cost and nothing corrects the analyser's numbers. Whether
/// a cost, or either part, counts as zero is judged by
/// <see cref="EquityDisplay.CountsAsZero"/>, and shown by
/// <see cref="EquityDisplay.FormatLoss"/>, so what is shown and what is judged cannot
/// disagree.
/// </para>
/// <para>
/// A cost is not an analysis fact: an answer's doubling part is the doubling
/// action's error (<see cref="CubeDecisionData.DoublerActionError"/>) except
/// where §3's conventions charge a misreading that loses no equity, so the
/// action errors describe the analysis and these describe the answer.
/// </para>
/// </remarks>
public sealed class CubeAnswerCost : IEquatable<CubeAnswerCost>, IEqualityOperators<CubeAnswerCost, CubeAnswerCost, bool>
{
    internal CubeAnswerCost(double doublingPart, double takePart)
    {
        DoublingPart = doublingPart;
        TakePart = takePart;
    }

    /// <summary>
    /// The doubling part, never negative: what the answer's doubling half
    /// costs. Each answer's is stated once, on <see cref="CubeDecision.CostOf"/>.
    /// </summary>
    public double DoublingPart { get; }

    /// <summary>
    /// The take part, never negative: the answer's full take/pass error when
    /// it commits to its response (<see cref="CubeAnswerExtensions.CommitsToResponse"/>),
    /// and 0 for No double, whose implied take is never charged.
    /// </summary>
    public double TakePart { get; }

    /// <summary>The whole cost: <see cref="DoublingPart"/> + <see cref="TakePart"/>.</summary>
    public double Total => DoublingPart + TakePart;

    /// <summary>Whether <paramref name="other"/> has the same two parts.</summary>
    public bool Equals(CubeAnswerCost? other) =>
        other is not null && DoublingPart.Equals(other.DoublingPart) && TakePart.Equals(other.TakePart);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CubeAnswerCost other && Equals(other);

    /// <summary>A hash of the two parts, consistent with <see cref="Equals(CubeAnswerCost?)"/>.</summary>
    public override int GetHashCode() => HashCode.Combine(DoublingPart, TakePart);

    /// <summary>Whether the two are equal costs (<see cref="Equals(CubeAnswerCost?)"/>); two nulls are equal.</summary>
    public static bool operator ==(CubeAnswerCost? left, CubeAnswerCost? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>Whether the two are not equal costs (<see cref="Equals(CubeAnswerCost?)"/>).</summary>
    public static bool operator !=(CubeAnswerCost? left, CubeAnswerCost? right) => !(left == right);
}
