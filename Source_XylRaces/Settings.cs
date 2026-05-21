using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public class Settings : ModSettings
{
    public enum ThreeStateMode
    {
        Always,
        Sometimes,
        Never,
    }

    public static Settings instance;

    public ThreeStateMode allowBackerBackstories = ThreeStateMode.Sometimes;
    public ThreeStateMode fixLactationBugs = ThreeStateMode.Always;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref allowBackerBackstories, nameof(allowBackerBackstories), ThreeStateMode.Sometimes);
        // ReSharper disable once RedundantArgumentDefaultValue
        Scribe_Values.Look(ref fixLactationBugs, nameof(fixLactationBugs), ThreeStateMode.Always);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();

        listing.Begin(inRect);

        ThreeStateSetting(listing, nameof(allowBackerBackstories));
        ThreeStateSetting(listing, nameof(fixLactationBugs));

        listing.End();
    }

    private void ThreeStateSetting(Listing_Standard listing, string fieldName)
    {
        var valueRef = AccessTools.FieldRefAccess<ThreeStateMode>(GetType(), fieldName);

        if (listing.ButtonTextLabeled(
                $"XylSettingDescription_{fieldName}".Translate(),
                $"XylSettingOption_{fieldName}_{valueRef(this).ToString()}".Translate(),
                tooltip: $"XylSettingTooltip_{fieldName}".Translate()))
        {
            Find.WindowStack.Add(new FloatMenu([
                new($"XylSettingOption_{fieldName}_{ThreeStateMode.Always}".Translate(),
                    () => { valueRef(this) = ThreeStateMode.Always; }),
                new($"XylSettingOption_{fieldName}_{ThreeStateMode.Sometimes}".Translate(),
                    () => { valueRef(this) = ThreeStateMode.Sometimes; }),
                new($"XylSettingOption_{fieldName}_{ThreeStateMode.Never}".Translate(),
                    () => { valueRef(this) = ThreeStateMode.Never; }),
            ]));
        }
    }

    public bool ShouldFixLactationBugsFor(Pawn pawn)
    {
        return fixLactationBugs switch
        {
            ThreeStateMode.Always => true,
            ThreeStateMode.Never => false,
            ThreeStateMode.Sometimes => pawn.HasActiveGeneOfType<Hyperlactation>(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public bool AllowBackerBackstoriesFor(XenotypeDef xenotypeDef)
    {
        return allowBackerBackstories switch
        {
            ThreeStateMode.Always => true,
            ThreeStateMode.Never => false,
            ThreeStateMode.Sometimes => xenotypeDef?.GetModExtension<XenotypeDefExtension>()?.allowSolidBackstories ?? true,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
