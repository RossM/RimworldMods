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

    private static MethodInfo MakeTranspiler(InstructionMatcher[] matchers, string typeName, bool debug)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

        FieldBuilder matchersField = typeBuilder.DefineField("matchers", typeof(InstructionMatcher[]),
            FieldAttributes.Public | FieldAttributes.Static);
        FieldBuilder debugField = typeBuilder.DefineField("debug", typeof(bool),
            FieldAttributes.Public | FieldAttributes.Static);

        MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
            typeof(List<CodeInstruction>), [typeof(MethodBase), typeof(IEnumerable<CodeInstruction>), typeof(ILGenerator)]);
        ILGenerator generator = methodBuilder.GetILGenerator();

        MethodInfo matchAndReplace = SymbolExtensions.GetMethodInfo(() => InstructionMatcher.RunMatchers);

        generator.Emit(OpCodes.Ldsfld, matchersField);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldsfld, debugField);
        generator.Emit(OpCodes.Call, matchAndReplace);
        generator.Emit(OpCodes.Ret);

        Type type = typeBuilder.CreateType();
        type.GetField(matchersField.Name).SetValue(null, matchers);
        type.GetField(debugField.Name).SetValue(null, debug);
        return type.GetMethod(methodBuilder.Name);
    }

    /// <summary>
    ///     This creates a rule that replaces all calls of a given method with calls of a given other method. The
    ///     new method's parameters will be filled with the values of the old method's parameters that have the
    ///     same name. If the old method doesn't have a parameter with that name, the parameters of the method
    ///     containing the call being modified are checked, and used if they match.
    ///     You can also use __instance to match the instance the method was invoked on, and __caller to match
    ///     the instance the calling method was invoked on.
    ///     If there isn't a parameter with a matching name, this will fall back to trying to match based
    ///     on parameter type, but this will give a warning.
    /// </summary>
    /// <param name="oldMember"></param>
    /// <param name="newMember"></param>
    /// <param name="minMatches"></param>
    /// <returns></returns>
    public static InstructionMatcher.Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember)
    {
        return new()
        {
            LateGenerator = (outer, _, generator) => RedirectRule_Core(generator, outer, oldMember, newMember, [], [], []),
        };
    }

    private static InstructionMatcher.Rule RedirectRule_Core(
        ILGenerator generator,
        MethodBase outer,
        MemberInfo inner,
        MethodInfo? replacement,
        List<PatchInfo> prefixes,
        List<PatchInfo> postfixes,
        List<Type> localTypes)
    {
        var methodPatchWorker = new RuleBuilder(generator, outer, inner, replacement, prefixes, postfixes, localTypes);

        return methodPatchWorker.BuildRule();
    }
}
