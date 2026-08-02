using System.Reflection.Emit;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OptimizerPassTests
{
    private static readonly MethodInfo TargetMethod =
        typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.Void))!;

    [Test]
    public void MakeBasicBlocksBuildsEdgesAndResolvesLabels()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, target),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });

        optimizer.MakeBasicBlocks();

        Assert.That(optimizer.BasicBlocks, Has.Count.EqualTo(3));
        Optimizer.BasicBlock condition = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock fallthrough = optimizer.BasicBlocks[1];
        Optimizer.BasicBlock target = optimizer.BasicBlocks[2];
        Assert.That(condition.next, Is.SameAs(fallthrough));
        Assert.That(condition.ops[^1].Operand, Is.SameAs(target));
        Assert.That(condition.successors, Is.EqualTo(new[] { fallthrough, target }));
        Assert.That(fallthrough.predecessors, Is.EqualTo(new[] { condition }));
        Assert.That(target.predecessors, Is.EqualTo(new[] { condition, fallthrough }));
    }

    [Test]
    public void NopEliminationRemovesEveryNop()
    {
        Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        optimizer.NopElimination();

        Assert.That(OpCodesIn(optimizer.BasicBlocks.Single()), Is.EqualTo(new[] { OpCodes.Ret }));
    }

    [Test]
    public void JumpThreadingSkipsEmptyFallthroughBlock()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label afterEmptyBlock = generator.DefineLabel();
            Label exit = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, exit),
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Ldc_I4_1).WithLabels(afterEmptyBlock),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock condition = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock empty = optimizer.BasicBlocks[1];
        Optimizer.BasicBlock afterEmpty = optimizer.BasicBlocks[2];
        optimizer.NopElimination();

        optimizer.JumpThreading();

        Assert.That(empty.ops, Is.Empty);
        Assert.That(condition.next, Is.SameAs(afterEmpty));
        Assert.That(condition.successors, Does.Not.Contain(empty));
        Assert.That(empty.predecessors, Is.Empty);
    }

    [Test]
    public void SimpleDeadCodeEliminationRemovesUnreachableBlock()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock unreachable = optimizer.BasicBlocks[1];

        optimizer.SimpleDeadCodeElimination();

        Assert.That(optimizer.BasicBlocks, Has.Count.EqualTo(2));
        Assert.That(optimizer.BasicBlocks, Does.Not.Contain(unreachable));
        Assert.That(optimizer.Blocks, Does.Not.Contain(unreachable));
    }

    [Test]
    public void BranchEliminationReplacesRedundantConditionWithPop()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, target),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock condition = optimizer.BasicBlocks[0];

        optimizer.BranchElimination();

        Assert.That(OpCodesIn(condition), Is.EqualTo(new[] { OpCodes.Ldc_I4_0, OpCodes.Pop }));
        Assert.That(condition.successors, Has.Count.EqualTo(1));
        Assert.That(condition.successors[0], Is.SameAs(condition.next));
    }

    [Test]
    public void MergeBlocksAppendsSinglePredecessorSuccessor()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label secondBlock = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop).WithLabels(secondBlock),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock first = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock merged = optimizer.BasicBlocks[1];

        optimizer.MergeBlocks();

        Assert.That(OpCodesIn(first), Is.EqualTo(new[] { OpCodes.Ldc_I4_1, OpCodes.Pop, OpCodes.Ret }));
        Assert.That(first.next, Is.Null);
        Assert.That(first.successors, Is.Empty);
        Assert.That(merged.predecessors, Is.Empty);
    }

    [Test]
    public void AggressiveDeadCodeEliminationAndReorderFollowsControlFlowOrder()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.Block root = optimizer.Blocks[0];
        Optimizer.BasicBlock entry = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock unreachable = optimizer.BasicBlocks[1];
        Optimizer.BasicBlock target = optimizer.BasicBlocks[2];

        optimizer.AggressiveDeadCodeEliminationAndReorder();

        Assert.That(optimizer.BasicBlocks, Is.EqualTo(new[] { entry, target }));
        Assert.That(optimizer.BasicBlocks, Does.Not.Contain(unreachable));
        Assert.That(optimizer.Blocks, Is.EqualTo(new Optimizer.Block[] { root, entry, target }));
    }

    [Test]
    public void DeduceTypesPropagatesLocalTypeThroughJoin()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            LocalBuilder local = generator.DeclareLocal(typeof(string));
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldstr, "first"),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldstr, "second").WithLabels(alternative),
                new CodeInstruction(OpCodes.Stloc, local),
                new CodeInstruction(OpCodes.Ldloc, local).WithLabels(join),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock join = optimizer.BasicBlocks.Single(block =>
            block.ops.Any(op => op.Opcode == OpCodes.Ldloc));

        optimizer.DeduceTypes();

        Assert.That(join.entryStack, Is.Empty);
        Assert.That(join.entryLocals, Is.EqualTo(new[] { typeof(string) }));
    }

    [Test]
    public void ConvertStackToVariablesRepresentsCrossBlockStackWithoutCilCopies()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldstr, "value"),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Nop).WithLabels(alternative),
                new CodeInstruction(OpCodes.Pop).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.DeduceTypes();
        int operationCount = optimizer.BasicBlocks.Sum(block => block.ops.Count);
        Optimizer.BasicBlock entry = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock join = optimizer.BasicBlocks[3];
        Assert.That(join.entryStack, Is.EqualTo(new[] { typeof(string) }));

        optimizer.ConvertStackToVariables();

        Assert.That(optimizer.Form, Is.EqualTo(Optimizer.IrForm.Variables));
        Assert.That(entry.ops[2].inputs, Is.EqualTo(new[] { entry.ops[1].outputs.Single() }));
        Assert.That(entry.exitStackVariables, Is.EqualTo(new[] { entry.ops[0].outputs.Single() }));
        Assert.That(join.entryStackVariables, Has.Count.EqualTo(1));
        Assert.That(join.incomingEdges, Has.Count.EqualTo(2));
        Assert.That(join.incomingEdges.SelectMany(edge => edge.assignments), Has.All.Matches<Optimizer.VariableAssignment>(
            assignment => assignment.Destination == join.entryStackVariables[0]));
        Assert.That(optimizer.BasicBlocks.Sum(block => block.ops.Count), Is.EqualTo(operationCount));
        Assert.That(optimizer.Blocks.SelectMany(block =>
            block.entryLocals.Concat(block.entryStack).Concat(block.exitLocals).Concat(block.exitStack)), Is.Empty);
        Assert.That(optimizer.BasicBlocks.SelectMany(block => block.ops), Has.All.Matches<Optimizer.Op>(op =>
            op.inputTypes.Count == 0 && op.stackOutputs.Count == 0 && op.variableAccesses.Count == 0 && !op.clearsStack));

        optimizer.InsertBranches();
        optimizer.Emit();
        Assert.That(optimizer.output.instructions, Has.None.Matches<CodeInstruction>(instruction =>
            instruction.IsLdloc() || instruction.IsStloc()));
    }

    [Test]
    public void ConvertStackToVariablesUsesLocalBuilderIdentityAndDeclaredType()
    {
        LocalBuilder? declaredLocal = null;
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            declaredLocal = generator.DeclareLocal(typeof(string));
            return
            [
                new CodeInstruction(OpCodes.Ldstr, "value"),
                new CodeInstruction(OpCodes.Stloc, declaredLocal),
                new CodeInstruction(OpCodes.Ldloc, declaredLocal),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.DeduceTypes();

        optimizer.ConvertStackToVariables();

        Optimizer.Variable local = optimizer.LocalVariables[declaredLocal!.LocalIndex];
        Optimizer.Op store = optimizer.BasicBlocks[0].ops[1];
        Optimizer.Op load = optimizer.BasicBlocks[0].ops[2];
        Assert.That(local.kind, Is.EqualTo(Optimizer.VariableKind.Local));
        Assert.That(local.type, Is.EqualTo(typeof(string)));
        Assert.That(local.localBuilder, Is.SameAs(declaredLocal));
        Assert.That(store.outputs, Is.EqualTo(new[] { local }));
        Assert.That(load.inputs, Is.EqualTo(new[] { local }));
    }

    [Test]
    public void ConvertStackToVariablesDoesNotGuessUnknownNumericLocalType()
    {
        Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Stloc_S, (byte)4),
            new CodeInstruction(OpCodes.Ldloc_S, (byte)4),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        optimizer.DeduceTypes();

        optimizer.ConvertStackToVariables();

        Optimizer.Variable local = optimizer.LocalVariables[4];
        Assert.That(local.type, Is.Null);
        Assert.That(local.localBuilder, Is.Null);
        Assert.That(optimizer.BasicBlocks[0].ops[1].outputs, Is.EqualTo(new[] { local }));
        Assert.That(optimizer.BasicBlocks[0].ops[2].inputs, Is.EqualTo(new[] { local }));
    }

    [Test]
    public void MakeBasicBlocksBundlesPrefixesWithTheirOperation()
    {
        FieldInfo field = typeof(OptimizerPatches).GetField(nameof(OptimizerPatches.PatchCalls))!;
        Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Volatile),
            new CodeInstruction(OpCodes.Ldsfld, field),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();

        Optimizer.Op fieldLoad = optimizer.BasicBlocks[0].ops[0];
        Assert.That(fieldLoad.Opcode, Is.EqualTo(OpCodes.Ldsfld));
        Assert.That(fieldLoad.prefixes.Select(prefix => prefix.Opcode), Is.EqualTo(new[] { OpCodes.Volatile }));

        optimizer.DeduceTypes();
        optimizer.ConvertStackToVariables();
        optimizer.Emit();
        Assert.That(optimizer.output.instructions.Select(instruction => instruction.opcode), Is.EqualTo(new[]
        {
            OpCodes.Volatile,
            OpCodes.Ldsfld,
            OpCodes.Pop,
            OpCodes.Ret,
        }));
    }

    [Test]
    public void ConvertStackToVariablesMakesReturnValueAnExplicitInput()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntResult))!;
        Optimizer optimizer = CreateOptimizer(targetMethod, _ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        optimizer.DeduceTypes();

        optimizer.ConvertStackToVariables();

        Optimizer.Op value = optimizer.BasicBlocks[0].ops[0];
        Optimizer.Op returnOperation = optimizer.BasicBlocks[0].ops[1];
        Assert.That(returnOperation.inputs, Is.EqualTo(new[] { value.outputs.Single() }));
        Assert.That(optimizer.BasicBlocks[0].exitStackVariables, Is.Empty);
    }

    [Test]
    public void ConvertStackToVariablesTracksMutableArgumentIdentity()
    {
        MethodInfo targetMethod = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntArgument))!;
        Optimizer optimizer = CreateOptimizer(targetMethod, _ =>
        [
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Starg_S, (byte)0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        optimizer.DeduceTypes();

        optimizer.ConvertStackToVariables();

        Optimizer.Variable argument = optimizer.ArgumentVariables[0];
        Assert.That(argument.type, Is.EqualTo(typeof(int)));
        Assert.That(optimizer.BasicBlocks[0].ops[1].outputs, Is.EqualTo(new[] { argument }));
        Assert.That(optimizer.BasicBlocks[0].ops[2].inputs, Is.EqualTo(new[] { argument }));
    }

    [Test]
    public void ConvertStackToVariablesUsesSymbolicStackAliasingForDup()
    {
        Optimizer optimizer = CreateOptimizer(_ =>
        [
            new CodeInstruction(OpCodes.Ldstr, "value"),
            new CodeInstruction(OpCodes.Dup),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ret),
        ]);
        optimizer.MakeBasicBlocks();
        optimizer.DeduceTypes();

        optimizer.ConvertStackToVariables();

        Optimizer.Op load = optimizer.BasicBlocks[0].ops[0];
        Optimizer.Op duplicate = optimizer.BasicBlocks[0].ops[1];
        Assert.That(duplicate.inputs, Is.EqualTo(new[] { load.outputs.Single() }));
        Assert.That(duplicate.outputs, Is.EqualTo(new[] { load.outputs.Single(), load.outputs.Single() }));
    }

    [Test]
    public void BranchInversionMakesSinglePredecessorTargetFallThrough()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label shared = generator.DefineLabel();
            Label unique = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brfalse, unique),
                new CodeInstruction(OpCodes.Ret).WithLabels(shared),
                new CodeInstruction(OpCodes.Br, shared).WithLabels(unique),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock condition = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock shared = optimizer.BasicBlocks[1];
        Optimizer.BasicBlock unique = optimizer.BasicBlocks[2];

        optimizer.BranchInversion();

        Assert.That(condition.ops[^1].Opcode, Is.EqualTo(OpCodes.Brtrue_S));
        Assert.That(condition.ops[^1].Operand, Is.SameAs(shared));
        Assert.That(condition.next, Is.SameAs(unique));
    }

    [Test]
    public void InsertBranchesRestoresNonAdjacentFallThroughBranch()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        Optimizer.BasicBlock entry = optimizer.BasicBlocks[0];
        Optimizer.BasicBlock target = optimizer.BasicBlocks[2];

        optimizer.InsertBranches();

        Assert.That(entry.next, Is.Null);
        Assert.That(entry.ops, Has.Count.EqualTo(1));
        Assert.That(entry.ops[0].Opcode, Is.EqualTo(OpCodes.Br_S));
        Assert.That(entry.ops[0].Operand, Is.SameAs(target));
    }

    [Test]
    public void EmitConvertsBlockTargetsBackToLabels()
    {
        Optimizer optimizer = CreateOptimizer(generator =>
        {
            Label target = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Br, target),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ret).WithLabels(target),
            ];
        });
        optimizer.MakeBasicBlocks();
        optimizer.InsertBranches();
        Optimizer.BasicBlock target = optimizer.BasicBlocks[2];

        optimizer.Emit();

        CodeInstruction branch = optimizer.output.instructions[0];
        Assert.That(branch.opcode, Is.EqualTo(OpCodes.Br_S));
        Assert.That(branch.operand, Is.TypeOf<Label>());
        Assert.That(branch.operand, Is.EqualTo(target.label));
        CodeInstruction emittedTarget = optimizer.output.instructions.Single(instruction =>
            instruction.labels.Contains((Label)branch.operand));
        Assert.That(emittedTarget.opcode, Is.EqualTo(OpCodes.Ret));
    }

    private static Optimizer CreateOptimizer(Func<ILGenerator, List<CodeInstruction>> createInstructions)
        => CreateOptimizer(TargetMethod, createInstructions);

    private static Optimizer CreateOptimizer(
        MethodBase targetMethod,
        Func<ILGenerator, List<CodeInstruction>> createInstructions)
    {
        var dynamicMethod = new DynamicMethod("OptimizerPassTest", typeof(void), Type.EmptyTypes);
        ILGenerator generator = dynamicMethod.GetILGenerator();
        return new Optimizer(targetMethod, createInstructions(generator), generator, debug: false);
    }

    private static OpCode[] OpCodesIn(Optimizer.BasicBlock block) => [.. block.ops.Select(op => op.Opcode)];
}
