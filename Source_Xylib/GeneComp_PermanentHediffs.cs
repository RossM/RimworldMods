namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class GeneCompProperties_PermanentHediffs : GeneCompProperties
{
    public List<HediffGiver_Event>? hediffs;

    public GeneCompProperties_PermanentHediffs()
    {
        compClass = typeof(GeneComp_PermanentHediffs);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        GetVerbsAndTools(out var verbs, out var tools);
        Pawn? pawn = req.Pawn;

        // Melee DPS & armor penetration
        {
            StringBuilder sbDps = new();
            StringBuilder sbArmorPenetration = new();

            float totalMeleeDamage = 0f;
            float totalCooldown = 0f;
            float totalArmorPenetration = 0f;
            float totalWeight = 0f;

            foreach (var verb in VerbUtility.GetAllVerbProperties(verbs, tools).Where(v => v.verbProps?.IsMeleeAttack is true))
            {
                float meleeDamage = verb.verbProps.AdjustedMeleeDamageAmount(verb.tool, pawn, null, null);
                float cooldown = verb.verbProps.AdjustedCooldown(verb.tool, pawn, null, null);
                float armorPenetration = verb.verbProps.AdjustedArmorPenetration(verb.tool, pawn, null, null);
                float weight = verb.verbProps.AdjustedMeleeSelectionWeight(verb.tool, pawn, null, null, false);

                totalMeleeDamage += meleeDamage * weight;
                totalCooldown += cooldown * weight;
                totalArmorPenetration += armorPenetration * weight;
                totalWeight += weight;

                if (verb.tool != null)
                {
                    sbDps.AppendLine($"  {verb.tool.LabelCap} ({verb.ToolCapacity?.label})");
                    sbArmorPenetration.AppendLine($"  {verb.tool.LabelCap} ({verb.ToolCapacity?.label})");
                }
                else
                {
                    sbDps.AppendLine($"  {"StatsReport_NonToolAttack".Translate()}:");
                    sbArmorPenetration.AppendLine($"  {"StatsReport_NonToolAttack".Translate()}:");
                }

                sbDps.AppendLine($"    {meleeDamage:F1} {"DamageLower".Translate()}");
                sbDps.AppendLine($"    {cooldown:F2} {"SecondsPerAttackLower".Translate()}");

                sbArmorPenetration.AppendLine($"    {armorPenetration.ToStringPercent()}");
            }

            if (totalWeight > 0f)
            {
                float dps = totalCooldown > 0f ? totalMeleeDamage / totalCooldown : 0f;
                float armorPenetration = totalArmorPenetration / totalWeight;

                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee!, StatDefOf.MeleeWeapon_AverageDPS!,
                    dps, req).SetReportText(sbDps.ToString());
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee!, XStatDefOf.MeleeWeapon_AverageArmorPenetration,
                    armorPenetration, req).SetReportText(sbArmorPenetration.ToString());
            }
        }
    }

    private void GetVerbsAndTools(out List<VerbProperties> verbs, out List<Tool> tools)
    {
        verbs = [];
        tools = [];

        foreach (var hediff in hediffs!)
        {
            var props = hediff.hediff!.CompProps<HediffCompProperties_VerbGiver>();
            if (props?.verbs != null)
                verbs.AddRange(props.verbs);
            if (props?.tools != null)
                tools.AddRange(props.tools);
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        if (hediffs is null)
            yield return $"{nameof(hediffs)} is null";
    }
}

[PublicAPI]
public class GeneComp_PermanentHediffs : GeneComp, IEventListener
{
    public GeneCompProperties_PermanentHediffs Props => (GeneCompProperties_PermanentHediffs)props;

    public override void CompPostPostAdd()
    {
        if (!Active)
            return;

        foreach (var hediffGiver in Props.hediffs!)
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

    private void UpdatePermanentHediffs()
    {
        foreach (var hediffGiver in Props.hediffs!)
        {
            if (hediffGiver.partsToAffect is not { Count: > 0 })
                continue;

            List<BodyPartRecord> partsToAdd = [];
            List<BodyPartRecord> partsToRemove = [];
            HediffDef hediffDef = hediffGiver.hediff!;
            int partCount = 0;

            foreach (BodyPartRecord part in Pawn.def.race!.body!.AllParts!)
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
                    else if (typeof(Hediff_AddedPart).IsAssignableFrom(hediff.def.hediffClass!) ||
                             typeof(Hediff_MissingPart).IsAssignableFrom(hediff.def.hediffClass!))
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

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostCheckForStateChange, Pawn, Notify_HediffStateChange);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
