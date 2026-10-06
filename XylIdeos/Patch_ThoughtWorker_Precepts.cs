namespace XylIdeos;

[HarmonyPatch]
public static class Patch_ThoughtWorker_Precepts
{
    private static readonly IReadOnlyCollection<BodyPartGroupDef> GroinBodyParts = [BodyPartGroupDefOf.Legs];

    private static readonly IReadOnlyCollection<BodyPartGroupDef> HairOrFaceBodyParts =
        [BodyPartGroupDefOf.UpperHead, BodyPartGroupDefOf.FullHead];

    [Feature(Features.ApparelRequirementsOverrideNudity)]
    [Prefix]
    [Target(typeof(ThoughtWorker_Precept_AnyBodyPartButGroinCovered),
        nameof(ThoughtWorker_Precept_AnyBodyPartButGroinCovered.HasCoveredBodyPartsButGroin))]
    public static bool HasCoveredBodyPartsButGroin_Prefix(Pawn p, out bool __result)
    {
        __result = PatchHelpers.HasUnnecessaryApparel(p, GroinBodyParts);
        return false;
    }

    [Feature(Features.ApparelRequirementsOverrideNudity)]
    [Prefix]
    [Target(typeof(ThoughtWorker_Precept_AnyBodyPartButHairOrFaceCovered),
        nameof(ThoughtWorker_Precept_AnyBodyPartButHairOrFaceCovered.HasCoveredBodyPartsButHairOrFace))]
    public static bool HasCoveredBodyPartsButHairOrFace_Prefix(Pawn p, out bool __result)
    {
        __result = PatchHelpers.HasUnnecessaryApparel(p, HairOrFaceBodyParts);
        return false;
    }

    [Feature(Features.ApparelRequirementsOverrideNudity)]
    [Prefix]
    [Target(typeof(ThoughtWorker_Precept_AnyBodyPartCovered),
        nameof(ThoughtWorker_Precept_AnyBodyPartCovered.HasUnnecessarilyCoveredBodyParts))]
    public static bool HasUnnecessarilyCoveredBodyParts_Prefix(Pawn p, out bool __result)
    {
        __result = PatchHelpers.HasUnnecessaryApparel(p);
        return false;
    }
}
