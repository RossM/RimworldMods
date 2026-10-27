namespace XylXenos;

public class Dialog_EnableDisableXenotypes : Window
{
    public override Vector2 InitialSize => new(600f, 700f);
    private const float RowHeight = 30f;
    private const float IconSize = 24f;
    private const float Gap = 12f;

    private readonly Settings settings;
    private readonly List<XenotypeDef> xenotypes;
    private Vector2 scrollPosition;
    private readonly HashSet<string> newDisabledXenotypes;


    public Dialog_EnableDisableXenotypes(Settings settings)
    {
        this.settings = settings;
        xenotypes = DefDatabase<XenotypeDef>.AllDefs.OrderByDescending(xenotype => xenotype.displayPriority).ToList();
        optionalTitle = "XylEnableDisableXenotypes".Translate();
        doCloseButton = true;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        newDisabledXenotypes = [.. settings.disabledXenotypes];
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        string warning = "";
        List<FactionDef> disabledFactions = [.. DisabledFactions().OrderByDescending(f => f.listOrderPriority)];
        if (disabledFactions.Count > 0)
            warning = "XylXenotypeSelectionFactionsDisabledWarning".Translate(disabledFactions.Select(f => f.label).ToCommaList());
        float warningHeight = Text.CalcHeight(warning, inRect.width);
        Rect warningRect = new(inRect.x, inRect.yMax - FooterRowHeight - warningHeight, inRect.width, warningHeight);
        Rect listRect = new(inRect.x, inRect.y, inRect.width, warningRect.yMin - inRect.y - Gap);
        Rect viewRect = new(0f, 0f, listRect.width - 16f, xenotypes.Count * RowHeight);

        Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
        Listing_Standard listing = new();
        listing.Begin(viewRect);
        foreach (XenotypeDef xenotype in xenotypes)
        {
            bool alwaysEnabled = xenotype == XenotypeDefOf.Baseliner || xenotype == XenotypeDefOf.Sanguophage;
            bool enabled = alwaysEnabled || !newDisabledXenotypes.Contains(xenotype.defName);
            bool wasEnabled = enabled;
            Rect rowRect = listing.GetRect(RowHeight);
            Rect entryRect = rowRect;
            entryRect.xMax -= Widgets.InfoCardButtonSize + 6f;
            Rect iconRect = new(rowRect.x, rowRect.y + (RowHeight - IconSize) / 2f, IconSize, IconSize);
            Widgets.DefIcon(iconRect, xenotype);
            Rect checkboxRect = entryRect;
            checkboxRect.xMin = iconRect.xMax + 6f;
            Widgets.CheckboxLabeled(checkboxRect, xenotype.LabelCap, ref enabled, disabled: alwaysEnabled);
            TooltipHandler.TipRegion(entryRect, xenotype.descriptionShort ?? xenotype.description);
            Widgets.InfoCardButton(rowRect.xMax - Widgets.InfoCardButtonSize,
                rowRect.y + (RowHeight - Widgets.InfoCardButtonSize) / 2f, xenotype);
            if (enabled == wasEnabled)
                continue;

            if (enabled)
                newDisabledXenotypes.Remove(xenotype.defName);
            else
                newDisabledXenotypes.Add(xenotype.defName);
        }

        listing.End();
        Widgets.EndScrollView();

        Widgets.Label(warningRect, warning.Colorize(Color.red));
    }

    public override void PostClose()
    {
        if (!newDisabledXenotypes.SetEquals(settings.disabledXenotypes))
            Find.WindowStack.Add(new Dialog_MessageBox(
                "XylXenotypeSelectionRestartWarning".Translate(),
                "Yes".Translate(),
                () =>
                {
                    settings.disabledXenotypes = newDisabledXenotypes;
                    settings.Write();
                    GenCommandLine.Restart();
                },
                "No".Translate(),
                () => { },
                buttonADestructive: true));

        base.PostClose();
    }

    private IEnumerable<FactionDef> DisabledFactions()
    {
        foreach (var faction in DefDatabase<FactionDef>.AllDefs)
        {
            if (faction.xenotypeSet == null || faction.hidden)
                continue;
            if ((faction.GetModExtension<DefModExtension_SettingsData>()?.requiredXenotype is { } xenotype && 
                 newDisabledXenotypes.Contains(xenotype.defName)) ||
                (faction.xenotypeSet.Count == 1 && faction.xenotypeSet[0].chance >= 1f &&
                 newDisabledXenotypes.Contains(faction.xenotypeSet[0].xenotype.defName)) ||
                (faction.xenotypeSet is XenotypeSetWithDefault { defaultXenotype: { } defaultXenotype } &&
                 newDisabledXenotypes.Contains(defaultXenotype.defName)))
                yield return faction;
        }
    }
}
