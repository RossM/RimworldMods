using System.Reflection.Emit;

using Disharmony.Optimizer;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class ControlFlowGraphGeneratorTests
{
    private static readonly MethodInfo VoidMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.ReturnVoid))!;

    private static readonly MethodInfo IntMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.ReturnInt))!;

    private static readonly MethodInfo TwoArgumentMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.Add))!;

    private static readonly MethodInfo VoidTwoArgumentMethod =
        typeof(ControlFlowGraphTargets).GetMethod(nameof(ControlFlowGraphTargets.Consume))!;

    private static readonly MethodInfo InstanceMethod =
        typeof(ControlFlowGraphInstanceTarget).GetMethod(nameof(ControlFlowGraphInstanceTarget.Add))!;

    private static readonly ConstructorInfo Constructor =
        typeof(ControlFlowGraphInstanceTarget).GetConstructor([typeof(int)])!;

    private static readonly FieldInfo InstanceField =
        typeof(ControlFlowGraphInstanceTarget).GetField(nameof(ControlFlowGraphInstanceTarget.Value))!;

    [Test]
    public void Metadata_StaticMethod_CreatesDeclaredArgumentsAndReturnType()
    {
        var generator = CreateGenerator(TwoArgumentMethod, ThrowTerminated());

        Assert.That(generator.Arguments.Keys, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(generator.Arguments[0].Type, Is.EqualTo(typeof(int)));
        Assert.That(generator.Arguments[1].Type, Is.EqualTo(typeof(int)));
        Assert.That(generator.ReturnType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Metadata_InstanceMethod_IncludesThisBeforeDeclaredArguments()
    {
        var generator = CreateGenerator(InstanceMethod, ThrowTerminated());

        Assert.That(generator.Arguments.Keys, Is.EqualTo(new[] { 0, 1 }));
        Assert.That(generator.Arguments[0].Type, Is.EqualTo(typeof(ControlFlowGraphInstanceTarget)));
        Assert.That(generator.Arguments[1].Type, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Locals_MethodBodyLocal_IsCreatedWithoutLocalBuilder()
    {
        MethodInfo method = typeof(ControlFlowGraphTargets)
            .GetMethod(nameof(ControlFlowGraphTargets.MethodWithLocal))!;
        LocalVariableInfo metadataLocal = method.GetMethodBody()!.LocalVariables[0];
        Assert.That(metadataLocal.LocalType, Is.EqualTo(typeof(int)));

        var generator = CreateGenerator(method, ThrowTerminated());

        Assert.That(generator.Locals[metadataLocal.LocalIndex].Type, Is.EqualTo(typeof(int)));
        Assert.That(generator.Locals[metadataLocal.LocalIndex].LocalBuilder, Is.Null);
    }

    [Test]
    public void Locals_LocalBuilderOnly_IsCreatedWithBuilder()
    {
        LocalBuilder builder = PatchProcessor.CreateILGenerator().DeclareLocal(typeof(string));

        var generator = CreateGenerator(
            VoidMethod,
            [new CodeInstruction(OpCodes.Ldloc_S, builder), new CodeInstruction(OpCodes.Pop), .. ThrowTerminated()]);

        Assert.That(generator.Locals[builder.LocalIndex].Type, Is.EqualTo(typeof(string)));
        Assert.That(generator.Locals[builder.LocalIndex].LocalBuilder, Is.SameAs(builder));
    }

    [Test]
    public void Locals_MetadataLocalAndMatchingBuilder_AreMerged()
    {
        MethodInfo method = typeof(ControlFlowGraphTargets)
            .GetMethod(nameof(ControlFlowGraphTargets.MethodWithLocal))!;
        LocalVariableInfo metadataLocal = method.GetMethodBody()!.LocalVariables[0];
        Assert.That(metadataLocal.LocalType, Is.EqualTo(typeof(int)));
        ILGenerator localGenerator = PatchProcessor.CreateILGenerator();
        LocalBuilder builder = null!;
        for (var index = 0; index <= metadataLocal.LocalIndex; index++)
            builder = localGenerator.DeclareLocal(typeof(int));

        var generator = CreateGenerator(
            method,
            [new CodeInstruction(OpCodes.Ldloc_S, builder), new CodeInstruction(OpCodes.Pop), .. ThrowTerminated()]);

        Assert.That(generator.Locals[metadataLocal.LocalIndex].LocalBuilder, Is.SameAs(builder));
    }

    [Test]
    public void Locals_MetadataLocalAndConflictingBuilder_Throws()
    {
        MethodInfo method = typeof(ControlFlowGraphTargets)
            .GetMethod(nameof(ControlFlowGraphTargets.MethodWithLocal))!;
        LocalVariableInfo metadataLocal = method.GetMethodBody()!.LocalVariables[0];
        Assert.That(metadataLocal.LocalType, Is.EqualTo(typeof(int)));
        ILGenerator localGenerator = PatchProcessor.CreateILGenerator();
        LocalBuilder builder = null!;
        for (var index = 0; index <= metadataLocal.LocalIndex; index++)
            builder = localGenerator.DeclareLocal(typeof(string));
        var generator = new ControlFlowGraphGenerator(
            method,
            [new CodeInstruction(OpCodes.Ldloc_S, builder), new CodeInstruction(OpCodes.Pop), .. ThrowTerminated()]);

        Assert.Throws<InvalidOperationException>(() => generator.CreateControlFlowGraph());
    }

    [Test]
    public void StackBehaviourPop_EveryFixedFormCreatesExpectedInputs()
    {
        (StackBehaviour Behaviour, CodeInstruction Instruction, int InputCount)[] cases =
        [
            (StackBehaviour.Pop0, new CodeInstruction(OpCodes.Nop), 0),
            (StackBehaviour.Pop1, new CodeInstruction(OpCodes.Pop), 1),
            (StackBehaviour.Pop1_pop1, new CodeInstruction(OpCodes.Add), 2),
            (StackBehaviour.Popi, new CodeInstruction(OpCodes.Initobj, typeof(int)), 1),
            (StackBehaviour.Popi_pop1, new CodeInstruction(OpCodes.Stobj, typeof(int)), 2),
            (StackBehaviour.Popi_popi, new CodeInstruction(OpCodes.Stind_I4), 2),
            (StackBehaviour.Popi_popi8, new CodeInstruction(OpCodes.Stind_I8), 2),
            (StackBehaviour.Popi_popi_popi, new CodeInstruction(OpCodes.Cpblk), 3),
            (StackBehaviour.Popi_popr4, new CodeInstruction(OpCodes.Stind_R4), 2),
            (StackBehaviour.Popi_popr8, new CodeInstruction(OpCodes.Stind_R8), 2),
            (StackBehaviour.Popref, new CodeInstruction(OpCodes.Castclass, typeof(object)), 1),
            (StackBehaviour.Popref_pop1, new CodeInstruction(OpCodes.Stfld, InstanceField), 2),
            (StackBehaviour.Popref_popi, new CodeInstruction(OpCodes.Ldelem_I4), 2),
            (StackBehaviour.Popref_popi_popi, new CodeInstruction(OpCodes.Stelem_I4), 3),
            (StackBehaviour.Popref_popi_popi8, new CodeInstruction(OpCodes.Stelem_I8), 3),
            (StackBehaviour.Popref_popi_popr4, new CodeInstruction(OpCodes.Stelem_R4), 3),
            (StackBehaviour.Popref_popi_popr8, new CodeInstruction(OpCodes.Stelem_R8), 3),
            (StackBehaviour.Popref_popi_popref, new CodeInstruction(OpCodes.Stelem_Ref), 3),
            (StackBehaviour.Popref_popi_pop1, new CodeInstruction(OpCodes.Stelem, typeof(object)), 3),
        ];
        Assert.That(cases.Select(testCase => testCase.Behaviour).Distinct(), Is.EquivalentTo(
            Enum.GetValues(typeof(StackBehaviour)).Cast<StackBehaviour>()
                .Where(behaviour => behaviour != StackBehaviour.Varpop && behaviour.ToString().StartsWith("Pop"))));

        foreach (var testCase in cases)
        {
            List<CodeInstruction> instructions =
            [.. Enumerable.Repeat(new CodeInstruction(OpCodes.Ldc_I4_0), testCase.InputCount), testCase.Instruction, .. ThrowTerminated()];
            var generator = CreateGenerator(VoidMethod, instructions);
            ILOp operation = GetILOp(generator, testCase.Instruction.opcode);

            Assert.That(operation.Inputs, Has.Count.EqualTo(testCase.InputCount), testCase.Behaviour.ToString());
        }
    }

    [Test]
    public void StackBehaviourPop_VarpopHandlesStaticInstanceAndConstructorOperands()
    {
        (CodeInstruction Instruction, int InputCount)[] cases =
        [
            (new CodeInstruction(OpCodes.Call, TwoArgumentMethod), 2),
            (new CodeInstruction(OpCodes.Callvirt, InstanceMethod), 2),
            (new CodeInstruction(OpCodes.Newobj, Constructor), 1),
        ];

        foreach (var testCase in cases)
        {
            List<CodeInstruction> instructions =
            [.. Enumerable.Repeat(new CodeInstruction(OpCodes.Ldc_I4_0), testCase.InputCount), testCase.Instruction, .. ThrowTerminated()];
            var generator = CreateGenerator(VoidMethod, instructions);
            ILOp operation = GetILOp(generator, testCase.Instruction.opcode);

            Assert.That(operation.Inputs, Has.Count.EqualTo(testCase.InputCount), testCase.Instruction.opcode.Name);
        }
    }

    [Test]
    public void StackBehaviourPush_EveryFixedFormProducesExpectedStackDepth()
    {
        (StackBehaviour Behaviour, CodeInstruction[] Instructions, int StackDepth)[] cases =
        [
            (StackBehaviour.Push0, [new CodeInstruction(OpCodes.Nop)], 0),
            (StackBehaviour.Push1, [new CodeInstruction(OpCodes.Ldarg_0)], 1),
            (StackBehaviour.Pushi, [new CodeInstruction(OpCodes.Ldc_I4_0)], 1),
            (StackBehaviour.Pushi8, [new CodeInstruction(OpCodes.Ldc_I8, 0L)], 1),
            (StackBehaviour.Pushr4, [new CodeInstruction(OpCodes.Ldc_R4, 0f)], 1),
            (StackBehaviour.Pushr8, [new CodeInstruction(OpCodes.Ldc_R8, 0d)], 1),
            (StackBehaviour.Pushref, [new CodeInstruction(OpCodes.Ldnull)], 1),
            (StackBehaviour.Push1_push1,
                [new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Dup)], 2),
        ];

        foreach (var testCase in cases)
        {
            var generator = CreateGenerator(TwoArgumentMethod, BranchToThrowTarget(testCase.Instructions));
            BasicBlock entry = generator.ControlFlowGraph.BasicBlocks.First();

            Assert.That(generator.BlockStacks[entry.Label].OutgoingStack, Has.Count.EqualTo(testCase.StackDepth),
                testCase.Behaviour.ToString());
        }
    }

    [Test]
    public void StackBehaviourPush_VarpushUsesMethodReturnType()
    {
        var valueGenerator = CreateGenerator(
            VoidMethod,
            BranchToThrowTarget([new CodeInstruction(OpCodes.Call, IntMethod)]));
        var voidGenerator = CreateGenerator(
            VoidMethod,
            BranchToThrowTarget(
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call, VoidTwoArgumentMethod),
            ]));

        BasicBlock valueEntry = valueGenerator.ControlFlowGraph.BasicBlocks.First();
        BasicBlock voidEntry = voidGenerator.ControlFlowGraph.BasicBlocks.First();
        Assert.That(valueGenerator.BlockStacks[valueEntry.Label].OutgoingStack, Has.Count.EqualTo(1));
        Assert.That(voidGenerator.BlockStacks[voidEntry.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void Dup_ReusesSameStackValueTwiceWithoutCreatingAnILOp()
    {
        var generator = CreateGenerator(
            VoidMethod,
            BranchToThrowTarget([new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Dup)]));
        BasicBlock entry = generator.ControlFlowGraph.BasicBlocks.First();
        List<StackSlot> outgoing = generator.BlockStacks[entry.Label].OutgoingStack;

        Assert.That(outgoing, Has.Count.EqualTo(2));
        Assert.That(outgoing[0], Is.SameAs(outgoing[1]));
        Assert.That(entry.Ops.SelectMany(Flatten).OfType<ILOp>().Any(operation => operation.IL.OpCode == OpCodes.Dup), Is.False);
    }

    [Test]
    public void Prefixes_AreAttachedInOrderToFollowingInstruction()
    {
        var generator = CreateGenerator(
            VoidMethod,
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Unaligned, (byte)1),
                new CodeInstruction(OpCodes.Volatile),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Pop),
                .. ThrowTerminated(),
            ]);

        ILOp load = GetILOp(generator, OpCodes.Ldind_I4);
        Assert.That(load.IL.Prefixes.Select(prefix => prefix.OpCode),
            Is.EqualTo(new[] { OpCodes.Unaligned, OpCodes.Volatile }));
        Assert.That(load.IL.Prefixes[0].Operand, Is.EqualTo((byte)1));
    }

    [Test]
    public void ControlFlow_Ret_VoidMethodCreatesReturnWithVoidValue()
    {
        var generator = CreateGenerator(VoidMethod, [new CodeInstruction(OpCodes.Ret)]);

        BasicBlock block = generator.ControlFlowGraph.BasicBlocks.Single();
        Assert.That(block.Branch, Is.TypeOf<Return>());
        var branch = (Return)block.Branch;
        Assert.That(branch.IL.OpCode, Is.EqualTo(OpCodes.Ret));
        Assert.That(branch.Value, Is.TypeOf<VoidOp>());
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    [Test]
    public void ControlFlow_Ret_ValueMethodConsumesReturnValue()
    {
        var generator = CreateGenerator(
            IntMethod,
            [new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ret)]);

        BasicBlock block = generator.ControlFlowGraph.BasicBlocks.Single();
        Assert.That(block.Branch, Is.TypeOf<Return>());
        var branch = (Return)block.Branch;
        Assert.That(branch.IL.OpCode, Is.EqualTo(OpCodes.Ret));
        Assert.That(branch.Value, Is.TypeOf<StackSlot>());
        Assert.That(generator.BlockStacks[block.Label].OutgoingStack, Is.Empty);
    }

    private static ControlFlowGraphGenerator CreateGenerator(MethodBase method, List<CodeInstruction> instructions)
    {
        var generator = new ControlFlowGraphGenerator(method, instructions);
        generator.CreateControlFlowGraph();
        return generator;
    }

    private static List<CodeInstruction> ThrowTerminated() =>
        [new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Throw)];

    private static List<CodeInstruction> BranchToThrowTarget(IEnumerable<CodeInstruction> body)
    {
        Label label = PatchProcessor.CreateILGenerator().DefineLabel();
        var target = new CodeInstruction(OpCodes.Ldnull);
        target.labels.Add(label);
        return [.. body, new CodeInstruction(OpCodes.Br, label), target, new CodeInstruction(OpCodes.Throw)];
    }

    private static ILOp GetILOp(ControlFlowGraphGenerator generator, OpCode opcode) =>
        generator.ControlFlowGraph.BasicBlocks
            .SelectMany(block => block.Ops)
            .SelectMany(Flatten)
            .OfType<ILOp>()
            .Single(operation => operation.IL.OpCode == opcode);

    private static IEnumerable<Op> Flatten(Op operation)
    {
        yield return operation;
        switch (operation)
        {
            case AssignmentOp assignment:
                foreach (var nested in Flatten(assignment.Input))
                    yield return nested;
                break;
            case ILOp il:
                foreach (var input in il.Inputs)
                foreach (var nested in Flatten(input))
                    yield return nested;
                break;
        }
    }
}
