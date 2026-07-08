namespace XylXenos;

public class Command_ToggleWithRightClickOptions : Command_Toggle
{
    public override IEnumerable<FloatMenuOption>? RightClickFloatMenuOptions => rightClickFloatMenuOptions;

    public List<FloatMenuOption>? rightClickFloatMenuOptions;
}
