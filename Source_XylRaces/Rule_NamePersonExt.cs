using Verse.Grammar;

namespace XylXenos;

[UsedFromXml]
public class Rule_NamePersonExt : Rule
{
    public override float BaseSelectionWeight => 1f;
    public Gender gender;
    public PawnNameSlot slot = PawnNameSlot.First;

    public override Rule DeepCopy()
    {
        Rule_NamePersonExt copy = (Rule_NamePersonExt)base.DeepCopy();
        copy.gender = gender;
        copy.slot = slot;
        return copy;
    }

    public override string Generate()
    {
        NameBank? nameBank = PawnNameDatabaseShuffled.BankOf(PawnNameCategory.HumanStandard);
        DebugAssert.NotNull(nameBank);

        return nameBank.GetName(slot, gender, checkIfAlreadyUsed: false);
    }

    public override string ToString()
    {
        // ReSharper disable once StringLiteralTypo
        return $"{keyword}->(personname_{gender}_{slot})";
    }
}
