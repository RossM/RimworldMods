#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
namespace Disharmony;

internal static class TranspilerTemplate
{
    public static InstructionMatcher[] matchers;

    public static List<CodeInstruction> Invoke(MethodBase target, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        return InstructionMatcher.RunMatchers(matchers, target, instructions, generator);
    }
}
