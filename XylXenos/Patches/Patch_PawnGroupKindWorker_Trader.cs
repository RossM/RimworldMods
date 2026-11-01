using RimWorld.Planet;

namespace XylXenos.Patches;

[Patch(typeof(PawnGroupKindWorker_Trader))]
public static class Patch_PawnGroupKindWorker_Trader
{
    [Feature(nameof(MyDefOf.XylSelkie))]
    [Postfix]
    [Target("<>c__DisplayClass1_0.<CanGenerateFrom>b__0", typeof(PawnGenOption))]
    [Target("<>c__DisplayClass4_0.<GenerateCarriers>b__1", typeof(PawnGenOption))]
    public static void IsPackAnimalAllowedCheck_Postfix([Argument(0)] PawnGenOption option, [Field] PawnGroupMakerParms parms, ref bool __result)
    {
        if (__result && parms.tile.Valid && option.kind == MyDefOf.XylSelkie && !Find.WorldGrid[parms.tile].IsCoastalOrRiverTile)
            __result = false;
    }
}
