namespace Disharmony;

internal class InlineRuleBuilder : RuleBuilder
{
    private static class OpCodeValue
    {
        // ReSharper disable IdentifierTypo
        // @formatter:off
        public const int Ldarg_0  =   0x02;
        public const int Ldarg_1  =   0x03;
        public const int Ldarg_2  =   0x04;
        public const int Ldarg_3  =   0x05;
        public const int Ldarg    = 0xFE09;
        public const int Ldarg_S  =   0x0E;
        public const int Ldarga   = 0xFE0A;
        public const int Ldarga_S =   0x0F;
        public const int Ldloc_0  =   0x06;
        public const int Ldloc_1  =   0x07;
        public const int Ldloc_2  =   0x08;
        public const int Ldloc_3  =   0x09;
        public const int Ldloc    = 0xFE0C;
        public const int Ldloc_S  =   0x11;
        public const int Ldloca   = 0xFE0D;
        public const int Ldloca_S =   0x12;
        public const int Stloc_0  =   0x0A;
        public const int Stloc_1  =   0x0B;
        public const int Stloc_2  =   0x0C;
        public const int Stloc_3  =   0x0D;
        public const int Stloc    = 0xFE0E;
        public const int Stloc_S  =   0x13;
        public const int Ret      =   0x2A;
        // @formatter:on
        // ReSharper restore IdentifierTypo
    }

    private readonly MethodBase method;
    private readonly int[] argumentLocals;
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
                OpCodeValue.Ldarg_0  => CodeInstruction.LoadLocal(argumentLocals[0]),
                OpCodeValue.Ldarg_1  => CodeInstruction.LoadLocal(argumentLocals[1]),
                OpCodeValue.Ldarg_2  => CodeInstruction.LoadLocal(argumentLocals[2]),
                OpCodeValue.Ldarg_3  => CodeInstruction.LoadLocal(argumentLocals[3]),
                OpCodeValue.Ldarg    => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)]),
                OpCodeValue.Ldarg_S  => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)]),
                OpCodeValue.Ldarga   => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)], true),
                OpCodeValue.Ldarga_S => CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)], true),
                OpCodeValue.Ldloc_0  => CodeInstruction.LoadLocal(GetLocal(0)),
                OpCodeValue.Ldloc_1  => CodeInstruction.LoadLocal(GetLocal(1)),
                OpCodeValue.Ldloc_2  => CodeInstruction.LoadLocal(GetLocal(2)),
                OpCodeValue.Ldloc_3  => CodeInstruction.LoadLocal(GetLocal(3)),
                OpCodeValue.Ldloc    => CodeInstruction.LoadLocal(GetLocal(Convert.ToInt32(inst.operand))),
                OpCodeValue.Ldloc_S  => CodeInstruction.LoadLocal(GetLocal(Convert.ToInt32(inst.operand))),
                OpCodeValue.Ldloca   => CodeInstruction.LoadLocal(GetLocal(Convert.ToInt32(inst.operand)), true),
                OpCodeValue.Ldloca_S => CodeInstruction.LoadLocal(GetLocal(Convert.ToInt32(inst.operand)), true),
                OpCodeValue.Stloc_0  => CodeInstruction.StoreLocal(GetLocal(0)),
                OpCodeValue.Stloc_1  => CodeInstruction.StoreLocal(GetLocal(1)),
                OpCodeValue.Stloc_2  => CodeInstruction.StoreLocal(GetLocal(2)),
                OpCodeValue.Stloc_3  => CodeInstruction.StoreLocal(GetLocal(3)),
                OpCodeValue.Stloc    => CodeInstruction.StoreLocal(GetLocal(Convert.ToInt32(inst.operand))),
                OpCodeValue.Stloc_S  => CodeInstruction.StoreLocal(GetLocal(Convert.ToInt32(inst.operand))),
                OpCodeValue.Ret      => new(OpCodes.Br, returnLabel),
                _ => inst,
                // @formatter:on
            };

            translated.labels = inst.labels;
            translated.blocks = inst.blocks;

            output.Add(translated);
        }

        output.Add(CodeInstruction.Annotation("End inlined method body"));

        output.Add(new(OpCodes.Nop) { labels = [returnLabel] });

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
            Min = 1,
            Max = 0,
            Mode = InstructionMatcher.OutputMode.Replace,
            Pattern = pattern.ToArray(),
            Output = output.Instructions.ToArray(),
            Name = method.FullName,
        };
    }
}
