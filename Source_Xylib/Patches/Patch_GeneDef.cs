using System.Reflection;
using System.Reflection.Emit;

namespace Xylib.Patches;

[HarmonyPatch(typeof(GeneDef))]
internal static class Patch_GeneDef
{
    [Feature(typeof(DefModExtension_GeneWithComps))]
    [InfixPostfix(typeof(GeneDef), nameof(GeneDef.customEffectDescriptions))]
    [InfixPatch("GetDescriptionFull")]
    public static void GeneDef_customEffectDescriptions_Postfix(GeneDef __instance, ref List<string> __result)
    {
        var extraDescriptions = __instance.GetGeneEffectDescriptions().ToList();
        if (extraDescriptions.Count == 0)
            return;

        __result = __result is not { Count: > 0 } ? extraDescriptions : [.. __result, .. extraDescriptions];
    }

    [Feature(typeof(DefModExtension_GeneWithComps))]
    [HarmonyPostfix]
    [HarmonyPatch("SpecialDisplayStats")]
    public static void GeneDef_SpecialDisplayStats_Postfix(GeneDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
    {
        var defExt = __instance.Extension_GeneWithComps;
        if (defExt == null)
            return;

        __result = __result.Concat(defExt.SpecialDisplayStats(req));
    }

    // GeneDef.ConfigErrors doesn't call Def.ConfigErrors, resulting in DefModExtension.ConfigErrors not getting called
    // for gene mod extensions. This breaks GeneWithComps config error reporting.
    [Feature("BUGFIX")]
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(GeneDef.ConfigErrors))]
    public static IEnumerable<CodeInstruction> ConfigErrors_Transpiler(MethodBase methodBase, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var local = generator.DeclareLocal(typeof(IEnumerable<string>));

        foreach (var inst in instructions)
            if (inst.opcode.Value != OpCodes.Ret.Value)
                yield return inst;

        yield return CodeInstruction.StoreLocal(local.LocalIndex);
        yield return CodeInstruction.LoadArgument(0);
        yield return new(OpCodes.Call, AccessTools.Method(typeof(Def), nameof(Def.ConfigErrors)));
        yield return CodeInstruction.LoadLocal(local.LocalIndex);
        yield return new(OpCodes.Call, AccessTools.Method(typeof(Enumerable), nameof(Enumerable.Concat)).MakeGenericMethod(typeof(string)));
        yield return new(OpCodes.Ret);
    }
}
