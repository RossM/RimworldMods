namespace XylXenos;

public class GeneExt : Gene, INotificationListener
{
    [NotNull]
    public DefModExtension_Gene DefExt => def.DefExt!;

    public GeneType GeneType => geneTypeInternal ??= pawn.genes.Xenogenes.Contains(this) ? GeneType.Xenogene : GeneType.Endogene;

    public bool Removed => removed;

    [Unsaved] private GeneType? geneTypeInternal;
    [Unsaved] private bool removed = false;

    public override bool Active
    {
        get
        {
            if (!base.Active)
                return false;
            if (removed)
                return false;
            if (DefExt.gender != null && DefExt.gender != pawn.gender)
                return false;
            if (DefExt.geneType != null && DefExt.geneType != GeneType)
                return false;
            return true;
        }
    }

    public virtual IEnumerable<ThingDefCount> GetStartingItems()
    {
        if (DefExt.startingItems.NullOrEmpty())
            yield break;

        foreach (var startingItem in DefExt.startingItems)
        {
            if (!Rand.Chance(startingItem.chance))
                continue;

            var itemDef = startingItem.item ?? DefDatabase<ThingDef>.AllDefsListForReading
                .Where(thingDef => Validate(thingDef, startingItem)).RandomElement();
            if (itemDef == null)
                continue;

            var itemNutrition = itemDef.GetStatBase(StatDefOf.Nutrition);
            int count;
            if (startingItem.nutritionAmount != FloatRange.Zero && itemNutrition > 0)
                count = Mathf.FloorToInt(startingItem.nutritionAmount.RandomInRange / itemNutrition);
            else if (startingItem.count != IntRange.Zero)
                count = startingItem.count.RandomInRange;
            else if (itemDef.possessionCount > 0)
                count = itemDef.possessionCount;
            else
                count = 1;

            yield return new(itemDef, Mathf.Clamp(count, 1, itemDef.stackLimit));
        }

        bool Validate(ThingDef thingDef, StartingItemOption startingItem)
        {
            return thingDef.ingestible?.foodType.HasFlag(startingItem.foodType) == true;
        }
    }

    public void RemoveInvalidChemicalHediffs()
    {
        HashSet<HediffDef> hediffDefsToRemove = [];

        foreach (var chemicalDef in DefDatabase<ChemicalDef>.AllDefs)
        {
            if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
            {
                if (chemicalDef.toleranceHediff != null)
                    hediffDefsToRemove.Add(chemicalDef.toleranceHediff);
                if (chemicalDef.addictionHediff != null)
                    hediffDefsToRemove.Add(chemicalDef.addictionHediff);
            }
        }

        var hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
        foreach (var hediff in hediffs)
        {
            if (hediffDefsToRemove.Contains(hediff.def))
                pawn.health.RemoveHediff(hediff);
        }
    }

    public override void PostAdd()
    {
        if (Active && !DefExt.permanentHediffs.NullOrEmpty())
        {
            foreach (var hediffGiver in DefExt.permanentHediffs)
                hediffGiver.EventOccurred(pawn);
        }

        RemoveInvalidChemicalHediffs();

        if (DefExt.hasPsycast)
            pawn.psychicEntropy.SetInitialPsyfocusLevel();

        base.PostAdd();
    }

    public override void PostRemove()
    {
        removed = true;
        NotificationManager.Instance.UnregisterAll(this);

        if (!DefExt.permanentHediffs.NullOrEmpty())
        {
            foreach (var hediff in GetLinkedHediffs())
                pawn.health.RemoveHediff(hediff);
        }

        RemoveInvalidChemicalHediffs();

        base.PostRemove();
    }

    public override void Reset()
    {
        base.Reset();

        if (!DefExt.permanentHediffs.NullOrEmpty())
        {
            foreach (var hediff in GetLinkedHediffs())
                hediff.Severity = hediff.def.initialSeverity;
        }
    }

    private IEnumerable<Hediff> GetLinkedHediffs()
    {
        if (DefExt.permanentHediffs.NullOrEmpty())
            return [];

        HashSet<HediffDef> defs = [.. DefExt.permanentHediffs.Select(hediffGiver => hediffGiver.hediff)];
        return pawn.health.hediffSet.hediffs.Where(hediff => defs.Contains(hediff.def)).ToList();
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (!DefExt.permanentHediffs.NullOrEmpty())
        {
            foreach (Tool tool in DefExt.permanentHediffs
                         .Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
                         .Where(verbGiver => verbGiver != null).SelectMany(verbGiver => verbGiver.tools))
            {
                float armorPenetration = tool.armorPenetration;
                if (armorPenetration < 0f)
                {
                    armorPenetration = tool.power * 0.015f;
                }

                // TODO: Calculate DPS
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "StatsReport_MeleeDamage".Translate(),
                    tool.power.ToStringByStyle(ToStringStyle.FloatTwo), "", 4102);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "ArmorPenetration".Translate(),
                    armorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 4101);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "StatsReport_Cooldown".Translate(),
                    "StatsReport_CooldownFormat".Translate(tool.cooldownTime.ToStringDecimalIfSmall()), "", 4100);
            }
        }

        if (DefExt.femaleChance != null)
        {
            yield return new(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
                DefExt.GenderRatioDescription, "XylGenderRatioDesc".TranslateSimple(), 1);
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (!Active)
            return;
        if (DefExt.hediffGivers.NullOrEmpty())
            return;
        if (!pawn.IsHashIntervalTick(60, delta))
            return;

        foreach (var hediffGiver in DefExt.hediffGivers)
        {
            hediffGiver.OnIntervalPassed(pawn, null);
        }
    }

    public void Notify_HediffStateChange()
    {
        if (!Active)
            return;
        if (DefExt.permanentHediffs.NullOrEmpty())
            return;

        foreach (var hediffGiver in DefExt.permanentHediffs)
        {
            if (hediffGiver.partsToAffect.NullOrEmpty())
                continue;

            List<BodyPartRecord> partsToAdd = [];
            List<BodyPartRecord> partsToRemove = [];
            HediffDef hediffDef = hediffGiver.hediff;
            int partCount = 0;

            foreach (BodyPartRecord part in pawn.def.race.body.AllParts)
            {
                if (!hediffGiver.partsToAffect.Contains(part.def))
                    continue;

                bool alreadyHasHediff = false;
                bool missingPart = false;
                foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff.Part != part)
                        continue;

                    if (hediff.def == hediffDef)
                        alreadyHasHediff = true;
                    else if (typeof(Hediff_AddedPart).IsAssignableFrom(hediff.def.hediffClass))
                        missingPart = true;
                    else if (typeof(Hediff_MissingPart).IsAssignableFrom(hediff.def.hediffClass))
                        missingPart = true;
                }

                if (alreadyHasHediff)
                    partCount++;

                if (missingPart && alreadyHasHediff)
                    partsToRemove.Add(part);
                else if (!missingPart && !alreadyHasHediff)
                    partsToAdd.Add(part);
            }

            int maxToAdd = hediffGiver.countToAffect - partCount;
            partsToAdd.Shuffle();

            foreach (BodyPartRecord part in partsToAdd.Take(maxToAdd))
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, part);
                pawn.health.AddHediff(hediff);
            }

            foreach (BodyPartRecord part in partsToRemove)
            {
                Hediff hediff = pawn.health.hediffSet.hediffs.First(h => h.def == hediffDef && h.Part == part);
                pawn.health.RemoveHediff(hediff);
            }
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (!DebugSettings.godMode)
            yield break;
        if (DefExt.hediffGivers == null)
            yield break;

        for (var index = 0; index < DefExt.hediffGivers.Count; index++)
        {
            HediffGiver hediffGiver = DefExt.hediffGivers[index];
            yield return new Command_Action
            {
                defaultLabel = $"DEV: Trigger {Label} ({hediffGiver.hediff.label}) #{index}",
                action = () => hediffGiver.TryApply(pawn),
                groupable = false,
            };
        }
    }

    public void GenerateExtraApparel()
    {
        foreach (var item in DefExt.extraApparel!)
        {
            if (!Rand.Chance(item.chance))
                continue;

            var apparel = PawnApparelGenerator.GenerateApparelOfDefFor(pawn, item.item);
            if (apparel != null && apparel.PawnCanWear(pawn))
            {
                PawnApparelGenerator.PostProcessApparel(apparel, pawn);
                PawnGenerator.PostProcessGeneratedGear(apparel, pawn);
                pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            }
        }
    }

    public void Notify_PostGenerateNewPawn(PawnGenerationRequest request)
    {
        if (!request.ForceNoGear && !request.AllowedDevelopmentalStages.Newborn())
            GenerateExtraApparel();
    }

    public void Notify_PostRedressPawn(PawnGenerationRequest request)
    {
        if (!request.ForceNoGear && !request.AllowedDevelopmentalStages.Newborn())
            GenerateExtraApparel();
    }

    public virtual void RegisterWith(NotificationManager manager)
    {
        if (!DefExt.permanentHediffs.NullOrEmpty())
            manager.Register(NotificationDefOf.PostCheckForStateChange, pawn, Notify_HediffStateChange);

        if (!DefExt.extraApparel.NullOrEmpty())
        {
            manager.Register<PawnGenerationRequest>(NotificationDefOf.PostGenerateNewPawn, pawn, Notify_PostGenerateNewPawn);
            manager.Register<PawnGenerationRequest>(NotificationDefOf.PostRedressPawn, pawn, Notify_PostRedressPawn);
        }
    }

    public void PreUnregister(NotificationManager manager)
    {
    }
}
