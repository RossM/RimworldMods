using System.Reflection;
using System.Reflection.Emit;

namespace Xylib.Patches;

[HarmonyPatch(typeof(GeneDef))]
internal static class Patch_GeneDef
{
    // GeneDef.ConfigErrors doesn't call Def.ConfigErrors, resulting in DefModExtension.ConfigErrors not getting called
    // for gene mod extensions. This breaks GeneWithComps config error reporting.
    //
    // This can't currently be replaced with a Disharmony patch because it needs to make a direct call to the base
    // method.
    [Feature("BUGFIX")]
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(GeneDef.ConfigErrors))]
    public static IEnumerable<CodeInstruction> ConfigErrors_Transpiler(
        MethodBase methodBase,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var local = generator.DeclareLocal(typeof(IEnumerable<string>));
        var label = generator.DefineLabel();

        foreach (var inst in instructions)
            yield return inst.opcode.Value == OpCodes.Ret.Value ? new(OpCodes.Br_S, label) : inst;

        yield return CodeInstruction.StoreLocal(local.LocalIndex).WithLabels(label);
        yield return CodeInstruction.LoadArgument(0);
        yield return new(OpCodes.Call, SymbolExtensions.GetMethodInfo((Def def) => def.ConfigErrors()));
        yield return CodeInstruction.LoadLocal(local.LocalIndex);
        yield return new(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => Enumerable.Concat<string>(null, null)));
        yield return new(OpCodes.Ret);
    }

    [Feature(typeof(DefModExtension_GeneWithComps))]
    [InnerPostfix(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
    [Target("GetDescriptionFull")]
    public static void GeneDef_customEffectDescriptions_Postfix(GeneDef __instance, ref List<string> __result)
    {
        var extraDescriptions = __instance.GetGeneEffectDescriptions().ToList();
        if (extraDescriptions.Count == 0)
            return;

        __result = __result is not { Count: > 0 } ? extraDescriptions : [.. __result, .. extraDescriptions];
    }

    [Feature(typeof(DefModExtension_GeneWithComps))]
    [Postfix]
    [Target("SpecialDisplayStats")]
    public static void GeneDef_SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
    {
        var defExt = __instance.Extension_GeneWithComps;
        if (defExt == null)
            return;

        __result = __result.Concat(defExt.SpecialDisplayStats(req));
    }
}
