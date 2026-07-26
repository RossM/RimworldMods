using JetBrains.Annotations;
using HarmonyPatch = HarmonyLib.PatchInfo;

namespace Disharmony;

internal class Patcher
{
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

    private const string HarmonyID = "Xylthixlm.Disharmony.Autopatcher";

    public static readonly Patcher Instance = new();

    private readonly bool extraDebug = false;
    private readonly Module module;

    // These variables must only be accessed while HarmonyInternals.locker is held
    private readonly Dictionary<MethodBase, MethodInfo> trampolines = new();
    private int trampolineCount;

    private struct MethodPatch
    {
        public required InstructionMatcher[] matchers;
        public bool optimize;
        public bool debug;
    }

    private readonly Dictionary<MethodBase, MethodPatch> methodPatches = new();

    public bool trampolinesEnabled = true;
    public bool optimizerEnabled = false;

    public Patcher()
    {
        module = GetType().Module;
    }

    /// <summary>
    ///     This does the same thing as <see cref="Harmony.Patch" />> but must be called
    ///     while we are already holding <see cref="HarmonyInternals.locker" />.
    /// </summary>
    /// <param name="original"></param>
    private static void PatchDirectly(MethodBase original)
    {
        HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original) ?? new HarmonyPatch();

        MethodInfo replacement = HarmonyInternals.UpdateWrapper(original, patchInfo);
#if ENABLE_DISASSEMBLY
        if (patchInfo.transpilers.Any(p => p.debug && p.owner == HarmonyID))
            JitAssemblyLogger.TryLog(original, replacement);
#endif

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
        while (true)
        {
            lock (HarmonyInternals.locker)
            {
                if (trampolines.Count == 0)
                    return;
                var method = trampolines.Keys.First();
                if (extraDebug)
                    FileLog.Log($"!!! Resolving trampoline to {method.FullName}");

                PatchDirectly(method);
                trampolines.Remove(method);
            }
        }
    }

    // Must hold HarmonyInternals.locker
    public MethodInfo ApplyTrampoline(MethodBaseInvocation method)
    {
        if (trampolines.TryGetValue(method.MethodBase, out var existingTrampoline))
            return existingTrampoline;

        if (extraDebug)
            FileLog.Log($"!!! Applying trampoline to {method.FullName}");

        MethodInfo trampoline = MakeTrampoline(method);

        HarmonyInternals.DetourMethod(method.MethodBase, trampoline);

        trampolines[method.MethodBase] = trampoline;

        return trampoline;
    }

    [UsedImplicitly]
    private static List<CodeInstruction> Transpiler(
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var instructionsList = instructions.ToList();
        var patch = Instance.methodPatches[method];
        foreach (var matcher in patch.matchers)
        {
            matcher.MatchAndReplace(method, ref instructionsList, generator);
        }

        if (Instance.optimizerEnabled && patch.optimize)
        {
            var optimizer = new Optimizer(method, instructionsList, generator, debug: patch.debug);
            optimizer.Optimize();
            return optimizer.output.instructions;
        }

        return instructionsList;
    }

    private MethodInfo MakeTrampoline(MethodBaseInvocation target)
    {
        Type[] parameterTypes = target.ParameterTypes;

        trampolineCount++;
        var method = new DynamicMethod($"{target.FullName}_Trampoline{trampolineCount}", target.ReturnType,
            parameterTypes, module, true);

        ILGenerator generator = method.GetILGenerator();

        EmitLoadArguments(generator, parameterTypes);

        // Call ResolveTrampoline(), which generates the real patch and applies a detour
        generator.Emit(OpCodes.Ldtoken, target);
        generator.Emit(OpCodes.Call, InfoOf.GetMethodFromHandle);
        generator.Emit(OpCodes.Call, InfoOf.ResolveTrampoline);

        // Do a tail call to the original method, which will actually go to the newly installed patch
        generator.Emit(OpCodes.Tailcall);
        generator.Emit(OpCodes.Call, target);
        generator.Emit(OpCodes.Ret);

        return method;
    }

    private static void EmitLoadArguments(ILGenerator generator, Type[] parameterTypes)
    {
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
    }

    public void ApplyPatch(MethodBaseInvocation original, InstructionMatcher[] matchers, bool useTrampolines)
    {
        if (!trampolinesEnabled)
            useTrampolines = false;

        if (matchers.Length == 0)
            Unpatch(original);

        lock (HarmonyInternals.locker)
        {
            HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original.MethodBase) ?? new HarmonyPatch();

            bool debug = PatchRegistry.Instance.GetPatchesFor(original).Any(p => p.debug);
            bool optimize = PatchRegistry.Instance.GetPatchesFor(original).Any(p => p.optimize);

            if (!methodPatches.ContainsKey(original.MethodBase))
            {
                HarmonyMethod patcher = new(InfoOf.Transpiler, priority: Priority.LowerThanNormal) { debug = debug };

                patchInfo.transpilers =
                [
                    .. patchInfo.transpilers,
                    new Patch(patcher, patchInfo.transpilers.Length, HarmonyID),
                ];
            }

            methodPatches[original.MethodBase] = new()
            {
                matchers = matchers,
                optimize = optimize,
                debug = debug,
            };

            MethodInfo replacement;
            if (useTrampolines)
                replacement = ApplyTrampoline(original);
            else
            {
                replacement = HarmonyInternals.UpdateWrapper(original.MethodBase, patchInfo);
#if ENABLE_DISASSEMBLY
                if (patchInfo.transpilers.Any(p => p.debug && p.owner == HarmonyID))
                    JitAssemblyLogger.TryLog(original.MethodBase, replacement);
#endif
            }

            HarmonyInternals.UpdatePatchInfo(original.MethodBase, replacement, patchInfo);
        }
    }

    public void Unpatch(MethodBaseInvocation original)
    {
        lock (HarmonyInternals.locker)
        {
            if (!methodPatches.Remove(original.MethodBase))
                return;

            trampolines.Remove(original.MethodBase);

            HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original.MethodBase) ?? new HarmonyPatch();

            patchInfo.transpilers =
            [
                .. patchInfo.transpilers.Where(t => t.owner != HarmonyID),
            ];

            MethodInfo replacement = HarmonyInternals.UpdateWrapper(original.MethodBase, patchInfo);

            HarmonyInternals.UpdatePatchInfo(original.MethodBase, replacement, patchInfo);
        }
    }
}
