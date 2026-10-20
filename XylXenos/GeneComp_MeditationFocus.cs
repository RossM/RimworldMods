namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_MeditationFocus : GeneCompProperties
{
    public List<MeditationFocusDef>? allowedMeditationFocusTypes;

    public override IEnumerable<string> CustomEffectDescriptions()
    {
        foreach (var type in allowedMeditationFocusTypes ?? Enumerable.Empty<MeditationFocusDef>())
        {
            yield return $"{"MeditationFocusesUnlocked".Translate()}: {type.LabelCap}";
        }
    }

    public GeneCompProperties_MeditationFocus()
    {
        compClass = typeof(GeneComp_MeditationFocus);
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
