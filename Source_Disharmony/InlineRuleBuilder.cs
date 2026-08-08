namespace Disharmony;

internal class InlineRuleBuilder : RuleBuilder
{
    private readonly MethodBase method;
    private readonly LocalTracker[] argumentLocals;
    private LocalTracker? returnLocal = null;
    private readonly Dictionary<int, LocalTracker> localMap = new();
    private readonly Type[] parameterTypes;
    private readonly List<LocalVariableInfo>? locals;

    public InlineRuleBuilder(RuleBuilderContext context, MethodInvocation patch) : base(context, EmptyInvocation.Instance)
    {
        method = patch.MethodInfo;

        parameterTypes = patch.ParameterTypes;
        argumentLocals = new LocalTracker[parameterTypes.Length];
        locals = method.GetMethodBody()?.LocalVariables.ToList();
    }

    private bool EmitReplacement()
    {
        if (locals is null)
            throw new InvalidOperationException();

        for (int i = parameterTypes.Length - 1; i >= 0; i--)
        {
            argumentLocals[i] = output.AddLocal(parameterTypes[i]);
            output.Add(argumentLocals[i].Store());
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
                OpCodeValues.Ldarg_0  => argumentLocals[0].Load(),
                OpCodeValues.Ldarg_1  => argumentLocals[1].Load(),
                OpCodeValues.Ldarg_2  => argumentLocals[2].Load(),
                OpCodeValues.Ldarg_3  => argumentLocals[3].Load(),
                OpCodeValues.Ldarg    => argumentLocals[Convert.ToInt32(inst.operand)].Load(),
                OpCodeValues.Ldarg_S  => argumentLocals[Convert.ToInt32(inst.operand)].Load(),
                OpCodeValues.Ldarga   => argumentLocals[Convert.ToInt32(inst.operand)].Load(true),
                OpCodeValues.Ldarga_S => argumentLocals[Convert.ToInt32(inst.operand)].Load(true),
                OpCodeValues.Starg    => argumentLocals[Convert.ToInt32(inst.operand)].Store(),
                OpCodeValues.Starg_S  => argumentLocals[Convert.ToInt32(inst.operand)].Store(),
                OpCodeValues.Ldloc_0  => GetLocal(0).Load(),
                OpCodeValues.Ldloc_1  => GetLocal(1).Load(),
                OpCodeValues.Ldloc_2  => GetLocal(2).Load(),
                OpCodeValues.Ldloc_3  => GetLocal(3).Load(),
                OpCodeValues.Ldloc    => GetLocal(GetLocalIndex(inst.operand)).Load(),
                OpCodeValues.Ldloc_S  => GetLocal(GetLocalIndex(inst.operand)).Load(),
                OpCodeValues.Ldloca   => GetLocal(GetLocalIndex(inst.operand)).Load(true),
                OpCodeValues.Ldloca_S => GetLocal(GetLocalIndex(inst.operand)).Load(true),
                OpCodeValues.Stloc_0  => GetLocal(0).Store(),
                OpCodeValues.Stloc_1  => GetLocal(1).Store(),
                OpCodeValues.Stloc_2  => GetLocal(2).Store(),
                OpCodeValues.Stloc_3  => GetLocal(3).Store(),
                OpCodeValues.Stloc    => GetLocal(GetLocalIndex(inst.operand)).Store(),
                OpCodeValues.Stloc_S  => GetLocal(GetLocalIndex(inst.operand)).Store(),
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
        if (returnLocal != null)
        {
            output.Add(returnLocal.Store());
            output.Add(returnLocal.Load());
        }

        return true;
    }

    private LocalTracker GetLocal(int index)
    {
        if (locals is null)
            throw new InvalidOperationException();

        if (!localMap.TryGetValue(index, out var value))
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
            phase = 2,
            mode = OutputMode.Replace,
            pattern = [.. pattern],
            output = [.. output.instructions],
            name = method.FullName,
        };
    }
}
