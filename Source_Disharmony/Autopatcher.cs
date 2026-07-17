using JetBrains.Annotations;

namespace Disharmony;

public enum PatchType
{
    Prefix,
    Postfix,
    InnerPrefix,
    InnerPostfix,
}

[PublicAPI]
public static partial class Autopatcher
{
    // Lock order: applyLock, PatchRegistry.SyncRoot, Harmony's lock.
    private static readonly object applyLock = new();
    private static readonly PatchRegistry registry = PatchRegistry.Instance;
    private static readonly Patcher patcher = Patcher.Instance;

    public static void PatchAll(Assembly assembly)
    {
        RegisterAll(assembly);
        Apply();
    }

    public static void RegisterAll(Assembly assembly)
    {
        registry.ProcessAssembly(assembly);
    }

    public static void PatchCategory(Assembly assembly, string? category)
    {
        RegisterCategory(assembly, category);
        Apply();
    }

    public static void RegisterCategory(Assembly assembly, string? category)
    {
        registry.ProcessAssembly(assembly, category);
    }

    public static void Register(Type type)
    {
        registry.ProcessType(type.GetTypeInfo());
    }

    public static void Patch(Type type)
    {
        Register(type);
        Apply();
    }

    public static void Register(MethodInfo method)
    {
        registry.ProcessMethod(method);
    }

    public static void Patch(MethodInfo method)
    {
        Register(method);
        Apply();
    }

    public static void Apply()
    {
        ApplyImpl(useTrampolines: true);
    }

    private static void ApplyImpl(bool useTrampolines)
    {
        lock (applyLock)
        {
            lock (registry.SyncRoot)
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
        }
    }

    public static void ForceApply()
    {
        ApplyImpl(useTrampolines: false);
        patcher.ResolveAllTrampolines();
    }
}
