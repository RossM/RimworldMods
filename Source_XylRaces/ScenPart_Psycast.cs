using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class ScenPart_Psycast : ScenPart_PawnModifier
{
    private static IEnumerable<AbilityDef> PossiblePsycasts => field ??=
        GetPossiblePsycasts();

    public required AbilityDef psycast;

    private static List<AbilityDef> GetPossiblePsycasts()
    {
        return DefDatabase<AbilityDef>.AllDefsListForReading.Where(abilityDef =>
                abilityDef.verbProperties?.verbClass == typeof(Verb_CastPsycast))
            .OrderBy(abilityDef => abilityDef.level)
            .ThenBy(AbilityDef => AbilityDef.label).ToList();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref psycast, nameof(psycast));
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 4f);

        if (Widgets.ButtonText(scenPartRect.TopPartPixels(RowHeight), GetLabel(psycast)))
            FloatMenuUtility.MakeMenu(PossiblePsycasts, GetLabel, abilityDef => delegate { psycast = abilityDef; });

        DoPawnModifierEditInterface(scenPartRect.BottomPartPixels(RowHeight * 2f));
    }

    private static string GetLabel(AbilityDef abilityDef)
    {
        return "XylScenPartPsycastLabel".Translate(abilityDef.label.CapitalizeFirst(), abilityDef.level);
    }

    public override void Randomize()
    {
        psycast = PossiblePsycasts.RandomElement();
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override bool HasNullDefs()
    {
        if (base.HasNullDefs())
            return true;
        return psycast == null;
    }

    protected override void ModifyNewPawn(Pawn pawn)
    {
        LearnPsycast(pawn);
    }

    protected override void ModifyHideOffMapStartingPawnPostMapGenerate(Pawn pawn)
    {
        LearnPsycast(pawn);
    }

    private void LearnPsycast(Pawn pawn)
    {
        if (CanLearnPsycast(pawn, psycast))
        {
            DebugAssert.NotNull(pawn.abilities);

            pawn.abilities.GainAbility(psycast);
        }
    }

    private static bool CanLearnPsycast(Pawn pawn, AbilityDef abilityDef)
    {
        return pawn.abilities != null && pawn.GetPsylinkLevel() >= abilityDef.level && pawn.abilities.GetAbility(abilityDef) == null;
    }
}
