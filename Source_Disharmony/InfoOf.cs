using System.Runtime.ExceptionServices;

namespace Disharmony;

internal static class InfoOf
{
    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
    public static readonly MethodInfo GetMethodFromHandle
        = SymbolExtensions.GetMethodInfo(() => MethodBase.GetMethodFromHandle(new RuntimeMethodHandle()));

    public static readonly MethodInfo ResolveTrampoline = SymbolExtensions.GetMethodInfo(() => HarmonyInterface.ResolveTrampoline);

    public static readonly MethodInfo Transpiler = SymbolExtensions.GetMethodInfo(() => HarmonyInterface.Transpiler);

    public static readonly MethodInfo ExceptionDispatchInfo_Capture = SymbolExtensions.GetMethodInfo(() => ExceptionDispatchInfo.Capture);

    public static readonly MethodInfo RethrowException = SymbolExtensions.GetMethodInfo(() => RuntimeHelpers.RethrowException);
}
