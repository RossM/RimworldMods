namespace Disharmony;

public enum PatchType
{
    Prefix,
    Postfix,
    InnerPrefix,
    InnerPostfix,
}

public static partial class Autopatcher
{
    private class StateBuilder
    {
        public List<Type> LocalTypes => output.LocalTypes;
        private readonly Dictionary<Type, (int index, Type type)> stateMap = new();
        private readonly InstructionList output = [];

        private int GetOrAddStateLocal(Type stateKey, Type localType, MethodInfo method)
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

        public IEnumerable<Rule> BuildRules()
        {
            if (LocalTypes.Count == 0)
                yield break;

            for (int index = 0; index < LocalTypes.Count; index++)
                output.EmitLocalInitializer(index);

            yield return new Rule
            {
                Mode = InstructionMatcher.OutputMode.MethodPrefix,
                Output = output.Instructions.ToArray(),
                Name = "state variable initialization",
            };
        }

        public void AssignStateVariableIndexes(List<PatchInfo> patches)
        {
            foreach (var patch in patches)
            {
                ParameterBinding[] parameters = patch.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].BindingType == BindingType.State)
                    {
                        parameters[i].Index = GetOrAddStateLocal(patch.patchMethod.DeclaringType,
                            parameters[i].Parameter.ParameterType, patch.patchMethod);
                    }
                }
            }
        }
    }

    private static readonly PatchRegistry registry = PatchRegistry.Instance;
    private static readonly Patcher patcher = Patcher.Instance;

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
        ApplyImpl(useTrampolines: true);
    }

    private static void ApplyImpl(bool useTrampolines)
    {
        foreach (MethodInfo patchedMethod in registry.MethodsToUpdate)
        {
            try
            {
                var worker = new PatchWorker(registry, patchedMethod, useTrampolines);

                worker.UpdateMethod();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
            }
        }

        registry.MethodsToUpdate.Clear();
    }

    public static void ForceApply()
    {
        ApplyImpl(useTrampolines: false);
        patcher.ResolveAllTrampolines();
    }
}
