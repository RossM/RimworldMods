using System.Reflection;
using System.Reflection.Emit;

namespace XylXenos.Patches;

// GeneDef.GetDescriptionFull doesn't check whether thought stages are null, resulting in a NullReferenceException
// when processing XylHyperlactation and XylSoreBreasts.
[HarmonyPatch(typeof(GeneDef))]
public static class Patch_GeneDef_GetDescriptionFull
{
    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        var label = generator.DefineLabel();

        yield return new(OpCodes.Ldarg_1);
        yield return new(OpCodes.Brtrue_S, label);
        yield return new(OpCodes.Ldc_I4_0);
        yield return new(OpCodes.Ret);
        yield return new(OpCodes.Nop) { labels = [label] };
        foreach (var instruction in instructions)
            yield return instruction;
    }

    [HarmonyTargetMethods]
    public static IEnumerable<MethodInfo> TargetMethods()
    {
        var type = AccessTools.TypeByName("Verse.GeneDef");
        type = type.GetNestedType("<>c", AccessTools.all);
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            var parameters = method.GetParameters();
            if (method.ReturnType == typeof(bool) && parameters.Length == 1 && parameters[0].ParameterType == typeof(ThoughtStage))
                yield return method;
        }
    }
}
