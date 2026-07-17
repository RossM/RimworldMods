namespace Disharmony;

public static partial class Autopatcher
{
    private class InlineRuleBuilder : RuleBuilder
    {
        private readonly MethodInfo method;
        private readonly int[] argumentLocals;
        private readonly Dictionary<int, int> localMap = new();
        private readonly ParameterInfo[] parameters;
        private readonly List<LocalVariableInfo> locals;

        public InlineRuleBuilder(RuleBuilderContext context, PatchInfo patch) : base(context)
        {
            method = patch.patchMethod;

            parameters = patch.patchMethod.GetParameters();
            argumentLocals = new int[parameters.Length];
            locals = patch.patchMethod.GetMethodBody().LocalVariables.ToList();
        }

        static class OpCodeValue
        {
            public static short Ldarg_0 = OpCodes.Ldarg_0.Value;
            public static short Ldarg_1 = OpCodes.Ldarg_1.Value;
            public static short Ldarg_2 = OpCodes.Ldarg_2.Value;
            public static short Ldarg_3 = OpCodes.Ldarg_3.Value;
            public static short Ldarg = OpCodes.Ldarg.Value;
            public static short Ldarg_S = OpCodes.Ldarg_S.Value;
            public static short Ldarga = OpCodes.Ldarga.Value;
            public static short Ldarga_S = OpCodes.Ldarga_S.Value;
            public static short Ldloc_0 = OpCodes.Ldloc_0.Value;
            public static short Ldloc_1 = OpCodes.Ldloc_1.Value;
            public static short Ldloc_2 = OpCodes.Ldloc_2.Value;
            public static short Ldloc_3 = OpCodes.Ldloc_3.Value;
            public static short Ldloc = OpCodes.Ldloc.Value;
            public static short Ldloc_S = OpCodes.Ldloc_S.Value;
            public static short Ldloca = OpCodes.Ldloca.Value;
            public static short Ldloca_S = OpCodes.Ldloca_S.Value;
            public static short Stloc_0 = OpCodes.Stloc_0.Value;
            public static short Stloc_1 = OpCodes.Stloc_1.Value;
            public static short Stloc_2 = OpCodes.Stloc_2.Value;
            public static short Stloc_3 = OpCodes.Stloc_3.Value;
            public static short Stloc = OpCodes.Stloc.Value;
            public static short Stloc_S = OpCodes.Stloc_S.Value;
            public static short Ret = OpCodes.Ret.Value;
        }

        private bool EmitReplacement()
        {
            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                argumentLocals[i] = output.AddLocal(parameters[i].ParameterType);
                output.Add(CodeInstruction.StoreLocal(argumentLocals[i]));
            }

            var instructions = PatchProcessor.GetOriginalInstructions(method, generator);
            if (instructions == null)
                return false;

            Label returnLabel = generator.DefineLabel();

            output.Add(CodeInstruction.Annotation("Begin inlined method body"));

            foreach (var inst in instructions)
            {
                CodeInstruction translated;
                if (inst.opcode.Value == OpCodeValue.Ldarg_0)
                    translated = CodeInstruction.LoadLocal(argumentLocals[0]);
                else if (inst.opcode.Value == OpCodeValue.Ldarg_1)
                    translated = CodeInstruction.LoadLocal(argumentLocals[1]);
                else if (inst.opcode.Value == OpCodeValue.Ldarg_2)
                    translated = CodeInstruction.LoadLocal(argumentLocals[2]);
                else if (inst.opcode.Value == OpCodeValue.Ldarg_3)
                    translated = CodeInstruction.LoadLocal(argumentLocals[3]);
                else if (inst.opcode.Value == OpCodeValue.Ldarg || inst.opcode.Value == OpCodeValue.Ldarg_S)
                    translated = CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)]);
                else if (inst.opcode.Value == OpCodeValue.Ldarga || inst.opcode.Value == OpCodeValue.Ldarga_S)
                    translated = CodeInstruction.LoadLocalAddress(argumentLocals[Convert.ToInt32(inst.operand)]);
                else if (inst.opcode.Value == OpCodeValue.Ldloc_0)
                    translated = CodeInstruction.LoadLocal(GetLocal(0));
                else if (inst.opcode.Value == OpCodeValue.Ldloc_1)
                    translated = CodeInstruction.LoadLocal(GetLocal(1));
                else if (inst.opcode.Value == OpCodeValue.Ldloc_2)
                    translated = CodeInstruction.LoadLocal(GetLocal(2));
                else if (inst.opcode.Value == OpCodeValue.Ldloc_3)
                    translated = CodeInstruction.LoadLocal(GetLocal(3));
                else if (inst.opcode.Value == OpCodeValue.Ldloc || inst.opcode.Value == OpCodeValue.Ldloc_S)
                    translated = CodeInstruction.LoadLocal(GetLocal(Convert.ToInt32(inst.operand)));
                else if (inst.opcode.Value == OpCodeValue.Ldloca || inst.opcode.Value == OpCodeValue.Ldloca_S)
                    translated = CodeInstruction.LoadLocalAddress(GetLocal(Convert.ToInt32(inst.operand)));
                else if (inst.opcode.Value == OpCodeValue.Stloc_0)
                    translated = CodeInstruction.StoreLocal(GetLocal(0));
                else if (inst.opcode.Value == OpCodeValue.Stloc_1)
                    translated = CodeInstruction.StoreLocal(GetLocal(1));
                else if (inst.opcode.Value == OpCodeValue.Stloc_2)
                    translated = CodeInstruction.StoreLocal(GetLocal(2));
                else if (inst.opcode.Value == OpCodeValue.Stloc_3)
                    translated = CodeInstruction.StoreLocal(GetLocal(3));
                else if (inst.opcode.Value == OpCodeValue.Stloc || inst.opcode.Value == OpCodeValue.Stloc_S)
                    translated = CodeInstruction.StoreLocal(GetLocal(Convert.ToInt32(inst.operand)));
                else if (inst.opcode.Value == OpCodeValue.Ret)
                    translated = new(OpCodes.Br, returnLabel);
                else
                    translated = inst;

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
            if (!localMap.TryGetValue(index, out int value))
                localMap[index] = value = output.AddLocal(locals[index].LocalType);
            return value;
        }

        public override IEnumerable<Rule> BuildRules()
        {
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
}
