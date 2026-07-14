using System.Reflection.Emit;
using HarmonyLib;

namespace Disharmony;

public static class CodeInstructionUtil
{
    extension (CodeInstruction)
    {
        public static CodeInstruction LoadLocalAddress(int index)
        {
            return new(index <= 255 ? OpCodes.Ldloca_S : OpCodes.Ldloca, index);
        }
    }
}