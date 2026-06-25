namespace Xylib;

[UsedFromXml]
public class GeneCompProperties_PermanentHediffs : GeneCompProperties
{
    public List<HediffGiver_Event> hediffs;

    public GeneCompProperties_PermanentHediffs()
    {
        compClass = typeof(GeneComp_PermanentHediffs);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (Tool tool in hediffs.Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
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

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        foreach (Tool tool in hediffs.Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
                     .Where(verbGiver => verbGiver != null).SelectMany(verbGiver => verbGiver.tools))
        {
            float armorPenetration = tool.armorPenetration;
            if (armorPenetration < 0f)
            {
                armorPenetration = tool.power * 0.015f;
            }

            // TODO: Calculate DPS
            yield return $"{"StatsReport_MeleeDamage".Translate()}: {tool.power.ToStringByStyle(ToStringStyle.FloatTwo)}";
            yield return $"{"ArmorPenetration".Translate()}: {armorPenetration.ToStringPercent()}";
            yield return
                $"{"StatsReport_Cooldown".Translate()}: {"StatsReport_CooldownFormat".Translate(tool.cooldownTime.ToStringDecimalIfSmall())}";
        }
    }
}

public class GeneComp_PermanentHediffs : GeneComp, IEventListener
{
    public GeneCompProperties_PermanentHediffs Props => (GeneCompProperties_PermanentHediffs)props;

    public override void CompPostPostAdd()
    {
        if (!Active)
            return;

        foreach (var hediffGiver in Props.hediffs)
            hediffGiver.EventOccurred(Pawn);
    }

    public override void CompPostPostRemove()
    {
        foreach (var hediff in GetLinkedHediffs())
            Pawn.health.RemoveHediff(hediff);
    }

    public override void CompReset()
    {
        foreach (var hediff in GetLinkedHediffs())
            hediff.Severity = hediff.def.initialSeverity;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        foreach (Tool tool in Props.hediffs
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

    private void UpdatePermanentHediffs()
    {
        foreach (var hediffGiver in Props.hediffs)
        {
            if (hediffGiver.partsToAffect.NullOrEmpty())
                continue;

            List<BodyPartRecord> partsToAdd = [];
            List<BodyPartRecord> partsToRemove = [];
            HediffDef hediffDef = hediffGiver.hediff;
            int partCount = 0;

            foreach (BodyPartRecord part in Pawn.def.race.body.AllParts)
            {
                if (!hediffGiver.partsToAffect.Contains(part.def))
                    continue;

                bool alreadyHasHediff = false;
                bool missingPart = false;
                foreach (Hediff hediff in Pawn.health.hediffSet.hediffs)
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
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, Pawn, part);
                Pawn.health.AddHediff(hediff);
            }

            foreach (BodyPartRecord part in partsToRemove)
            {
                Hediff hediff = Pawn.health.hediffSet.hediffs.First(h => h.def == hediffDef && h.Part == part);
                Pawn.health.RemoveHediff(hediff);
            }
        }
    }


    private IEnumerable<Hediff> GetLinkedHediffs()
    {
        HashSet<HediffDef> defs = [.. Props.hediffs.Select(hediffGiver => hediffGiver.hediff)];
        return Pawn.health.hediffSet.hediffs.Where(hediff => defs.Contains(hediff.def)).ToList();
    }


    public void Notify_HediffStateChange()
    {
        if (!Active)
            return;

        UpdatePermanentHediffs();
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostCheckForStateChange, Pawn, Notify_HediffStateChange);
    }

    public void PreUnregister(EventManager manager)
    {
    }
}
