namespace Disharmony;

public static class CodeInstructionExtensions
{
    extension(CodeInstruction inst)
    {
        public static CodeInstruction Annotation(string str) => new(OpCodes.Nop, str);
    }
}
