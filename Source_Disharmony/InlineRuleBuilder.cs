namespace Disharmony;

internal class InlineRuleBuilder : RuleBuilder
{
    private readonly MethodBase method;
    private readonly int[] argumentLocals;
    private int returnLocal = -1;
    private readonly Dictionary<int, int> localMap = new();
    private readonly Type[] parameterTypes;
    private readonly List<LocalVariableInfo>? locals;

    public InlineRuleBuilder(RuleBuilderContext context, MethodInvocation patch) : base(context, EmptyInvocation.Instance)
    {
        method = patch.MethodInfo;

        parameterTypes = patch.ParameterTypes;
        argumentLocals = new int[parameterTypes.Length];
        locals = method.GetMethodBody()?.LocalVariables.ToList();
    }

    private bool EmitReplacement()
    {
        if (locals is null)
            throw new InvalidOperationException();

        for (int i = parameterTypes.Length - 1; i >= 0; i--)
        {
            argumentLocals[i] = output.AddLocal(parameterTypes[i]);
            output.Add(CodeInstruction.StoreLocal(argumentLocals[i]));
        }

        if (method is MethodInfo m && m.ReturnType != typeof(void))
            returnLocal = output.AddLocal(m.ReturnType);

        var instructions = PatchProcessor.GetOriginalInstructions(method, generator);
        if (instructions == null)
            return false;

        Label returnLabel = generator.DefineLabel();

        output.Add(CodeInstruction.Annotation("Begin inlined method body"));

        foreach (var inst in instructions)
        {
            CodeInstruction translated = unchecked((ushort)inst.opcode.Value) switch
            {
                // @formatter:off
                OpCodeValues.Ldarg_0  => CodeInstruction.LoadLocal(argumentLocals[0]),
                OpCodeValues.Ldarg_1  => CodeInstruction.LoadLocal(argumentLocals[1]),
                OpCodeValues.Ldarg_2  => CodeInstruction.LoadLocal(argumentLocals[2]),
                OpCodeValues.Ldarg_3  => CodeInstruction.LoadLocal(argumentLocals[3]),
                OpCodeValues.Ldarg    => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)]),
                OpCodeValues.Ldarg_S  => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)]),
                OpCodeValues.Ldarga   => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)], true),
                OpCodeValues.Ldarga_S => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)], true),
                OpCodeValues.Ldloc_0  => CodeInstruction.LoadLocal(GetLocal(0)),
                OpCodeValues.Ldloc_1  => CodeInstruction.LoadLocal(GetLocal(1)),
                OpCodeValues.Ldloc_2  => CodeInstruction.LoadLocal(GetLocal(2)),
                OpCodeValues.Ldloc_3  => CodeInstruction.LoadLocal(GetLocal(3)),
                OpCodeValues.Ldloc    => CodeInstruction.LoadLocal(GetLocal(GetLocalIndex(inst.operand))),
                OpCodeValues.Ldloc_S  => CodeInstruction.LoadLocal(GetLocal(GetLocalIndex(inst.operand))),
                OpCodeValues.Ldloca   => CodeInstruction.LoadLocal(GetLocal(GetLocalIndex(inst.operand)), true),
                OpCodeValues.Ldloca_S => CodeInstruction.LoadLocal(GetLocal(GetLocalIndex(inst.operand)), true),
                OpCodeValues.Stloc_0  => CodeInstruction.StoreLocal(GetLocal(0)),
                OpCodeValues.Stloc_1  => CodeInstruction.StoreLocal(GetLocal(1)),
                OpCodeValues.Stloc_2  => CodeInstruction.StoreLocal(GetLocal(2)),
                OpCodeValues.Stloc_3  => CodeInstruction.StoreLocal(GetLocal(3)),
                OpCodeValues.Stloc    => CodeInstruction.StoreLocal(GetLocal(GetLocalIndex(inst.operand))),
                OpCodeValues.Stloc_S  => CodeInstruction.StoreLocal(GetLocal(GetLocalIndex(inst.operand))),
                OpCodeValues.Ret      => new(OpCodes.Br, returnLabel),
                _ => inst,
                // @formatter:on
            };

            translated.labels = inst.labels;
            translated.blocks = inst.blocks;

            output.Add(translated);
        }

        output.Add(CodeInstruction.Annotation("End inlined method body"));

        output.Add(new(OpCodes.Nop) { labels = [returnLabel] });

        // It's necessary to do a type conversion here to simulate a return correctly. For now, do it by storing to a local.
        if (returnLocal >= 0)
        {
            output.Add(CodeInstruction.StoreLocal(returnLocal));
            output.Add(CodeInstruction.LoadLocal(returnLocal));
        }

        return true;
    }

    private int GetLocal(int index)
    {
        if (locals is null)
            throw new InvalidOperationException();

        if (!localMap.TryGetValue(index, out int value))
            localMap[index] = value = output.AddLocal(locals[index].LocalType);
        return value;
    }

    private static int GetLocalIndex(object? operand) => operand is LocalBuilder localBuilder
        ? localBuilder.LocalIndex
        : Convert.ToInt32(operand);

    public override IEnumerable<Rule> BuildRules()
    {
        if (locals == null)
            yield break;

        List<CodeInstruction> pattern =
        [
            new(OpCodes.Call, method),
        ];

        if (!EmitReplacement())
            yield break;

        yield return new Rule
        {
            min = 1,
            max = 0,
            mode = InstructionMatcher.OutputMode.Replace,
            pattern = [.. pattern],
            output = [.. output.instructions],
            name = method.FullName,
        };
    }
}
