namespace XylXenos;

[UsedFromXml]
public class ScenPart_PsylinkLevels : ScenPart_PawnModifier
{
    public int count = 1;
    public bool givePsycasts = true;

    private string countBuf;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref count, nameof(count));
        Scribe_Values.Look(ref givePsycasts, nameof(givePsycasts));
    }

    protected override void ModifyNewPawn(Pawn pawn)
    {
        GivePsylink(pawn);
    }

    protected override void ModifyHideOffMapStartingPawnPostMapGenerate(Pawn pawn)
    {
        GivePsylink(pawn);
    }

    private void GivePsylink(Pawn pawn)
    {
        for (var i = 0; i < count; ++i)
        {
            if (givePsycasts)
                pawn.ChangePsylinkLevel(1, false);
            else
                ChangePsylinkLevelWithoutAbility(pawn, 1, false);
        }
    }

    public static void ChangePsylinkLevelWithoutAbility(Pawn pawn, int levelOffset, bool sendLetter = true)
    {
        Hediff_Psylink mainPsylinkSource = pawn.GetMainPsylinkSource();
        if (mainPsylinkSource == null)
        {
            mainPsylinkSource = (Hediff_Psylink)HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn);
            try
            {
                var oldAbilities = new HashSet<Ability>(pawn.abilities.AllAbilitiesForReading);
                mainPsylinkSource.suppressPostAddLetter = !sendLetter;
                pawn.health.AddHediff(mainPsylinkSource, pawn.health.hediffSet.GetBrain());
                foreach (var newAbility in pawn.abilities.AllAbilitiesForReading.Where(a => !oldAbilities.Contains(a)))
                    pawn.abilities.RemoveAbility(newAbility.def);
                levelOffset -= 1;
            }
            finally
            {
                mainPsylinkSource.suppressPostAddLetter = false;
            }
        }

        if (levelOffset > 0)
        {
            float num = Math.Min(levelOffset, mainPsylinkSource.def.maxSeverity - mainPsylinkSource.level);
            for (var i = 0; i < num; i++)
            {
                pawn.psychicEntropy?.Notify_GainedPsylink();
            }
        }

        mainPsylinkSource.level = (int)Mathf.Clamp(mainPsylinkSource.level + levelOffset, mainPsylinkSource.def.minSeverity,
            mainPsylinkSource.def.maxSeverity);
    }

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 4f);
        Widgets.TextFieldNumeric(scenPartRect.TopPartPixels(RowHeight), ref count, ref countBuf, 1, 10);
        Widgets.CheckboxLabeled(new(scenPartRect.x, scenPartRect.y + RowHeight, scenPartRect.width, RowHeight), "Give psycasts",
            ref givePsycasts);
        DoPawnModifierEditInterface(scenPartRect.BottomPartPixels(RowHeight * 2f));
    }
}
