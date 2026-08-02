namespace Disharmony;

internal partial class Optimizer
{
    internal class VariableToStackConverter(Optimizer optimizer)
    {
        private readonly Dictionary<Variable, LocalBuilder> spillLocals = [];

        // Replaces canonical variable operands with an executable CIL stack schedule, then
        // invalidates the variable-form data that no longer describes the operations.
        public void ConvertVariablesToStack()
        {
            Dictionary<BasicBlock, List<Variable>> exitStacks = CheckPreconditions();

            foreach (var block in optimizer.basicBlocks)
                ConvertBlock(block, exitStacks[block]);

            // Variable information is no longer canonical once the operations have been stackified.
            // Clear it so later stack-form passes cannot accidentally consume stale dataflow.
            foreach (var block in optimizer.basicBlocks)
                block.entryStackVariables.Clear();

            optimizer.variables.Clear();
            optimizer.argumentVariables.Clear();
            optimizer.localVariables.Clear();
            optimizer.nextVariableId = 0;
            optimizer.Form = IrForm.Stack;
        }

        // Validate the complete input before mutating any block. In particular, edge assignments
        // belong only to the future SSA forms and must already have been eliminated before lowering.
        private Dictionary<BasicBlock, List<Variable>> CheckPreconditions()
        {
            if (optimizer.Form != IrForm.Variables)
                throw new InvalidOperationException($"Cannot convert {optimizer.Form} form to stack");

            ControlFlowEdge? assignedEdge = optimizer.basicBlocks.SelectMany(block => block.outgoingEdges)
                .FirstOrDefault(edge => edge.assignments.Count != 0);
            if (assignedEdge != null)
            {
                throw new InvalidOperationException(
                    $"Cannot lower variables to stack while edge {assignedEdge.Source.ID} => " +
                    $"{assignedEdge.Target.ID} still has SSA assignments");
            }

            Dictionary<BasicBlock, List<Variable>> exitStacks = [];
            foreach (var block in optimizer.basicBlocks)
            {
                foreach (var op in block.ops)
                {
                    if (op.stackInputCount < 0 || op.stackInputCount > op.inputs.Count ||
                        op.stackOutputCount < 0 || op.stackOutputCount > op.outputs.Count)
                    {
                        throw new InvalidOperationException(
                            $"Invalid variable operand counts on {op.Opcode} in {block.ID}");
                    }
                }

                List<Variable> exitStack = GetNaturalExitStack(block);
                foreach (var edge in block.outgoingEdges)
                {
                    if (!exitStack.SequenceEqual(edge.Target.entryStackVariables))
                    {
                        throw new InvalidOperationException(
                            $"Natural exit stack of {block.ID} does not match the entry stack of {edge.Target.ID}");
                    }
                }

                exitStacks.Add(block, exitStack);
            }

            return exitStacks;
        }

        // Postcondition: block.ops is an executable stack-form sequence that realizes every
        // canonical operand use and leaves the successor entry stack on the CIL stack.
        private void ConvertBlock(BasicBlock block, List<Variable> exitStack)
        {
            List<Variable> stack = [.. block.entryStackVariables];
            Dictionary<Variable, int> remainingUses = CountRemainingUses(block, exitStack);
            List<Op> operations = [];

            foreach (var op in block.ops)
            {
                List<Variable> inputs = [.. op.inputs.Take(op.stackInputCount)];
                foreach (var input in inputs)
                    RemoveUse(remainingUses, input);

                ArrangeStack(
                    stack,
                    inputs,
                    remainingUses,
                    operations,
                    block,
                    op.outputs.Take(op.stackOutputCount));

                stack.RemoveRange(stack.Count - inputs.Count, inputs.Count);
                operations.Add(ConvertOperation(op));
                stack.AddRange(op.outputs.Take(op.stackOutputCount));
                if (op.ClearsStack)
                    stack.Clear();
            }

            foreach (var variable in exitStack)
                RemoveUse(remainingUses, variable);
            ArrangeStack(stack, exitStack, remainingUses, operations, block, exact: true);

            if (remainingUses.Count != 0)
                throw new InvalidOperationException($"Unaccounted variable uses remain in {block.ID}");

            block.ops.Clear();
            block.ops.AddRange(operations);
        }

        // Replay only the recorded CIL stack arities. Canonical operand rewrites may change which
        // variables an operation consumes, but they do not change its intrinsic stack effect.
        private static List<Variable> GetNaturalExitStack(BasicBlock block)
        {
            List<Variable> stack = [.. block.entryStackVariables];
            foreach (var op in block.ops)
            {
                if (op.stackInputCount > stack.Count)
                    throw new InvalidOperationException($"{op.Opcode} reads past the natural stack in {block.ID}");
                stack.RemoveRange(stack.Count - op.stackInputCount, op.stackInputCount);
                stack.AddRange(op.outputs.Take(op.stackOutputCount));
                if (op.ClearsStack)
                    stack.Clear();
            }

            return stack;
        }

        // These counts are scheduling obligations: the final available copy of a value cannot be
        // consumed or popped while any operation or exit slot still needs it.
        private static Dictionary<Variable, int> CountRemainingUses(
            BasicBlock block,
            IEnumerable<Variable> exitStack)
        {
            Dictionary<Variable, int> uses = [];
            foreach (var variable in block.ops.SelectMany(op => op.inputs.Take(op.stackInputCount))
                         .Concat(exitStack))
                uses[variable] = uses.GetValueSafe(variable) + 1;
            return uses;
        }

        private static void RemoveUse(Dictionary<Variable, int> uses, Variable variable)
        {
            int count = uses[variable] - 1;
            if (count == 0)
                uses.Remove(variable);
            else
                uses[variable] = count;
        }

        // Postcondition: required is on top of the modeled stack (or is the entire stack when
        // exact), and operations contains the spills, pops, and reloads needed to make it so.
        private void ArrangeStack(
            List<Variable> stack,
            IReadOnlyList<Variable> required,
            IReadOnlyDictionary<Variable, int> futureUses,
            List<Op> operations,
            BasicBlock block,
            IEnumerable<Variable>? producedVariables = null,
            bool exact = false)
        {
            int prefixCount = stack.Count - required.Count;
            bool inputsAlreadyOnTop = exact
                ? stack.SequenceEqual(required)
                : prefixCount >= 0 && stack.Skip(prefixCount).SequenceEqual(required);

            if (inputsAlreadyOnTop && CanConsumeWithoutLosingValues(
                    stack, prefixCount, required, producedVariables ?? [], futureUses))
                return;

            HashSet<Variable> needed = [.. required, .. futureUses.Keys];
            int keepCount = exact ? 0 : Math.Max(prefixCount, 0);
            foreach (var variable in required.Distinct())
            {
                if (HasStorage(variable))
                    continue;

                int availableIndex = stack.LastIndexOf(variable);
                if (availableIndex >= 0 && availableIndex < keepCount)
                    keepCount = availableIndex;
            }

            try
            {
                EmptyStack(stack, keepCount, needed, operations);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException(
                    $"Cannot arrange stack [{string.Join(", ", stack)}] as [{string.Join(", ", required)}] in {block.ID}",
                    exception);
            }
            foreach (var variable in required)
            {
                operations.Add(LoadVariable(variable, block));
                stack.Add(variable);
            }
        }

        // A ready-made stack layout is usable only if consuming it leaves some representation of
        // every value that still has future uses, either in storage or on the stack.
        private bool CanConsumeWithoutLosingValues(
            IReadOnlyList<Variable> stack,
            int prefixCount,
            IEnumerable<Variable> consumedVariables,
            IEnumerable<Variable> producedVariables,
            IReadOnlyDictionary<Variable, int> futureUses)
        {
            foreach (var variable in consumedVariables.Distinct())
            {
                futureUses.TryGetValue(variable, out int useCount);
                if (HasStorage(variable))
                    continue;

                int remainingStackCopies = stack.Take(prefixCount).Count(candidate => candidate == variable) +
                                           producedVariables.Count(candidate => candidate == variable);
                // One surviving copy is sufficient even for several later uses: a later dup may
                // create those copies, and preserving the existing stack schedule should not spill.
                if (useCount > 0 && remainingStackCopies == 0)
                    return false;
            }

            return true;
        }

        // Discard the selected suffix while materializing any value whose last stack copy is still
        // needed. The stack model is updated in lockstep with the emitted operations.
        private void EmptyStack(
            List<Variable> stack,
            int keepCount,
            ISet<Variable> needed,
            List<Op> operations)
        {
            for (int index = stack.Count - 1; index >= keepCount; index--)
            {
                Variable variable = stack[index];
                if (needed.Contains(variable) && !HasStorage(variable))
                    operations.Add(StoreVariable(variable));
                else
                    operations.Add(Ops.Pop);
            }

            stack.RemoveRange(keepCount, stack.Count - keepCount);
        }

        // Storage accesses must follow their canonical variable operand, which an optimization may
        // have changed independently of the original instruction's numeric operand.
        private Op ConvertOperation(Op op)
        {
            (Op.VariableAccessKind Kind, bool Argument, Variable Variable)? access = GetStorageAccess(op);
            Op operation;
            if (access is not { } variableAccess ||
                IsOriginalStorage(variableAccess.Argument, variableAccess.Variable, op.Index))
            {
                operation = new(op.Opcode, op.Operand);
            }
            else
            {
                Storage storage = GetStorage(variableAccess.Variable);
                operation = variableAccess.Kind switch
                {
                    Op.VariableAccessKind.Read => LoadStorage(storage),
                    Op.VariableAccessKind.Write => StoreStorage(storage),
                    Op.VariableAccessKind.Address => LoadStorageAddress(storage),
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            operation.prefixes.AddRange(op.prefixes);
            return operation;
        }

        // Evaluation-stack variables occupy the front of each operand list; the following variable
        // identifies the argument or local storage accessed by this opcode.
        private static (Op.VariableAccessKind Kind, bool Argument, Variable Variable)? GetStorageAccess(Op op)
        {
            ushort opcode = unchecked((ushort)op.Opcode.Value);
            return opcode switch
            {
                OpCodeValues.Ldarg_0 or OpCodeValues.Ldarg_1 or OpCodeValues.Ldarg_2 or OpCodeValues.Ldarg_3 or
                    OpCodeValues.Ldarg or OpCodeValues.Ldarg_S =>
                    (Op.VariableAccessKind.Read, true, op.inputs[op.stackInputCount]),
                OpCodeValues.Ldarga or OpCodeValues.Ldarga_S =>
                    (Op.VariableAccessKind.Address, true, op.inputs[op.stackInputCount]),
                OpCodeValues.Starg or OpCodeValues.Starg_S =>
                    (Op.VariableAccessKind.Write, true, op.outputs[op.stackOutputCount]),
                OpCodeValues.Ldloc_0 or OpCodeValues.Ldloc_1 or OpCodeValues.Ldloc_2 or OpCodeValues.Ldloc_3 or
                    OpCodeValues.Ldloc or OpCodeValues.Ldloc_S =>
                    (Op.VariableAccessKind.Read, false, op.inputs[op.stackInputCount]),
                OpCodeValues.Ldloca or OpCodeValues.Ldloca_S =>
                    (Op.VariableAccessKind.Address, false, op.inputs[op.stackInputCount]),
                OpCodeValues.Stloc_0 or OpCodeValues.Stloc_1 or OpCodeValues.Stloc_2 or OpCodeValues.Stloc_3 or
                    OpCodeValues.Stloc or OpCodeValues.Stloc_S =>
                    (Op.VariableAccessKind.Write, false, op.outputs[op.stackOutputCount]),
                _ => null,
            };
        }

        private static bool IsOriginalStorage(bool argument, Variable variable, int originalIndex) =>
            variable.index == originalIndex && variable.kind == (argument ? VariableKind.Argument : VariableKind.Local);

        private bool HasStorage(Variable variable) =>
            variable.kind is VariableKind.Argument or VariableKind.Local || spillLocals.ContainsKey(variable);

        // Original arguments and locals retain their storage identity. Logical values share one
        // lazily declared spill local across all blocks that preserve or reload that value.
        private Storage GetStorage(Variable variable)
        {
            switch (variable.kind)
            {
                case VariableKind.Argument: return new(VariableKind.Argument, variable.index, null);
                case VariableKind.Local: return new(VariableKind.Local, variable.index, variable.localBuilder);
            }

            if (!spillLocals.TryGetValue(variable, out LocalBuilder? local))
            {
                if (variable.type == null || IsSpecialType(variable.type))
                    throw new InvalidOperationException($"Cannot spill {variable}: its exact CIL type is unknown");
                local = optimizer.generator.DeclareLocal(variable.type);
                spillLocals.Add(variable, local);
            }

            return new(VariableKind.Local, local.LocalIndex, local);
        }

        private Op LoadVariable(Variable variable, BasicBlock block)
        {
            if (!HasStorage(variable))
                throw new InvalidOperationException($"{variable} is not available when rebuilding the stack in {block.ID}");
            return LoadStorage(GetStorage(variable));
        }

        private Op StoreVariable(Variable variable) => StoreStorage(GetStorage(variable));

        private static Op LoadStorage(Storage storage) => storage.Kind switch
        {
            VariableKind.Argument => new(OpCodes.Ldarg, storage.Index),
            VariableKind.Local => new(OpCodes.Ldloc, storage.Operand),
            _ => throw new ArgumentOutOfRangeException(),
        };

        private static Op StoreStorage(Storage storage) => storage.Kind switch
        {
            VariableKind.Argument => new(OpCodes.Starg, storage.Index),
            VariableKind.Local => new(OpCodes.Stloc, storage.Operand),
            _ => throw new ArgumentOutOfRangeException(),
        };

        private static Op LoadStorageAddress(Storage storage) => storage.Kind switch
        {
            VariableKind.Argument => new(OpCodes.Ldarga, storage.Index),
            VariableKind.Local => new(OpCodes.Ldloca, storage.Operand),
            _ => throw new ArgumentOutOfRangeException(),
        };

        private readonly record struct Storage(VariableKind Kind, int Index, LocalBuilder? LocalBuilder)
        {
            public object Operand => (object?)LocalBuilder ?? Index;
        }
    }
}
