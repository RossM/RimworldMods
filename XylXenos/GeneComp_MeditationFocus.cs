namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_MeditationFocus : GeneCompProperties
{
    public List<MeditationFocusDef>? allowedMeditationFocusTypes;

    public GeneCompProperties_MeditationFocus()
    {
        compClass = typeof(GeneComp_MeditationFocus);
    }

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        return allowedMeditationFocusTypes?.Select(type => $"{"MeditationFocusesUnlocked".Translate()}: {type.LabelCap}") ?? [];
    }
}

public class GeneComp_MeditationFocus : GeneComp
{
    public GeneCompProperties_MeditationFocus Props => (GeneCompProperties_MeditationFocus)props;

    public override void CompPostPostAdd()
    {
        MeditationFocusTypeAvailabilityCache.ClearFor(Pawn);
    }

    public override void CompPostPostRemove()
    {
        MeditationFocusTypeAvailabilityCache.ClearFor(Pawn);
    }

    public bool EnablesMeditationType(MeditationFocusDef type) =>
        Props.allowedMeditationFocusTypes is { Count: >= 1 } types && types.Contains(type);
}
