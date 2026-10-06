using RimWorld.QuestGen;

namespace XylXenos;

/// <summary>
///     Displays a joiner and cargo as one offer. Arrival nodes remain responsible for delivering them.
/// </summary>
[UsedFromXml]
public class QuestNode_AddPawnAndItemsReward : QuestNode
{
    public SlateRef<Pawn> pawn;
    public SlateRef<IEnumerable<Thing>> items;
    public SlateRef<bool> rewardDetailsHidden;

    [NoTranslate]
    public SlateRef<string> inSignalChoiceUsed;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        var choice = new QuestPart_Choice.Choice();

        Pawn? rewardPawn = pawn.GetValue(slate);
        if (rewardPawn is not null)
        {
            choice.rewards.Add(new Reward_Pawn
            {
                pawn = rewardPawn,
                detailsHidden = rewardDetailsHidden.GetValue(slate),
            });
        }

        IEnumerable<Thing>? rewardItems = items.GetValue(slate);
        if (rewardItems is not null)
        {
            var itemReward = new Reward_Items();
            itemReward.items.AddRange(rewardItems);
            if (itemReward.items.Count > 0)
                choice.rewards.Add(itemReward);
        }

        var choicePart = new QuestPart_Choice
        {
            inSignalChoiceUsed = QuestGenUtility.HardcodedSignalWithQuestID(inSignalChoiceUsed.GetValue(slate)),
        };
        choicePart.choices.Add(choice);
        QuestGen.quest.AddPart(choicePart);
    }
}
