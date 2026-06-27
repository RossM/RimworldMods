namespace XylXenos.Patches;

[HarmonyPatch(typeof(GenConstruct))]
public static class Patch_GenConstruct
{
    [Feature(typeof(GeneCompProperties_UnlockBuildables))]
    [InfixPostfix(typeof(Ideo), nameof(Ideo.MembersCanBuild))]
    [InfixPatch(nameof(GenConstruct.CanConstruct), [typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool), typeof(JobDef)])]
    public static void MembersCanBuild_Postfix(Ideo __instance, Thing thing, Pawn p, ref bool __result)
    {
        if (__result)
            return;

        if (__instance != p.Ideo)
            return;

        BuildableDef def = thing.def.entityDefToBuild ?? thing.def;

        bool hasGeneDesignator = p.GeneTracker_XylXenos?.unlockedBuildables?.Contains(def) ?? false;
        if (!hasGeneDesignator && GenConstruct.tmpIdeoMemberNames.Count == 0)
        {
            foreach (GeneDef gene in DefDatabase<GeneDef>.AllDefs)
            {
                if (gene.CompProps<GeneCompProperties_UnlockBuildables>()?.buildables.Contains(def) == true)
                    GenConstruct.tmpIdeoMemberNames.Add("XylCharactersWithGene".Translate(gene.LabelCap));
            }
        }

        __result = hasGeneDesignator;
    }
}
