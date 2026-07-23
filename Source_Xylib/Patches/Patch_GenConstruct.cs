namespace Xylib.Patches;

[HarmonyPatch(typeof(GenConstruct))]
internal static class Patch_GenConstruct
{
    private static readonly AccessTools.FieldRef<List<string>> tmpIdeoMemberNames
        = AccessTools.StaticFieldRefAccess<List<string>>(AccessTools.Field(typeof(GenConstruct), "tmpIdeoMemberNames"));

    [Feature(typeof(GeneCompProperties_UnlockBuildables))]
    [InnerPostfix(typeof(Ideo), nameof(Ideo.MembersCanBuild))]
    [Target(nameof(GenConstruct.CanConstruct), typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef))]
    public static void MembersCanBuild_Postfix(Ideo __instance, Thing thing, Pawn p, ref bool __result)
    {
        if (__result)
            return;

        if (__instance != p.Ideo)
            return;

        BuildableDef def = thing.def.entityDefToBuild ?? thing.def;

        bool hasGeneDesignator = p.GeneTracker_Xylib?.unlockedBuildables?.Contains(def) ?? false;
        if (!hasGeneDesignator && tmpIdeoMemberNames()!.Count == 0)
        {
            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefs)
            {
                if (gene.CompProps<GeneCompProperties_UnlockBuildables>()?.buildables.Contains(def) is true)
                    tmpIdeoMemberNames()!.Add("XylCharactersWithGene".Translate(gene.LabelCap));
            }
        }

        __result = hasGeneDesignator;
    }
}
