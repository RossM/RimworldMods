namespace Disharmony;

public static class CodeInstructionExtensions
{
    extension(CodeInstruction)
    {
        public static CodeInstruction Annotation(string str) => new(OpCodes.Nop, str);
    }
}
