using System.Text.Json.Serialization;

namespace BgDataTypes_Lib;

public class DecisionData
{
    /// <summary>Always length 2. Ignored when IsCube is true.</summary>
    public IReadOnlyList<int> Dice { get; init; } = new int[2];

    public IReadOnlyList<PlayCandidate> Plays { get; init; } = [];
    /// <summary>Index into Plays identifying the best play. </summary>
    public int BestPlayIndex { get; init; }
    /// <summary>
    /// Equity loss from the user's checker play vs. the best play (≥ 0).
    /// Null when no user play is recorded or IsCube is true.
    /// </summary>
    public double? UserPlayError { get; init; }
    /// <summary>Index into Plays identifying the user's play. -1 if not applicable.</summary>
    public int UserPlayIndex { get; init; } = -1;
    public bool IsCube { get; init; }

    // -----------------------------------------------------------------------
    //  Cube decision equity fields
    // -----------------------------------------------------------------------
    /// <summary>Analysis depth label for a cube decision, e.g. "3-ply",
    /// "Rollout: 1296 trials. 3-ply". Empty when IsCube is false.</summary>
    public string CubeDepth { get; init; } = string.Empty;

    /// <summary>Compact display form of CubeDepth. Empty when IsCube is false.</summary>
    public string CubeDepthAbbreviation { get; init; } = string.Empty;

    /// <summary>Ordinal ranking of CubeDepth; see PlayCandidate.DepthRank
    /// for semantics. Defaults to 0.</summary>
    public int CubeDepthRank { get; init; }
    public double NoDoubleEquity { get; init; }
    public double DoubleTakeEquity { get; init; }

    public double WinPctAfterNoDouble { get; init; }
    public double GammonPctAfterNoDouble { get; init; }
    public double BgPctAfterNoDouble { get; init; }
    public double LosePctAfterNoDouble { get; init; }
    public double LoseGammonPctAfterNoDouble { get; init; }
    public double LoseBgPctAfterNoDouble { get; init; }

    public double WinPctAfterDoubleTake { get; init; }
    public double GammonPctAfterDoubleTake { get; init; }
    public double BgPctAfterDoubleTake { get; init; }
    public double LosePctAfterDoubleTake { get; init; }
    public double LoseGammonPctAfterDoubleTake { get; init; }
    public double LoseBgPctAfterDoubleTake { get; init; }
    public double ProbOfOpponentErrorJustifyingDouble { get; init; }
    /// <summary>
    /// Equity loss from the user's doubling decision vs. the correct cube action (≥ 0).
    /// Null when no cube decision is recorded or IsCube is false.
    /// </summary>
    public double? UserDoubleError { get; init; }

    /// <summary>
    /// Equity loss from the user's take/drop decision vs. the correct response (≥ 0).
    /// Null when no cube decision is recorded or IsCube is false.
    /// </summary>
    public double? UserTakeError { get; init; }

    // -----------------------------------------------------------------------
    //  Cube-decision scoring helpers
    // -----------------------------------------------------------------------
    //
    //  Single-source policy for judging a cube decision. Two parallel views:
    //
    //    * Verdict-level helpers — quiz scoring, aggregate (BestVerdict,
    //      VerdictEquityLoss, and its DDE / TDE components). Encode the
    //      NoDouble↔TooGood strategic-confusion override: when the user's
    //      verdict matches the correct atomic doubler action but differs
    //      strategically (e.g. plays NoDouble when TooGood is correct), the
    //      Doubler-Decision-Equity (DDE) is bumped from 0 to the
    //      Take-Decision-Equity (TDE) so the total penalty reflects both
    //      halves of the misjudgement.
    //
    //    * Atomic-action helpers — stats / substrate, "two decisions
    //      evaluated separately" (BestDoublerAction, BestResponderAction,
    //      DoublerActionError, ResponderActionError). Pure equity-loss with
    //      no strategic-confusion overrides.
    //
    //  All eight throw InvalidOperationException when IsCube is false —
    //  they are only meaningful on cube decisions, and silent zero / default
    //  returns on play decisions would mask misuse.

    /// <summary>
    /// Equity the doubler earns when the opponent passes a double — always
    /// 1.0 per cube-equity normalisation. A pass forfeits exactly one cube
    /// by definition, independent of match score or cube value.
    /// </summary>
    private const double PassEquity = 1.0;

    /// <summary>
    /// The correct cube verdict for this decision — the four-way aggregate
    /// judgement combining the doubler's double/no-double half with the
    /// opponent's (possibly notional) take/pass half.
    /// </summary>
    /// <remarks>
    /// Regime boundaries:
    /// <list type="bullet">
    ///   <item><c>TooGood</c> when <c>NoDoubleEquity &gt;= 1</c>.</item>
    ///   <item><c>DoublePass</c> when <c>NoDoubleEquity &lt; 1</c> and
    ///         <c>DoubleTakeEquity &gt;= 1</c>.</item>
    ///   <item><c>DoubleTake</c> when <c>DoubleTakeEquity &lt; 1</c> and
    ///         <c>DoubleTakeEquity &gt; NoDoubleEquity</c>.</item>
    ///   <item><c>NoDouble</c> otherwise.</item>
    /// </list>
    /// Ties favour the more conservative side (NoDouble on the doubler half,
    /// Pass on the responder half, TooGood on the NoDouble↔TooGood boundary).
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    [JsonIgnore]
    public CubeVerdict BestVerdict
    {
        get
        {
            RequireCube();
            if (NoDoubleEquity >= PassEquity) return CubeVerdict.TooGood;
            double doubleEquity = Math.Min(DoubleTakeEquity, PassEquity);
            if (doubleEquity > NoDoubleEquity)
                return DoubleTakeEquity < PassEquity
                    ? CubeVerdict.DoubleTake
                    : CubeVerdict.DoublePass;
            return CubeVerdict.NoDouble;
        }
    }

    /// <summary>
    /// The correct atomic doubler action — <see cref="CubeAction.Double"/>
    /// if doubling has higher equity than not doubling against optimal
    /// opponent response, <see cref="CubeAction.NoDouble"/> otherwise.
    /// </summary>
    /// <remarks>
    /// Atomic counterpart to <see cref="BestVerdict"/>: this view collapses
    /// NoDouble / TooGood into a single <see cref="CubeAction.NoDouble"/>,
    /// since both decline to offer the cube. Tie (<c>min(DoubleTakeEquity,
    /// 1) == NoDoubleEquity</c>) favours <see cref="CubeAction.NoDouble"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    [JsonIgnore]
    public CubeAction BestDoublerAction
    {
        get
        {
            RequireCube();
            return Math.Min(DoubleTakeEquity, PassEquity) > NoDoubleEquity
                ? CubeAction.Double
                : CubeAction.NoDouble;
        }
    }

    /// <summary>
    /// The correct atomic responder action — <see cref="CubeAction.Take"/>
    /// when taking yields better responder equity than passing,
    /// <see cref="CubeAction.Pass"/> otherwise.
    /// </summary>
    /// <remarks>
    /// Determined from the doubler's <see cref="DoubleTakeEquity"/>: the
    /// responder's take equity is its negation, and pass equity is
    /// <c>-1</c>. Tie (<c>DoubleTakeEquity == 1</c>) favours
    /// <see cref="CubeAction.Pass"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    [JsonIgnore]
    public CubeAction BestResponderAction
    {
        get
        {
            RequireCube();
            return DoubleTakeEquity < PassEquity
                ? CubeAction.Take
                : CubeAction.Pass;
        }
    }

    /// <summary>
    /// Equity loss the doubler incurs by choosing <paramref name="action"/>
    /// rather than the optimal doubler action — <c>0</c> if
    /// <paramref name="action"/> matches <see cref="BestDoublerAction"/>,
    /// otherwise the positive equity gap.
    /// </summary>
    /// <remarks>
    /// Atomic view: no strategic-confusion override. <c>Double</c>'s value
    /// is computed against optimal opponent response
    /// (<c>min(DoubleTakeEquity, 1)</c>); <c>NoDouble</c>'s value is
    /// <see cref="NoDoubleEquity"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Double"/> or <see cref="CubeAction.NoDouble"/>.
    /// </exception>
    public double DoublerActionError(CubeAction action)
    {
        RequireCube();
        double actionEquity = action switch
        {
            CubeAction.Double   => Math.Min(DoubleTakeEquity, PassEquity),
            CubeAction.NoDouble => NoDoubleEquity,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action,
                "DoublerActionError requires a doubler-half action (Double or NoDouble).")
        };
        double bestEquity = Math.Max(Math.Min(DoubleTakeEquity, PassEquity), NoDoubleEquity);
        return Math.Max(0.0, bestEquity - actionEquity);
    }

    /// <summary>
    /// Equity loss the responder incurs by choosing <paramref name="action"/>
    /// rather than the optimal responder action — <c>0</c> if
    /// <paramref name="action"/> matches <see cref="BestResponderAction"/>,
    /// otherwise the positive equity gap (measured from the responder's
    /// perspective).
    /// </summary>
    /// <remarks>
    /// Atomic view: no strategic-confusion override. Responder equities are
    /// the doubler's negated: <c>Take</c> ⇒ <c>-DoubleTakeEquity</c>;
    /// <c>Pass</c> ⇒ <c>-1</c>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="action"/> is not
    /// <see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>.
    /// </exception>
    public double ResponderActionError(CubeAction action)
    {
        RequireCube();
        double actionEquity = action switch
        {
            CubeAction.Take => -DoubleTakeEquity,
            CubeAction.Pass => -PassEquity,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action,
                "ResponderActionError requires a responder-half action (Take or Pass).")
        };
        double bestEquity = Math.Max(-DoubleTakeEquity, -PassEquity);
        return Math.Max(0.0, bestEquity - actionEquity);
    }

    /// <summary>
    /// Equity loss attributable to the responder half of
    /// <paramref name="verdict"/> — the <c>TDE</c> component of
    /// <see cref="VerdictEquityLoss"/>. Equivalent to
    /// <see cref="ResponderActionError"/> applied to the verdict's
    /// responder action.
    /// </summary>
    /// <remarks>
    /// For verdicts whose responder action is notional rather than actual
    /// (<see cref="CubeVerdict.NoDouble"/>'s implicit <c>Take</c>,
    /// <see cref="CubeVerdict.TooGood"/>'s implicit <c>Pass</c>), the
    /// scoring treats the notional action uniformly with a played one.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="verdict"/> is not a defined
    /// <see cref="CubeVerdict"/> member.
    /// </exception>
    public double TakeDecisionEquityLoss(CubeVerdict verdict)
        => ResponderActionError(CubeActionPair.FromVerdict(verdict).Responder);

    /// <summary>
    /// Equity loss attributable to the doubler half of
    /// <paramref name="verdict"/> — the <c>DDE</c> component of
    /// <see cref="VerdictEquityLoss"/>.
    /// </summary>
    /// <remarks>
    /// Baseline is <see cref="DoublerActionError"/> applied to the verdict's
    /// doubler action — <c>0</c> when the atomic doubler action matches
    /// <see cref="BestDoublerAction"/>. A strategic-confusion override
    /// applies on the NoDouble↔TooGood pair: when
    /// <paramref name="verdict"/> is one of <c>NoDouble</c> / <c>TooGood</c>
    /// and <see cref="BestVerdict"/> is the other, the DDE is bumped from
    /// <c>0</c> to <see cref="TakeDecisionEquityLoss"/> so the total
    /// penalty reflects both halves of the misjudgement. The DoubleTake↔
    /// DoublePass pair does <em>not</em> trigger the override — its
    /// atomic-baseline DDE of <c>0</c> stands.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="verdict"/> is not a defined
    /// <see cref="CubeVerdict"/> member.
    /// </exception>
    public double DoublerDecisionEquityLoss(CubeVerdict verdict)
    {
        RequireCube();
        var pair = CubeActionPair.FromVerdict(verdict);
        if (IsNoDoubleTooGoodConfusion(verdict, BestVerdict))
            return TakeDecisionEquityLoss(verdict);
        return DoublerActionError(pair.Doubler);
    }

    /// <summary>
    /// Total equity loss attributable to <paramref name="verdict"/> — the
    /// sum of its doubler-decision and take-decision components.
    /// </summary>
    /// <remarks>
    /// <c>VerdictEquityLoss(v) ==
    /// DoublerDecisionEquityLoss(v) + TakeDecisionEquityLoss(v)</c>. See
    /// <see cref="DoublerDecisionEquityLoss"/> for the NoDouble↔TooGood
    /// strategic-confusion override that distinguishes verdict-level
    /// scoring from atomic-level scoring.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IsCube"/> is <see langword="false"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="verdict"/> is not a defined
    /// <see cref="CubeVerdict"/> member.
    /// </exception>
    public double VerdictEquityLoss(CubeVerdict verdict)
        => DoublerDecisionEquityLoss(verdict) + TakeDecisionEquityLoss(verdict);

    private void RequireCube()
    {
        if (!IsCube)
            throw new InvalidOperationException(
                "Cube-decision scoring helpers require IsCube to be true.");
    }

    private static bool IsNoDoubleTooGoodConfusion(CubeVerdict verdict, CubeVerdict best)
        => (verdict == CubeVerdict.NoDouble && best == CubeVerdict.TooGood)
        || (verdict == CubeVerdict.TooGood  && best == CubeVerdict.NoDouble);
}