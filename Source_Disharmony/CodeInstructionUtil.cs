namespace Disharmony;

public static class CodeInstructionUtil
{
    extension (CodeInstruction)
    {
        public static CodeInstruction Annotation(string str) => new(OpCodes.Nop, str);

        public static CodeInstruction LoadLocalAddress(int index) => new(index <= 255 ? OpCodes.Ldloca_S : OpCodes.Ldloca, index);
    }
}