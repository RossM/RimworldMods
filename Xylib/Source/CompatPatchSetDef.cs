using System.Reflection;

namespace Xylib;

[UsedFromXml]
public class CompatPatch
{
    public required Type target;
    public required string targetMethod;
    public Type[]? targetParameters;
    public required Type patch;
    public required string patchMethod;

    public void Apply()
    {
        MethodInfo? targetInfo = GetMethod(target, targetMethod, targetParameters);
        MethodInfo? patchInfo = GetMethod(patch, patchMethod);

        if (targetInfo is null)
        {
            Log.Error($"Not found: {target}:{targetMethod}");
            return;
        }

        if (patchInfo is null)
        {
            Log.Error($"Not found: {patch}:{patchMethod}");
            return;
        }

        var patchConfig = Patcher.PatchesFromMethod(patchInfo).First().Of(targetInfo);
        Patcher.Patch(patchConfig);
    }

    private MethodInfo? GetMethod(Type type, string method, Type[]? parameters = null) =>
        parameters != null ? type.GetMethod(method, AccessTools.all, parameters) : type.GetMethod(method, AccessTools.all);
}

[UsedFromXml]
public class CompatPatchSet : Def
{
    public required List<CompatPatch> patches;

    public override void ResolveReferences()
    {
        base.ResolveReferences();

        foreach (var patch in patches)
            patch.Apply();
    }
}
