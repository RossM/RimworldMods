namespace XylXenos;

public class Settings : ModSettings
{
    public enum ThreeStateMode
    {
        Always,
        Sometimes,
        Never,
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static Settings instance;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public ThreeStateMode allowBackerBackstories = ThreeStateMode.Sometimes;
    public ThreeStateMode fixLactationBugs = ThreeStateMode.Always;
    public bool fixGeneticPassions = true;

    public bool useDistinctiveFactionColors;

    public override void ExposeData()
    {
        // ReSharper disable RedundantArgumentDefaultValue
        Scribe_Values.Look(ref allowBackerBackstories, nameof(allowBackerBackstories), ThreeStateMode.Sometimes);
        Scribe_Values.Look(ref fixLactationBugs, nameof(fixLactationBugs), ThreeStateMode.Always);
        Scribe_Values.Look(ref useDistinctiveFactionColors, nameof(useDistinctiveFactionColors), true);
        Scribe_Values.Look(ref fixGeneticPassions, nameof(fixGeneticPassions), defaultValue: true);
        // ReSharper restore RedundantArgumentDefaultValue
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new();

        listing.Begin(inRect);

        EnumSetting<ThreeStateMode>(listing, nameof(allowBackerBackstories));
        EnumSetting<ThreeStateMode>(listing, nameof(fixLactationBugs));
        BoolSetting(listing, nameof(useDistinctiveFactionColors));
        BoolSetting(listing, nameof(fixGeneticPassions));

        listing.End();
    }

    private void BoolSetting(Listing_Standard listing, string fieldName)
    {
        var valueRef = AccessTools.FieldRefAccess<bool>(GetType(), fieldName);

        listing.CheckboxLabeled($"XylSettingDescription_{fieldName}".Translate(), ref valueRef(this),
            $"XylSettingTooltip_{fieldName}".Translate());
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
            ThreeStateMode.Sometimes => pawn.FirstActiveGeneWithComp<GeneComp_Hyperlactation>() != null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public bool AllowBackerBackstoriesFor(XenotypeDef? xenotypeDef)
    {
        return allowBackerBackstories switch
        {
            ThreeStateMode.Always => true,
            ThreeStateMode.Never => false,
            ThreeStateMode.Sometimes => xenotypeDef?.GetModExtension<DefModExtension_Xenotype>()?.allowSolidBackstories ?? true,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
