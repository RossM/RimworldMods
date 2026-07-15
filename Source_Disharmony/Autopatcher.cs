using JetBrains.Annotations;

namespace Disharmony;

public enum PatchType
{
    InnerPrefix,
    InnerPostfix,
}

public static partial class Autopatcher
{
    private class StateBuilder<TStateKey>
    {
        public List<Type> LocalTypes => output.LocalTypes;
        private readonly Dictionary<TStateKey, (int index, Type type)> stateMap = new();
        private readonly InstructionList output = [];

        public int GetOrAddStateLocal(TStateKey stateKey, Type localType, MethodInfo method)
        {
            if (localType.IsByRef)
                localType = localType.GetElementType();

            if (stateMap.TryGetValue(stateKey, out var tuple))
            {
                (int index, Type existingType) = tuple;

                if (existingType == localType)
                    return index;

                throw new ArgumentException(
                    $"{method.FullName} declares __state of type {localType} which conflicts with existing type {existingType}");
            }

            int newIndex = LocalTypes.Count;
            stateMap.Add(stateKey, (newIndex, localType));
            LocalTypes.Add(localType);
            return newIndex;
        }

        public InstructionMatcher.Rule BuildRule()
        {
            for (int index = 0; index < LocalTypes.Count; index++)
                output.EmitLocalInitializer(index);

            return new InstructionMatcher.Rule
            {
                Mode = InstructionMatcher.OutputMode.MethodPrefix,
                Output = output.Instructions.ToArray(),
                Name = "state variable initialization",
            };
        }
    }

    private const string harmonyID = "Xylthixlm.Disharmony.Autopatcher";

    private static readonly Dictionary<MethodInfo, Action<InstructionMatcher[]>> transpilerUpdaters = new();

    private static readonly AssemblyBuilder assemblyBuilder
        = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" }, AssemblyBuilderAccess.RunAndSave);

    private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

    private static readonly PatchRegistry registry = new();

    private static readonly Harmony harmony = new(harmonyID);

    private static readonly bool useTrampolines = true;

    private static readonly object harmonyInternal_locker = AccessTools.FieldRefAccess<object>("HarmonyLib.PatchProcessor:locker")();

    private static readonly Func<MethodBase, HarmonyLib.PatchInfo> harmonyInternal_GetPatchInfo
        = AccessTools.MethodDelegate<Func<MethodBase, HarmonyLib.PatchInfo>>("HarmonyLib.HarmonySharedState:GetPatchInfo");

    private static readonly Action<MethodBase, MethodBase> harmonyInternal_DetourMethod
        = AccessTools.MethodDelegate<Action<MethodBase, MethodBase>>("HarmonyLib.PatchTools:DetourMethod");

    private static readonly Action<MethodBase, MethodInfo, HarmonyLib.PatchInfo> harmonyInternal_UpdatePatchInfo
        = AccessTools.MethodDelegate<Action<MethodBase, MethodInfo, HarmonyLib.PatchInfo>>("HarmonyLib.HarmonySharedState:UpdatePatchInfo");

    public static void PatchAll(Assembly assembly)
    {
        RegisterAll(assembly);
        Apply();
    }

    public static void RegisterAll(Assembly assembly)
    {
        registry.CollectPatches(assembly);
    }

    public static void PatchCategory(Assembly assembly, string? category)
    {
        RegisterCategory(assembly, category);
        Apply();
    }

    public static void RegisterCategory(Assembly assembly, string? category)
    {
        registry.CollectPatches(assembly, category);
    }

    public static void Apply()
    {
        var worker = new PatchWorker(registry);

        foreach (MethodInfo patchedMethod in registry.MethodsToUpdate)
        {
            try
            {
                HarmonyMethod harmonyMethod = worker.GetHarmonyMethod(patchedMethod);

                if (useTrampolines)
                    AddTranspilerWithoutPatching(patchedMethod, harmonyMethod);
                else
                    RunPatch(patchedMethod, harmonyMethod);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
            }
        }

        registry.MethodsToUpdate.Clear();
        return;

        void RunPatch(MethodInfo patchedMethod, HarmonyMethod? harmonyMethod)
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
    }

    private static void AddTranspilerWithoutPatching(MethodInfo original, HarmonyMethod? harmonyMethod)
    {
        lock (harmonyInternal_locker)
        {
            HarmonyLib.PatchInfo patchInfo = harmonyInternal_GetPatchInfo(original) ?? new HarmonyLib.PatchInfo();

            if (harmonyMethod != null)
            {
                patchInfo.transpilers =
                [
                    .. patchInfo.transpilers,
                    new Patch(harmonyMethod, patchInfo.transpilers.Length, harmonyID),
                ];
            }

            var replacement = PatchWorker.ApplyTrampoline(original);

            harmonyInternal_UpdatePatchInfo(original, replacement, patchInfo);
        }
    }

    private static bool TryUpdateTranspiler(MethodInfo key, InstructionMatcher[] matchers)
    {
        if (!transpilerUpdaters.TryGetValue(key, out var setter))
            return false;

        setter(matchers);
        return true;
    }

    private static MethodInfo MakeTranspiler(InstructionMatcher[] matchers, string typeName, MethodInfo key)
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

        MethodInfo getMethodFromHandle = SymbolExtensions.GetMethodInfo(() => MethodBase.GetMethodFromHandle(new RuntimeMethodHandle()));
        MethodInfo updateMethod = SymbolExtensions.GetMethodInfo(() => PatchWorker.ResolveTrampoline);

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
        generator.Emit(OpCodes.Call, getMethodFromHandle);
        generator.Emit(OpCodes.Call, updateMethod);

        generator.Emit(OpCodes.Tailcall);
        generator.Emit(OpCodes.Call, target);

        generator.Emit(OpCodes.Ret);

        return method;
    }
}
