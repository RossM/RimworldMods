using JetBrains.Annotations;

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

        public static readonly Func<MethodBase, HarmonyLib.PatchInfo> GetPatchInfo
            = AccessTools.MethodDelegate<Func<MethodBase, HarmonyLib.PatchInfo>>("HarmonyLib.HarmonySharedState:GetPatchInfo");

        public static readonly Action<MethodBase, MethodBase> DetourMethod
            = AccessTools.MethodDelegate<Action<MethodBase, MethodBase>>("HarmonyLib.PatchTools:DetourMethod");

        public static readonly Action<MethodBase, MethodInfo, HarmonyLib.PatchInfo> UpdatePatchInfo
            = AccessTools.MethodDelegate<Action<MethodBase, MethodInfo, HarmonyLib.PatchInfo>>(
                "HarmonyLib.HarmonySharedState:UpdatePatchInfo");
    }

    private const string harmonyID = "Xylthixlm.Disharmony.Autopatcher";

    // These variables must only be access while trampolineLock is held
    private static readonly object trampolineLock = new();
    private static readonly Dictionary<MethodBase, MethodInfo> trampolines = new();
    private static int trampolineCount;

    private static readonly Dictionary<MethodInfo, Action<InstructionMatcher[]>> transpilerUpdaters = new();

    private static readonly AssemblyBuilder assemblyBuilder
        = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" }, AssemblyBuilderAccess.RunAndSave);

    private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

    private static readonly Harmony harmony = new(harmonyID);

    public static readonly bool useTrampolines = true;

    [UsedImplicitly]
    public static void ResolveTrampoline(MethodBase method)
    {
        lock (trampolineLock)
        {
            // If we can't remove the method, we lost a race and some other thread has
            // already replaced the trampoline
            if (!trampolines.Remove(method))
                return;

            FileLog.Log($"!!! Resolving trampoline to {method.FullName}");

            harmony.Patch(method);
        }
    }

    public static MethodInfo ApplyTrampoline(MethodInfo method)
    {
        lock (trampolineLock)
        {
            if (trampolines.TryGetValue(method, out var existingTrampoline))
                return existingTrampoline;

            FileLog.Log($"!!! Applying trampoline to {method.FullName}");

            string trampolineName = $"_Trampoline{trampolineCount}";
            trampolineCount++;
            var trampoline = MakeTrampoline(method, trampolineName);

            HarmonyInternals.DetourMethod(method, trampoline);

            trampolines[method] = trampoline;

            return trampoline;
        }
    }

    public static void RunPatch(MethodInfo patchedMethod, HarmonyMethod? harmonyMethod)
    {
        FileLog.Log($"# RunPatch: {patchedMethod.FullName}");

        bool oldForceDebug = InstructionMatcher.forceDebug;

        try
        {
            harmony.Patch(patchedMethod, transpiler: harmonyMethod);
        }
        catch (Exception)
        {
            // Rerun with debug on so we see what went wrong
            InstructionMatcher.forceDebug = true;
            harmonyMethod?.debug = true;
            harmony.Patch(patchedMethod, transpiler: harmonyMethod);
        }
        finally
        {
            InstructionMatcher.forceDebug = oldForceDebug;
        }
    }

    public static void AddTranspilerWithoutPatching(MethodInfo original, HarmonyMethod? harmonyMethod)
    {
        lock (HarmonyInternals.locker)
        {
            HarmonyLib.PatchInfo patchInfo = HarmonyInternals.GetPatchInfo(original) ?? new HarmonyLib.PatchInfo();

            if (harmonyMethod != null)
            {
                patchInfo.transpilers =
                [
                    .. patchInfo.transpilers,
                    new Patch(harmonyMethod, patchInfo.transpilers.Length, harmonyID),
                ];
            }

            var replacement = ApplyTrampoline(original);

            HarmonyInternals.UpdatePatchInfo(original, replacement, patchInfo);
        }
    }

    public static bool TryUpdateTranspiler(MethodInfo key, InstructionMatcher[] matchers)
    {
        if (!transpilerUpdaters.TryGetValue(key, out var setter))
            return false;

        setter(matchers);
        return true;
    }

    public static MethodInfo MakeTranspiler(InstructionMatcher[] matchers, string typeName, MethodInfo key)
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

    private static MethodInfo MakeTrampoline(MethodInfo target, string name)
    {
        Type[] parameterTypes = target.GetParameters().Types();
        if (!target.IsStatic)
            parameterTypes = [target.DeclaringType, .. parameterTypes];

        var method = new DynamicMethod($"{target.DeclaringType?.FullName}.{target.Name}{name}", target.ReturnType, parameterTypes,
            moduleBuilder, true);

        ILGenerator generator = method.GetILGenerator();

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

        generator.Emit(OpCodes.Ldtoken, target);
        generator.Emit(OpCodes.Call, InfoOf.GetMethodFromHandle);
        generator.Emit(OpCodes.Call, InfoOf.ResolveTrampoline);

        generator.Emit(OpCodes.Tailcall);
        generator.Emit(OpCodes.Call, target);

        generator.Emit(OpCodes.Ret);

        return method;
    }
}
