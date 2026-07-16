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
