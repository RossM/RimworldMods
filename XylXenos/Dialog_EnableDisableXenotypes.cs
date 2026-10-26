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

    public Dialog_EnableDisableXenotypes(Settings settings)
    {
        this.settings = settings;
        xenotypes = DefDatabase<XenotypeDef>.AllDefs.OrderByDescending(xenotype => xenotype.displayPriority).ToList();
        optionalTitle = "XylEnableDisableXenotypes".Translate();
        doCloseButton = true;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        string warning = "";
        List<FactionDef> disabledFactions =
        [
            .. settings.DisabledFactions().Where(f => f is { hidden: false })
                .OrderByDescending(f => f.listOrderPriority)
        ];
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
            bool enabled = alwaysEnabled || !settings.disabledXenotypes.Contains(xenotype.defName);
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
                settings.disabledXenotypes.Remove(xenotype.defName);
            else
                settings.disabledXenotypes.Add(xenotype.defName);
        }

        listing.End();
        Widgets.EndScrollView();

        Widgets.Label(warningRect, warning.Colorize(Color.red));
    }

    public override void PostClose()
    {
        base.PostClose();
        settings.Write();
    }
}
