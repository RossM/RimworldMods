namespace Xylib;

[UsedFromXml]
public class HediffComp_DebugHediffGivers : HediffComp
{
    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
            yield break;

        var stage = parent.CurStage;
        if (stage == null || stage.hediffGivers.NullOrEmpty())
            yield break;

        for (var index = 0; index < stage.hediffGivers.Count; index++)
        {
            HediffGiver giver = stage.hediffGivers[index];
            if (giver == null)
                continue;
            yield return new Command_Action
            {
                defaultLabel = parent.Part != null
                    ? $"DEV: Trigger {parent.LabelBase} ({parent.Part?.Label}) #{index}"
                    : $"DEV: Trigger {parent.LabelBase} #{index}",
                action = () => Trigger(giver),
                groupable = false,
            };
        }
    }

    private void Trigger([NotNull] HediffGiver giver)
    {
        if (giver is HediffGiver_RandomExt giverExt)
            giverExt.TryApply(parent.pawn!, parent);
        else
            giver.TryApply(parent.pawn!);
    }
}
