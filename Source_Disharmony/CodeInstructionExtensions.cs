namespace Disharmony;

public static class CodeInstructionExtensions
{
    extension(CodeInstruction inst)
    {
        public static CodeInstruction Annotation(string str) => new(OpCodes.Nop, str);

        public bool CanBranch => inst.opcode.FlowControl is not (FlowControl.Next or FlowControl.Call or FlowControl.Meta);
    }
}
