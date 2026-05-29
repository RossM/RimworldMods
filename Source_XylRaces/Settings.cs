using System;
using System.Collections.Generic;
using System.Linq;
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

    public bool useDistinctiveFactionColors;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref allowBackerBackstories, nameof(allowBackerBackstories), ThreeStateMode.Sometimes);
        // ReSharper disable once RedundantArgumentDefaultValue
        Scribe_Values.Look(ref fixLactationBugs, nameof(fixLactationBugs), ThreeStateMode.Always);
        Scribe_Values.Look(ref useDistinctiveFactionColors, nameof(useDistinctiveFactionColors), true);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();

        listing.Begin(inRect);

        EnumSetting<ThreeStateMode>(listing, nameof(allowBackerBackstories));
        EnumSetting<ThreeStateMode>(listing, nameof(fixLactationBugs));

        listing.CheckboxLabeled("Use distinctive faction colors", ref useDistinctiveFactionColors);

        listing.End();
    }

    private void EnumSetting<T>(Listing_Standard listing, string fieldName)
    {
        var valueRef = AccessTools.FieldRefAccess<T>(GetType(), fieldName);

        var enumType = typeof(T);

        if (listing.ButtonTextLabeled(
                $"XylSettingDescription_{fieldName}".Translate(),
                $"XylSettingOption_{fieldName}_{valueRef(this).ToString()}".Translate(),
                tooltip: $"XylSettingTooltip_{fieldName}".Translate()))
        {
            var names = Enum.GetNames(enumType);
            var values = (T[])Enum.GetValues(enumType);

            List<FloatMenuOption> options = [];
            for (int i = 0; i < names.Length; i++)
            {
                var curName = names[i];
                var curValue = values[i];

                options.Add(new($"XylSettingOption_{fieldName}_{curName}".Translate(),
                    () => { valueRef(this) = curValue; }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
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
