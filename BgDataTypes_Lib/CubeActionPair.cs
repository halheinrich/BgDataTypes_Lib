namespace BgDataTypes_Lib;

/// <summary>
/// The structural pair of atomic <see cref="CubeAction"/> values composing
/// a complete cube decision: the on-roll player's <see cref="Doubler"/>
/// action (<see cref="CubeAction.NoDouble"/> or <see cref="CubeAction.Double"/>)
/// and the opponent's <see cref="Responder"/> action
/// (<see cref="CubeAction.Take"/> or <see cref="CubeAction.Pass"/>).
/// </summary>
/// <remarks>
/// Carries the bidirectional mapping to/from <see cref="CubeVerdict"/>, the
/// quiz-facing aggregate form. Only four of the sixteen possible
/// <c>(CubeAction, CubeAction)</c> combinations correspond to valid cube
/// verdicts; the inverse mapping (<see cref="ToVerdict"/> /
/// <see cref="TryToVerdict"/>) is partial by construction.
///
/// The four correspondences are single-sourced in <see cref="FromVerdict"/> —
/// the inverse methods derive from it by linear scan rather than mirror its
/// switch. Adding a new <see cref="CubeVerdict"/> member (e.g. a future
/// beaver-related verdict) is a one-place edit there; the inverse methods
/// pick the new entry up automatically.
/// </remarks>
public readonly record struct CubeActionPair(CubeAction Doubler, CubeAction Responder)
{
    /// <summary>
    /// Decomposes a <see cref="CubeVerdict"/> into its atomic
    /// (<see cref="Doubler"/>, <see cref="Responder"/>) pair. Total over
    /// the defined enum members; throws on an out-of-range cast.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="verdict"/> is not a defined
    /// <see cref="CubeVerdict"/> member (e.g. <c>(CubeVerdict)42</c>).
    /// </exception>
    public static CubeActionPair FromVerdict(CubeVerdict verdict) => verdict switch
    {
        CubeVerdict.NoDouble   => new(CubeAction.NoDouble, CubeAction.Take),
        CubeVerdict.DoubleTake => new(CubeAction.Double,   CubeAction.Take),
        CubeVerdict.DoublePass => new(CubeAction.Double,   CubeAction.Pass),
        CubeVerdict.TooGood    => new(CubeAction.NoDouble, CubeAction.Pass),
        _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
    };

    /// <summary>
    /// Composes this <see cref="CubeActionPair"/> into its corresponding
    /// <see cref="CubeVerdict"/>. Partial by construction — only four of
    /// the sixteen possible pairs are valid verdicts.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when this pair does not correspond to any defined
    /// <see cref="CubeVerdict"/>.
    /// </exception>
    public CubeVerdict ToVerdict()
    {
        foreach (CubeVerdict v in Enum.GetValues<CubeVerdict>())
        {
            if (FromVerdict(v) == this) return v;
        }
        throw new ArgumentException(
            $"({Doubler}, {Responder}) is not a valid cube verdict.");
    }

    /// <summary>
    /// Attempts to compose <paramref name="pair"/> into its corresponding
    /// <see cref="CubeVerdict"/>. The standard .NET <c>Try</c>-pattern for
    /// the partial inverse — returns <see langword="false"/> on the twelve
    /// invalid pairs without throwing.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pair"/> corresponds to a
    /// defined <see cref="CubeVerdict"/>; otherwise <see langword="false"/>.
    /// </returns>
    public static bool TryToVerdict(CubeActionPair pair, out CubeVerdict verdict)
    {
        foreach (CubeVerdict v in Enum.GetValues<CubeVerdict>())
        {
            if (FromVerdict(v) == pair)
            {
                verdict = v;
                return true;
            }
        }
        verdict = default;
        return false;
    }
}
