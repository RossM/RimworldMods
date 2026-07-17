using JetBrains.Annotations;
using HarmonyPatch = HarmonyLib.PatchInfo;

namespace Disharmony;

internal class Patcher
{
    private static class InfoOf
    {
        public static readonly MethodInfo GetMethodFromHandle
            = SymbolExtensions.GetMethodInfo(() => MethodBase.GetMethodFromHandle(new RuntimeMethodHandle()));

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly MethodInfo ResolveTrampoline = SymbolExtensions.GetMethodInfo(() => Patcher.ResolveTrampoline);
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

    private readonly Dictionary<MethodInfo, Action<InstructionMatcher[]>> transpilerUpdaters = new();

    private readonly ModuleBuilder moduleBuilder;

    public bool trampolinesEnabled = true;

    private Patcher()
    {
        AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" },
            AssemblyBuilderAccess.RunAndSave);

        moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");
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
                FileLog.Log($"!!! Resolving trampoline to {method.FullName}");

                PatchDirectly(method);
            }

            trampolines.Clear();
        }
    }

    // Must hold HarmonyInternals.locker
    public MethodInfo ApplyTrampoline(MethodInfo method)
    {
        if (trampolines.TryGetValue(method, out var existingTrampoline))
            return existingTrampoline;

        FileLog.Log($"!!! Applying trampoline to {method.FullName}");

        trampolineCount++;
        var trampoline = MakeTrampoline(method, $"_Trampoline{trampolineCount}");

        HarmonyInternals.DetourMethod(method, trampoline);

        trampolines[method] = trampoline;

        return trampoline;
    }

    public MethodInfo MakeTranspiler(InstructionMatcher[] matchers, string typeName, MethodInfo key)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

        FieldBuilder fieldBuilder = typeBuilder.DefineField("matchers", typeof(InstructionMatcher[]),
            FieldAttributes.Public | FieldAttributes.Static);

        MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
            typeof(List<CodeInstruction>), [typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
        ILGenerator generator = methodBuilder.GetILGenerator();

        MethodInfo matchAndReplace = SymbolExtensions.GetMethodInfo(() => InstructionMatcher.RunMatchers);

        generator.Emit(OpCodes.Ldsfld, fieldBuilder);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Call, matchAndReplace);
        generator.Emit(OpCodes.Ret);

        Type type = typeBuilder.CreateType();
        FieldInfo field = type.GetField(fieldBuilder.Name);
        field.SetValue(null, matchers);
        transpilerUpdaters[key] = m => field.SetValue(null, m);
        return type.GetMethod(methodBuilder.Name);
    }

    private MethodInfo MakeTrampoline(MethodInfo target, string name)
    {
        Type[] parameterTypes = target.GetParameters().Types();
        if (!target.IsStatic)
        {
            if (target.DeclaringType.IsStruct())
                parameterTypes = [target.DeclaringType.MakeByRefType(), .. parameterTypes];
            else
                parameterTypes = [target.DeclaringType, .. parameterTypes];
        }

        var method = new DynamicMethod($"{target.DeclaringType?.FullName}.{target.Name}{name}", target.ReturnType, parameterTypes,
            moduleBuilder, true);

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
        generator.Emit(OpCodes.Ldtoken, target);
        generator.Emit(OpCodes.Call, InfoOf.GetMethodFromHandle);
        generator.Emit(OpCodes.Call, InfoOf.ResolveTrampoline);

        // Do a tail call to the original method, which will actually go to the newly installed patch
        generator.Emit(OpCodes.Tailcall);
        generator.Emit(OpCodes.Call, target);

        generator.Emit(OpCodes.Ret);

        return method;
    }

    public void ApplyPatch(MethodInfo original, InstructionMatcher[] matchers, bool useTrampolines)
    {
        if (!trampolinesEnabled)
            useTrampolines = false;

        HarmonyMethod? harmonyMethod;
        if (!transpilerUpdaters.TryGetValue(original, out var setter))
        {
            MethodInfo transpiler = MakeTranspiler(matchers,
                $"{original.DeclaringType?.FullName?.Replace('.', '_')}_{original.Name}_Transpiler", original);

            bool debug = PatchRegistry.Instance.PatchesByMethod[original].Any(p => p.debug);

            harmonyMethod = new(transpiler, priority: Priority.LowerThanNormal) { debug = debug };
        }
        else
        {
            setter(matchers);

            FileLog.Log($"# GetHarmonyMethod: Reusing transpiler for {original.FullName}");

            harmonyMethod = null;
        }

        lock (HarmonyInternals.locker)
        {
            HarmonyPatch patchInfo = HarmonyInternals.GetPatchInfo(original) ?? new HarmonyPatch();

            if (harmonyMethod != null)
            {
                patchInfo.transpilers =
                [
                    .. patchInfo.transpilers,
                    new Patch(harmonyMethod, patchInfo.transpilers.Length, harmonyID),
                ];
            }

            MethodInfo replacement;
            if (useTrampolines)
                replacement = ApplyTrampoline(original);
            else
                replacement = HarmonyInternals.UpdateWrapper(original, patchInfo);

            HarmonyInternals.UpdatePatchInfo(original, replacement, patchInfo);
        }
    }
}
