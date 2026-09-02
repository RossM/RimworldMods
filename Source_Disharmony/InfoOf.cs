using System.Runtime.ExceptionServices;

namespace Disharmony;

internal static class InfoOf
{
    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
    public static readonly MethodInfo GetMethodFromHandle1
        = SymbolExtensions.GetMethodInfo(() => MethodBase.GetMethodFromHandle(new RuntimeMethodHandle()));

    // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
    public static readonly MethodInfo GetMethodFromHandle2
        = SymbolExtensions.GetMethodInfo(() => MethodBase.GetMethodFromHandle(new RuntimeMethodHandle(), new RuntimeTypeHandle()));

    public static readonly MethodInfo ResolveTrampoline = SymbolExtensions.GetMethodInfo(() => HarmonyInterface.ResolveTrampoline);

    public static readonly MethodInfo Transpiler = SymbolExtensions.GetMethodInfo(() => HarmonyInterface.Transpiler);

    public static readonly MethodInfo ExceptionDispatchInfo_Capture = SymbolExtensions.GetMethodInfo(() => ExceptionDispatchInfo.Capture);

    public static readonly MethodInfo RethrowException = SymbolExtensions.GetMethodInfo(() => RuntimeHelpers.RethrowException);
}
