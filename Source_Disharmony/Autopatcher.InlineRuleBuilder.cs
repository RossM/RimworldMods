namespace Disharmony;

public static partial class Autopatcher
{
    private class InlineRuleBuilder
    {
        private readonly InstructionList output = new();
        private readonly MethodInfo method;
        private readonly int[] argumentLocals;
        private readonly Dictionary<int, int> localMap = new();
        private readonly ParameterInfo[] parameters;
        private readonly ILGenerator generator = PatchProcessor.CreateILGenerator();
        private readonly List<LocalVariableInfo> locals;

        public InlineRuleBuilder(PatchInfo patch)
        {
            method = patch.patchMethod;

            parameters = patch.patchMethod.GetParameters();
            argumentLocals = new int[parameters.Length];
            locals = patch.patchMethod.GetMethodBody().LocalVariables.ToList();
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
                if (inst.opcode == OpCodes.Ldarg_0)
                    translated = CodeInstruction.LoadLocal(argumentLocals[0]);
                else if (inst.opcode == OpCodes.Ldarg_1)
                    translated = CodeInstruction.LoadLocal(argumentLocals[1]);
                else if (inst.opcode == OpCodes.Ldarg_2)
                    translated = CodeInstruction.LoadLocal(argumentLocals[2]);
                else if (inst.opcode == OpCodes.Ldarg_3)
                    translated = CodeInstruction.LoadLocal(argumentLocals[3]);
                else if (inst.opcode == OpCodes.Ldarg || inst.opcode == OpCodes.Ldarg_S)
                    translated = CodeInstruction.LoadLocal(argumentLocals[Convert.ToInt32(inst.operand)]);
                else if (inst.opcode == OpCodes.Ldarga || inst.opcode == OpCodes.Ldarga_S)
                    translated = CodeInstruction.LoadLocalAddress(argumentLocals[Convert.ToInt32(inst.operand)]);
                else if (inst.opcode == OpCodes.Ldloc_0)
                    translated = CodeInstruction.LoadLocal(GetLocal(0));
                else if (inst.opcode == OpCodes.Ldloc_1)
                    translated = CodeInstruction.LoadLocal(GetLocal(1));
                else if (inst.opcode == OpCodes.Ldloc_2)
                    translated = CodeInstruction.LoadLocal(GetLocal(2));
                else if (inst.opcode == OpCodes.Ldloc_3)
                    translated = CodeInstruction.LoadLocal(GetLocal(3));
                else if (inst.opcode == OpCodes.Ldloc || inst.opcode == OpCodes.Ldloc_S)
                    translated = CodeInstruction.LoadLocal(GetLocal(Convert.ToInt32(inst.operand)));
                else if (inst.opcode == OpCodes.Ldloca || inst.opcode == OpCodes.Ldloca_S)
                    translated = CodeInstruction.LoadLocalAddress(GetLocal(Convert.ToInt32(inst.operand)));
                else if (inst.opcode == OpCodes.Stloc_0)
                    translated = CodeInstruction.StoreLocal(GetLocal(0));
                else if (inst.opcode == OpCodes.Stloc_1)
                    translated = CodeInstruction.StoreLocal(GetLocal(1));
                else if (inst.opcode == OpCodes.Stloc_2)
                    translated = CodeInstruction.StoreLocal(GetLocal(2));
                else if (inst.opcode == OpCodes.Stloc_3)
                    translated = CodeInstruction.StoreLocal(GetLocal(3));
                else if (inst.opcode == OpCodes.Stloc || inst.opcode == OpCodes.Stloc_S)
                    translated = CodeInstruction.StoreLocal(GetLocal(Convert.ToInt32(inst.operand)));
                else if (inst.opcode == OpCodes.Ret)
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

        public IEnumerable<InstructionMatcher.Rule> BuildRules()
        {
            List<CodeInstruction> pattern =
            [
                new(OpCodes.Call, method),
            ];

            if (!EmitReplacement())
                yield break;

            yield return new InstructionMatcher.Rule
            {
                Min = 1,
                Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = output.Instructions.ToArray(),
                LocalTypes = output.LocalTypes.ToArray(),
                Name = method.FullName,
            };
        }
    }
}
