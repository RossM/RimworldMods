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

    private static readonly Dictionary<MethodInfo, int> patchVersions = new();

    private static readonly AssemblyBuilder assemblyBuilder
        = AppDomain.CurrentDomain.DefineDynamicAssembly(new() { Name = "DynamicTranspilersAssembly" }, AssemblyBuilderAccess.RunAndSave);

    private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTranspilersModule");

    private static int IncrementVersion(MethodInfo method)
    {
        if (!patchVersions.TryGetValue(method, out int version))
            version = 0;
        return patchVersions[method] = version + 1;
    }

    public static void PatchAll(Harmony harmony, Assembly assembly)
    {
        var registry = new PatchRegistry();

        registry.CollectPatches(assembly);

        var worker = new PatchWorker(registry);

        foreach (MethodInfo patchedMethod in registry.PatchedMethods)
        {
            try
            {
                MethodInfo patchTranspiler = worker.CreatePatchTranspiler(patchedMethod);
                bool debug = worker.ShouldDebug(patchedMethod);

                RunPatch(patchedMethod, patchTranspiler, Priority.Normal, debug);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
            }
        }

        void RunPatch(MethodInfo patchedMethod, MethodInfo patchTranspiler, int priority, bool debug)
        {
            bool oldForceDebug = InstructionMatcher.forceDebug;

            FileLog.Log($"# RunPatch: {patchedMethod.FullName}");

            try
            {
                harmony.Patch(patchedMethod, transpiler: new(patchTranspiler, priority: priority) { debug = debug });
            }
            catch (Exception)
            {
                // Rerun with debug on so we see what went wrong
                InstructionMatcher.forceDebug = true;
                harmony.Patch(patchedMethod, transpiler: new(patchTranspiler, priority: priority) { debug = true });
            }
            finally
            {
                InstructionMatcher.forceDebug = oldForceDebug;
            }
        }
    }

    private static MethodInfo MakeTranspiler(InstructionMatcher[] matchers, string typeName)
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
        return type.GetMethod(methodBuilder.Name);
    }

    public static InstructionMatcher.Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember)
    {
        return new InstructionMatcher.Rule
        {
            Min = 1,
            Max = 0,
            Mode = InstructionMatcher.OutputMode.Replace,
            Pattern = [new(OpCodes.Call, oldMember)],
            Output = [new(OpCodes.Call, newMember)],
            LocalTypes = [],
            Name = oldMember.FullName,
        };
    }
}
