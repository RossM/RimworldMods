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

    private static readonly PatchRegistry registry = new();

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
                HarmonyMethod? harmonyMethod = worker.GetHarmonyMethod(patchedMethod);

                if (Patcher.useTrampolines)
                    Patcher.AddTranspilerWithoutPatching(patchedMethod, harmonyMethod);
                else
                    Patcher.RunPatch(patchedMethod, harmonyMethod);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error patching {patchedMethod.FullName}", e);
            }
        }

        registry.MethodsToUpdate.Clear();
    }
}
