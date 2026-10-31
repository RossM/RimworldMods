using Waylith.Injection;

namespace XylXenos;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global

public static class InjectedFields
{
    extension(Pawn pawn)
    {
        [InjectedField(Default = null)]
        internal ref GeneTracker_XylXenos? GeneTracker_XylXenos_Internal => throw new InvalidOperationException("not injected");
    }
}
