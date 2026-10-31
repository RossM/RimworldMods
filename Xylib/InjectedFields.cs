using Waylith.Injection;

namespace Xylib;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ValueParameterNotUsed

public static class InjectedFields
{
    private static bool _dummyBool;

    extension(Pawn pawn)
    {
        [InjectedField(Default = false)]
        public ref bool GenesGenerated => ref _dummyBool;
    }
}
    