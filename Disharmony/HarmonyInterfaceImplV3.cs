using HarmonyPatchInfo = HarmonyLib.PatchInfo;

namespace Disharmony;

internal sealed class HarmonyInterfaceImplV3 : HarmonyInterface
{
    private sealed class HarmonyInternalsContainer
    {
        public readonly object Locker = GetLocker();
        public readonly Func<MethodBase, HarmonyPatchInfo?> GetPatchInfo =
            Bind<Func<MethodBase, HarmonyPatchInfo?>>("HarmonyLib.HarmonySharedState", "GetPatchInfo");
        public readonly Action<MethodBase, MethodBase> DetourMethod =
            Bind<Action<MethodBase, MethodBase>>("HarmonyLib.PatchTools", "DetourMethod");
        public readonly Action<MethodBase, MethodInfo, byte[]> UpdatePatchInfo =
            Bind<Action<MethodBase, MethodInfo, byte[]>>("HarmonyLib.HarmonySharedState", "UpdatePatchInfo");
        public readonly Func<MethodBase, HarmonyPatchInfo, MethodInfo> UpdateWrapper =
            Bind<Func<MethodBase, HarmonyPatchInfo, MethodInfo>>("HarmonyLib.PatchFunctions", "UpdateWrapper");
        public readonly Action<HarmonyPatchInfo> ValidateSurvivingMetadata =
            Bind<Action<HarmonyPatchInfo>>("HarmonyLib.PatchInfo", "ValidateSurvivingMetadata", isStatic: false);
        public readonly Func<HarmonyPatchInfo, byte[]> SerializeValidated =
            Bind<Func<HarmonyPatchInfo, byte[]>>("HarmonyLib.PatchInfoSerialization", "SerializeValidated");

        public readonly Type InlineSignature = GetHarmonyType("HarmonyLib.InlineSignature");
        public readonly MethodInfo InlineSignatureParameters = GetInlineSignatureGetter("Parameters");
        public readonly MethodInfo InlineSignatureReturnType = GetInlineSignatureGetter("ReturnType");
        public readonly MethodInfo InlineSignatureHasThis = GetInlineSignatureGetter("HasThis");
    }

    private readonly HarmonyInternalsContainer internals = new();

    protected override object Locker => internals.Locker;

    private void InstallAndPublishTrampoline(MethodBase original, MethodInfo trampoline, HarmonyPatchInfo patchInfo)
    {
        // Trampolines bypass UpdateWrapper. Follow its validation/serialization order so an invalid
        // patch cannot replace the live method before its metadata is ready to publish.
        byte[] bytes = PreparePatchInfo(patchInfo);
        internals.DetourMethod(original, trampoline);
        internals.UpdatePatchInfo(original, trampoline, bytes);
    }

    private void PublishTrampoline(MethodBase original, MethodInfo trampoline, HarmonyPatchInfo patchInfo) =>
        internals.UpdatePatchInfo(original, trampoline, PreparePatchInfo(patchInfo));

    private byte[] PreparePatchInfo(HarmonyPatchInfo patchInfo)
    {
        internals.ValidateSurvivingMetadata(patchInfo);
        patchInfo.VersionCount++;
        return internals.SerializeValidated(patchInfo);
    }

    protected override Type InlineSignatureType => internals.InlineSignature;
    protected override List<object> InlineSignatureParameters(object signature) =>
        (List<object>)internals.InlineSignatureParameters.Invoke(signature, null)!;
    protected override object InlineSignatureReturnType(object signature) =>
        internals.InlineSignatureReturnType.Invoke(signature, null)!;
    protected override bool InlineSignatureHasThis(object signature) =>
        (bool)internals.InlineSignatureHasThis.Invoke(signature, null)!;

#if DEBUG
    internal override event Action? ApplyPatchHookForTesting = null;
#endif

    /// <summary>
    ///     Rebuilds the method with its registered patches while we hold <see cref="Locker" />.
    ///     V3's UpdateWrapper also publishes the patch information.
    /// </summary>
    /// <param name="original"></param>
    protected override Exception? PatchDirectly(MethodBase original)
    {
        HarmonyPatchInfo patchInfo = internals.GetPatchInfo(original) ?? new HarmonyPatchInfo();

        try
        {
            MethodInfo replacement = internals.UpdateWrapper(original, patchInfo);
#if ENABLE_DISASSEMBLY
            if (patchInfo.transpilers.Any(p => p.debug && p.owner == HarmonyID))
                JitAssemblyLogger.TryLog(original, replacement);
#endif
        }
        catch (Exception e)
        {
            patchInfo.transpilers =
            [
                .. patchInfo.transpilers.Where(t => t.owner != HarmonyID),
            ];

            internals.UpdateWrapper(original, patchInfo);
            return e;
        }

        return null;
    }

    // Must hold Locker
    private void ApplyTrampoline(MethodBaseInvocation method, HarmonyPatchInfo patchInfo)
    {
        if (trampolines.TryGetValue(method.MethodBase, out var existingTrampoline))
        {
            // Reusing the detour does not imply that the supplied patch metadata is already published.
            PublishTrampoline(method.MethodBase, existingTrampoline, patchInfo);
            return;
        }

        MethodInfo trampoline = MakeTrampoline(method);

        InstallAndPublishTrampoline(method.MethodBase, trampoline, patchInfo);

        trampolines[method.MethodBase] = trampoline;
    }

    public override void ApplyPatch(MethodBaseInvocation original, Ruleset ruleset, bool useTrampolines, bool debug, bool optimize)
    {
#if DEBUG
        ApplyPatchHookForTesting?.Invoke();
#endif

        lock (Locker)
        {
            HarmonyPatchInfo patchInfo = internals.GetPatchInfo(original.MethodBase) ?? new HarmonyPatchInfo();

            if (!methodPatches.ContainsKey(original.MethodBase))
            {
                HarmonyMethod patcher = new(InfoOf.HarmonyInterface_Transpiler, priority: Priority.LowerThanNormal - 1) { debug = debug };

                patchInfo.transpilers =
                [
                    .. patchInfo.transpilers,
                    new HarmonyLib.Patch(patcher, patchInfo.transpilers.Length, HarmonyID),
                ];
            }

            methodPatches[original.MethodBase] = new()
            {
                ruleset = ruleset,
                optimize = optimize,
                debug = debug,
            };

            if (useTrampolines)
                ApplyTrampoline(original, patchInfo);
            else
                try
                {
                    MethodInfo replacement = internals.UpdateWrapper(original.MethodBase, patchInfo);
#if ENABLE_DISASSEMBLY
                    if (patchInfo.transpilers.Any(p => p.debug && p.owner == HarmonyID))
                        JitAssemblyLogger.TryLog(original.MethodBase, replacement);
#endif
                }
                catch (Exception e)
                {
                    throw new RuntimePatchException($"Error patching {original.FullName}", e);
                }
        }
    }

    public override void Unpatch(MethodBase methodBase)
    {
        lock (Locker)
        {
            if (!methodPatches.Remove(methodBase))
                return;

            trampolines.Remove(methodBase);

            HarmonyPatchInfo patchInfo = internals.GetPatchInfo(methodBase) ?? new HarmonyPatchInfo();

            patchInfo.transpilers =
            [
                .. patchInfo.transpilers.Where(t => t.owner != HarmonyID),
            ];

            internals.UpdateWrapper(methodBase, patchInfo);
        }
    }

    private static TDelegate Bind<TDelegate>(string typeName, string methodName, bool isStatic = true)
        where TDelegate : Delegate
    {
        MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke")!;
        Type[] parameters = [.. invoke.GetParameters().Skip(isStatic ? 0 : 1).Select(p => p.ParameterType)];
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding
                             | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
        MethodInfo? method = GetHarmonyType(typeName).GetMethod(methodName, flags, null, parameters, null);
        if (method is null || method.ReturnType != invoke.ReturnType)
            throw Unsupported($"{typeName}.{methodName}");

        return AccessTools.MethodDelegate<TDelegate>(method);
    }

    private static object GetLocker() => GetHarmonyType("HarmonyLib.PatchProcessor")
        .GetField("locker", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)
        ?? throw Unsupported("HarmonyLib.PatchProcessor.locker");

    private static MethodInfo GetInlineSignatureGetter(string propertyName) =>
        GetHarmonyType("HarmonyLib.InlineSignature").GetProperty(propertyName)?.GetGetMethod()
        ?? throw Unsupported($"HarmonyLib.InlineSignature.{propertyName}");

}
