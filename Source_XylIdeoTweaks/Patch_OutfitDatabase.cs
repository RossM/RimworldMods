namespace XylIdeos;

[HarmonyPatch(typeof(OutfitDatabase))]
public static class Patch_OutfitDatabase
{
    [Feature(Features.BetterDefaultOutfits)]
    [HarmonyPostfix]
    [HarmonyPatch("GenerateStartingOutfits")]
    public static void GenerateStartingOutfits_Postfix(OutfitDatabase __instance)
    {
        var outfitNudist = __instance.AllOutfits.First(outfit => outfit.label == "OutfitNudist".Translate());

        outfitNudist.filter.SetDisallowAll();
        outfitNudist.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, allow: false);
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            if (def.apparel != null && (def.apparel.defaultOutfitTags.NotNullAndContains("Nudist") ||
                                        !def.apparel.countsAsClothingForNudity))
            {
                outfitNudist.filter.SetAllow(def, allow: true);
            }
        }

        if (ModsConfig.IdeologyActive)
        {
            var outfitSlavePermissive = __instance.MakeNewOutfit();
            outfitSlavePermissive.label = "Slave (permissive)";
            outfitSlavePermissive.filter.SetDisallowAll();
            outfitSlavePermissive.filter.SetAllow(SpecialThingFilterDefOf.AllowDeadmansApparel, allow: false);
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.apparel == null)
                    continue;

                if (def.apparel.defaultOutfitTags.NotNullAndContains("Slave"))
                {
                    outfitSlavePermissive.filter.SetAllow(def, allow: true);
                    continue;
                }

                if (def.equippedStatOffsets != null &&
                    def.equippedStatOffsets.Any(modifier => modifier.stat == Defs.SlaveSuppressionOffset && modifier.value < 0))
                    continue;
                if (def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                    def.apparel.layers.Contains(ApparelLayerDefOf.Shell))
                    continue;
                if (def.apparel.bodyPartGroups.Contains(Defs.Neck) && def.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
                    continue;

                outfitSlavePermissive.filter.SetAllow(def, allow: true);
            }
        }
    }

    [DefOf]
    public static class Defs
    {
        [UsedImplicitly] public static BodyPartGroupDef Neck;
        [UsedImplicitly] public static StatDef SlaveSuppressionOffset;
    }
}
