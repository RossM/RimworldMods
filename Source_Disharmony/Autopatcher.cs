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
        private readonly Dictionary<TStateKey, (int index, Type type)> stateMap = new();
        private readonly InstructionList output = [];
        public List<Type> LocalTypes => output.LocalTypes;

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

    private static readonly Dictionary<MethodInfo, Action<InstructionMatcher[]>> transpilerUpdaters = new();

    private static readonly AssemblyBuilder assemblyBuilder
        = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" }, AssemblyBuilderAccess.RunAndSave);

    private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

    private static readonly PatchRegistry registry = new();

    private static readonly Harmony harmony = new("Xylthixlm.Disharmony.Autopatcher");

    public static void PatchAll(Assembly assembly)
    {
        RegisterAll(assembly);
        Apply();
    }

    private static void RegisterAll(Assembly assembly)
    {
        registry.CollectPatches(assembly);
    }

    private static void Apply()
    {
        var worker = new PatchWorker(registry);

        foreach (MethodInfo patchedMethod in registry.MethodsToUpdate)
        {
            try
            {
                HarmonyMethod harmonyMethod = worker.GetHarmonyMethod(patchedMethod);

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
            bool oldForceDebug = InstructionMatcher.forceDebug;

            FileLog.Log($"# RunPatch: {patchedMethod.FullName}");

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
}
