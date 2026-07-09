namespace XylXenos.Patches;

[HarmonyPatch(typeof(Thought))]
public static class Patch_Thought
{
    [Feature(nameof(Config.Feature.UI_Misc))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Thought.Description), MethodType.Getter)]
    public static void Description_Postfix(Thought __instance, ref string __result)
    {
        DebugAssert.NotNull(__instance.pawn);

        GeneDef? sourceGene =
            __instance.def.requiredGenes?.FirstOrDefault(geneDef => __instance.pawn.HasActiveGene(geneDef));
        if (sourceGene == null)
            return;

        // This is a minor UI improvement to show which gene caused a thought
        __result += "\n\n" +
                    ("IncapableOfTooltipGene".Translate() + ": " + sourceGene.LabelCap).Colorize(ColoredText
                        .GeneColor);
    }
}
