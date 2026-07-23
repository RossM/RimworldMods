using JetBrains.Annotations;
using HarmonyPatch = HarmonyLib.PatchInfo;

namespace Disharmony;

internal class Patcher
{
    private readonly bool extraDebug = false;

    private static class InfoOf
    {
        // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
        public static readonly MethodInfo GetMethodFromHandle
            = SymbolExtensions.GetMethodInfo(() => MethodBase.GetMethodFromHandle(new RuntimeMethodHandle()));

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly MethodInfo ResolveTrampoline = SymbolExtensions.GetMethodInfo(() => Patcher.ResolveTrampoline);

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly MethodInfo Transpiler = SymbolExtensions.GetMethodInfo(() => Patcher.Transpiler);
    }

    private static class HarmonyInternals
    {
        public static readonly object locker = AccessTools.FieldRefAccess<object>("HarmonyLib.PatchProcessor:locker")();

        public static readonly Func<MethodBase, HarmonyPatch> GetPatchInfo
            = AccessTools.MethodDelegate<Func<MethodBase, HarmonyPatch>>("HarmonyLib.HarmonySharedState:GetPatchInfo");

        public static readonly Action<MethodBase, MethodBase> DetourMethod
            = AccessTools.MethodDelegate<Action<MethodBase, MethodBase>>("HarmonyLib.PatchTools:DetourMethod");

        public static readonly Action<MethodBase, MethodInfo, HarmonyPatch> UpdatePatchInfo
            = AccessTools.MethodDelegate<Action<MethodBase, MethodInfo, HarmonyPatch>>(
                "HarmonyLib.HarmonySharedState:UpdatePatchInfo");

        public static readonly Func<MethodBase, HarmonyPatch, MethodInfo> UpdateWrapper
            = AccessTools.MethodDelegate<Func<MethodBase, HarmonyPatch, MethodInfo>>("HarmonyLib.PatchFunctions:UpdateWrapper");
    }

    private const string harmonyID = "Xylthixlm.Disharmony.Autopatcher";

    public static readonly Patcher Instance = new();

    // These variables must only be accessed while HarmonyInternals.locker is held
    private readonly Dictionary<MethodBase, MethodInfo> trampolines = new();
    private int trampolineCount;

    private readonly Dictionary<MethodBase, InstructionMatcher[]> matchersByMethod = new();

    public bool trampolinesEnabled = true;

    /// <summary>
    ///     This does the same thing as <see cref="Harmony.Patch" />> but must be called
    ///     while we are already holding <see cref="HarmonyInternals.locker" />.
    /// </summary>
    /// <param name="original"></param>
    private static void PatchDirectly(MethodBase original)
    {
        HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original) ?? new HarmonyPatch();

        MethodInfo replacement = HarmonyInternals.UpdateWrapper(original, patchInfo);

        HarmonyInternals.UpdatePatchInfo(original, replacement, patchInfo);
    }

    public void ResolveTrampolineImpl(MethodBase method)
    {
        lock (HarmonyInternals.locker)
        {
            // If we can't remove the method, we lost a race and some other thread has
            // already replaced the trampoline
            if (!trampolines.Remove(method))
                return;

            if (extraDebug)
                FileLog.Log($"!!! Resolving trampoline to {method.FullName}");

            PatchDirectly(method);
        }
    }

    [UsedImplicitly]
    public static void ResolveTrampoline(MethodBase method)
    {
        Instance.ResolveTrampolineImpl(method);
    }

    public void ResolveAllTrampolines()
    {
        lock (HarmonyInternals.locker)
        {
            foreach (var method in trampolines.Keys)
            {
                if (extraDebug)
                    FileLog.Log($"!!! Resolving trampoline to {method.FullName}");

                PatchDirectly(method);
            }

            trampolines.Clear();
        }
    }

    // Must hold HarmonyInternals.locker
    public MethodInfo ApplyTrampoline(MethodBase method)
    {
        if (trampolines.TryGetValue(method, out var existingTrampoline))
            return existingTrampoline;

        if (extraDebug)
            FileLog.Log($"!!! Applying trampoline to {method.FullName}");

        MethodInfo trampoline = MakeTrampoline(MethodBaseInvocation.Create(method));

        HarmonyInternals.DetourMethod(method, trampoline);

        trampolines[method] = trampoline;

        return trampoline;
    }

    [UsedImplicitly]
    private static List<CodeInstruction> Transpiler(
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var instructionsList = instructions.ToList();
        foreach (var matcher in Instance.matchersByMethod[method])
        {
            matcher.MatchAndReplace(method, ref instructionsList, generator);
        }

        return instructionsList;
    }

    private MethodInfo MakeTrampoline(MethodBaseInvocation target)
    {
        Type[] parameterTypes = target.ParameterTypes;

        trampolineCount++;
        var method = new DynamicMethod($"{target.FullName}_Trampoline{trampolineCount}", target.ReturnType,
            parameterTypes, true);

        ILGenerator generator = method.GetILGenerator();

        // Load all arguments onto the stack
        if (parameterTypes.Length >= 1)
            generator.Emit(OpCodes.Ldarg_0);
        if (parameterTypes.Length >= 2)
            generator.Emit(OpCodes.Ldarg_1);
        if (parameterTypes.Length >= 3)
            generator.Emit(OpCodes.Ldarg_2);
        if (parameterTypes.Length >= 4)
            generator.Emit(OpCodes.Ldarg_3);
        for (int i = 4; i < parameterTypes.Length; i++)
            generator.Emit(OpCodes.Ldarg_S, i);

        // Call ResolveTrampoline(), which generates the real patch and applies a detour
        switch (target)
        {
            case MethodInvocation methodTarget: generator.Emit(OpCodes.Ldtoken, methodTarget.MethodInfo); break;
            case ConstructorInvocation constructorTarget: generator.Emit(OpCodes.Ldtoken, constructorTarget.ConstructorInfo); break;
            default: throw new NotSupportedException();
        }
        generator.Emit(OpCodes.Call, InfoOf.GetMethodFromHandle);
        generator.Emit(OpCodes.Call, InfoOf.ResolveTrampoline);

        switch (target)
        {
            case MethodInvocation methodTarget:
            {
                // Do a tail call to the original method, which will actually go to the newly installed patch
                // The IL verifier does not allow tail calls to be used with by-ref arguments, so skip the tailcall prefix if there are any
                if (!parameterTypes.Any(p => p.IsByRef))
                    generator.Emit(OpCodes.Tailcall);
                generator.Emit(OpCodes.Call, methodTarget.MethodInfo); break;
            }
            case ConstructorInvocation constructorTarget:
            {
                generator.Emit(OpCodes.Newobj, constructorTarget.ConstructorInfo);
                break;
            }
            default: throw new NotSupportedException();
        }

        generator.Emit(OpCodes.Ret);

        return method;
    }

    public void ApplyPatch(MethodBase original, InstructionMatcher[] matchers, bool useTrampolines)
    {
        if (!trampolinesEnabled)
            useTrampolines = false;

        if (matchers.Length == 0)
            Unpatch(original);

        lock (HarmonyInternals.locker)
        {
            HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original) ?? new HarmonyPatch();

            if (!matchersByMethod.ContainsKey(original))
            {
                bool debug = PatchRegistry.Instance.GetPatchesFor(original).Any(p => p.debug);

                HarmonyMethod harmonyMethod = new(InfoOf.Transpiler, priority: Priority.LowerThanNormal) { debug = debug };

                patchInfo.transpilers =
                [
                    .. patchInfo.transpilers,
                    new Patch(harmonyMethod, patchInfo.transpilers.Length, harmonyID),
                ];
            }

            matchersByMethod[original] = matchers;

            MethodInfo replacement;
            // Trampolines for constructors are currently bugged and do not correctly chain to the newly-patched constructor,
            // so disable trampolines for constructors.
            if (useTrampolines && original is MethodInfo)
                replacement = ApplyTrampoline(original);
            else
                replacement = HarmonyInternals.UpdateWrapper(original, patchInfo);

            HarmonyInternals.UpdatePatchInfo(original, replacement, patchInfo);
        }
    }

    public void Unpatch(MethodBase original)
    {
        lock (HarmonyInternals.locker)
        {
            if (!matchersByMethod.Remove(original))
                return;

            HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original) ?? new HarmonyPatch();

            patchInfo.transpilers =
            [
                .. patchInfo.transpilers.Where(t => t.owner != harmonyID),
            ];

            MethodInfo replacement = HarmonyInternals.UpdateWrapper(original, patchInfo);

            HarmonyInternals.UpdatePatchInfo(original, replacement, patchInfo);
        }
    }
}
