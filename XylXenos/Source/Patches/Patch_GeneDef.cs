namespace XylXenos.Patches;

// GeneDef.GetDescriptionFull doesn't check whether thought stages are null, resulting in a NullReferenceException
// when processing XylHyperlactation and XylFullMilk.
[Patch(typeof(GeneDef))]
public static class Patch_GeneDef_GetDescriptionFull
{
    [Feature(FeatureNameOf.Bugfix_Misc)]
    [Prefix]
    [Targets("GetDescriptionFull.*", typeof(ThoughtStage))]
    [PatchOptions(PatchOptions.Inline)]
    public static bool GetDescriptionFull_Lambda_Prefix([Argument(0)] ThoughtStage? stage, [ReturnValue] out bool __result)
    {
        __result = false;
        return stage is not null;
    }

    [Feature(FeatureNameOf.UI_Misc)]
    [Postfix]
    [Inner(typeof(StatDef), nameof(StatDef.CanShowWithLoadedMods))]
    [Target(nameof(GeneDef.SpecialDisplayStats))]
    [Target("GetDescriptionFull")]
    public static void CanShowWithLoadedMods_Postfix(GeneDef __caller, StatDef __instance, ref bool __result)
    {
        // Paranoia for modded genes
        if (__caller.GetType() != typeof(GeneDef))
            return;

        if (!__instance.showOnNonWildManHumanlikes)
            __result = false;
    }
}
