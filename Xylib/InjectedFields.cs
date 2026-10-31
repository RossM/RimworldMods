using Waylith.Injection;

namespace Xylib;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global

public static class InjectedFields
{
    extension(Pawn pawn)
    {
        [InjectedField(Default = false)]
        public ref bool GenesGenerated => throw new InvalidOperationException("not injected");

        [InjectedField(Default = null)]
        internal ref GeneTracker_Xylib? GeneTracker_Xylib_Internal => throw new InvalidOperationException("not injected");

        [InjectedField(Default = null)]
        internal ref GeneAndHediffCache? GeneAndHediffCache_Internal => throw new InvalidOperationException("not injected");
    }
}
    