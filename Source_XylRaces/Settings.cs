using UnityEngine;
using Verse;

namespace XylXenos;

public class Settings : ModSettings
{
    public static Settings instance;

    public bool allowBackerBackstoriesForAllXenotypes = false;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref allowBackerBackstoriesForAllXenotypes, nameof(allowBackerBackstoriesForAllXenotypes));
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();

        listing.Begin(inRect);
        listing.CheckboxLabeled(
            "XylSettingDescription_allowBackerBackstoriesForAllXenotypes".Translate(),
            ref instance.allowBackerBackstoriesForAllXenotypes,
            "XylSettingTooltip_allowBackerBackstoriesForAllXenotypes".Translate());
        listing.End();
    }
}
