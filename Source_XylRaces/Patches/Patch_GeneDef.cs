namespace XylXenos.Patches;

// GeneDef.GetDescriptionFull doesn't check whether thought stages are null, resulting in a NullReferenceException
// when processing XylHyperlactation and XylSoreBreasts.
[HarmonyPatch(typeof(GeneDef))]
public static class Patch_GeneDef_GetDescriptionFull
{
    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [Prefix]
    [Targets("GetDescriptionFull.*", typeof(ThoughtStage))]
    [Inline]
    public static bool GetDescriptionFull_Lambda_Prefix([Parameter(0)] ThoughtStage? stage, [ReturnValue] out bool __result)
    {
        __result = false;
        return stage is not null;
    }
}
