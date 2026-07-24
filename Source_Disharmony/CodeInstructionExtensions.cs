namespace Disharmony;

public static class CodeInstructionExtensions
{
    extension(CodeInstruction inst)
    {
        public static CodeInstruction Annotation(string str) => new(OpCodes.Nop, str);

        public bool IsLeave => inst.opcode == OpCodes.Leave_S || inst.opcode == OpCodes.Leave;

        public bool CanBranch => inst.opcode.FlowControl is not (FlowControl.Next or FlowControl.Call or FlowControl.Meta);

        public bool CanFallThrough => inst.opcode.FlowControl is FlowControl.Next or FlowControl.Call or FlowControl.Meta or FlowControl.Cond_Branch;
    }
}
