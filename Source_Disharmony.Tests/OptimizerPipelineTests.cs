using System.Reflection.Emit;
using Disharmony.Optimizer;
using HarmonyLib;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OptimizerPipelineTests
{
    [Test]
    public void StraightLine_Int32Arithmetic_EmitsCanonicalArgumentLoads()
    {
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int), typeof(int)], _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Ldc_I4_2),
            new CodeInstruction(OpCodes.Mul),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Ldarg, OpCodes.Add, OpCodes.Ldc_I4_2, OpCodes.Mul, OpCodes.Ret);
        Assert.That(output.Take(2).Select(instruction => instruction.operand), Is.EqualTo(new object[] { 0, 1 }));
    }

    [Test]
    public void Conditional_BoolAndUnsignedComparison_PreservesBothResults()
    {
        List<CodeInstruction> output = Optimize(typeof(bool), [typeof(bool), typeof(uint), typeof(uint)], generator =>
        {
            Label falseResult = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brfalse, falseResult),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Clt_Un),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(falseResult),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse, OpCodes.Ldarg, OpCodes.Ldarg,
            OpCodes.Clt_Un, OpCodes.Ret, OpCodes.Ldc_I4_0, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Clt_Un));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ret), Is.EqualTo(2));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_ShortLocalPhi_PreservesBothAssignments()
    {
        List<CodeInstruction> output = Optimize(typeof(short), [typeof(bool), typeof(short), typeof(short)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(short));
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Ldloc, value).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg_1, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldarg_2, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Count(instruction => instruction.IsStloc()), Is.EqualTo(2));
        Assert.That(output.Count(instruction => instruction.IsLdloc()), Is.EqualTo(1));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(short)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Loop_Int32Accumulator_PreservesHeaderPhiAndBackEdge()
    {
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int)], generator =>
        {
            LocalBuilder index = generator.DeclareLocal(typeof(int));
            LocalBuilder sum = generator.DeclareLocal(typeof(int));
            Label condition = generator.DefineLabel();
            Label body = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, index),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, sum),
                new CodeInstruction(OpCodes.Br, condition),
                new CodeInstruction(OpCodes.Ldloc, sum).WithLabels(body),
                new CodeInstruction(OpCodes.Ldloc, index),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, sum),
                new CodeInstruction(OpCodes.Ldloc, index),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, index),
                new CodeInstruction(OpCodes.Ldloc, index).WithLabels(condition),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Blt, body),
                new CodeInstruction(OpCodes.Ldloc, sum),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        // TODO: SSA destruction materializes every phi transfer, while spill allocation permits
        //       only one logical value to reclaim each original local. Liveness-based coalescing of
        //       noninterfering phi sources and destinations should remove most of these copies.
        AssertOpCodes(output,
            OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_0, OpCodes.Stloc, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldloc, OpCodes.Stloc, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldarg, OpCodes.Blt, OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldloc, OpCodes.Ldloc, OpCodes.Add, OpCodes.Ldc_I4_1,
            OpCodes.Stloc, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ldloc,
            OpCodes.Add, OpCodes.Ldloc, OpCodes.Stloc, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Add));
        Assert.That(output.Count(instruction => instruction.opcode.FlowControl == FlowControl.Cond_Branch),
            Is.EqualTo(1));
        Assert.That(output.Any(instruction => instruction.opcode.FlowControl == FlowControl.Branch), Is.True);
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc()), Is.Not.Empty);
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Loop_Int64CounterWithInt32Limit_PreservesWideConversionAndBackEdge()
    {
        List<CodeInstruction> output = Optimize(typeof(long), [typeof(int)], generator =>
        {
            LocalBuilder counter = generator.DeclareLocal(typeof(long));
            Label condition = generator.DefineLabel();
            Label body = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldc_I8, 0L),
                new CodeInstruction(OpCodes.Stloc, counter),
                new CodeInstruction(OpCodes.Br, condition),
                new CodeInstruction(OpCodes.Ldloc, counter).WithLabels(body),
                new CodeInstruction(OpCodes.Ldc_I8, 1L),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, counter),
                new CodeInstruction(OpCodes.Ldloc, counter).WithLabels(condition),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Blt, body),
                new CodeInstruction(OpCodes.Ldloc, counter),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldc_I8, OpCodes.Stloc, OpCodes.Ldarg, OpCodes.Conv_I8, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldloc, OpCodes.Blt, OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldc_I8, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ldloc,
            OpCodes.Add, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_I8));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldc_I8));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(long)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_FloatLocalPhi_PreservesArithmeticAndConversion()
    {
        List<CodeInstruction> output = Optimize(typeof(float), [typeof(bool), typeof(float), typeof(float)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(float));
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldc_R4, 1.5f),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                new CodeInstruction(OpCodes.Ldc_R4, 2.0f),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Ldloc, value).WithLabels(join),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg_1, OpCodes.Ldc_R4, OpCodes.Add, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Conv_R4, OpCodes.Ret,
            OpCodes.Ldarg_2, OpCodes.Ldc_R4, OpCodes.Mul, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Add));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Mul));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_R4));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(float)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Switch_DoubleArithmetic_PreservesAllCasesAndDefault()
    {
        List<CodeInstruction> output = Optimize(typeof(double), [typeof(int), typeof(double)], generator =>
        {
            Label add = generator.DefineLabel();
            Label multiply = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Switch, new[] { add, multiply }),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Neg),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(add),
                new CodeInstruction(OpCodes.Ldc_R8, 0.5),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(multiply),
                new CodeInstruction(OpCodes.Ldc_R8, 2.0),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        // TODO: SSA removes the reloadable argument read, so the forward stack scheduler emits the
        //       literal before discovering that Add/Mul needs the argument below it. Deferred or
        //       rematerializable pure producers should reconstruct the original load order.
        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Switch,
            OpCodes.Ldarg, OpCodes.Neg, OpCodes.Ret,
            OpCodes.Ldc_R8, OpCodes.Stloc, OpCodes.Ldarg, OpCodes.Ldloc, OpCodes.Add, OpCodes.Ret,
            OpCodes.Ldc_R8, OpCodes.Stloc, OpCodes.Ldarg, OpCodes.Ldloc, OpCodes.Mul, OpCodes.Ret);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Switch).operand,
            Is.TypeOf<Label[]>().And.Length.EqualTo(2));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ret), Is.EqualTo(3));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Neg));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Add));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Mul));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void NativeInt_UnsignedConversionsAndArithmetic_PreserveNativeStackType()
    {
        List<CodeInstruction> output = Optimize(typeof(IntPtr), [typeof(uint), typeof(uint)], _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Conv_U),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Conv_U),
            new CodeInstruction(OpCodes.Add),
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Conv_U),
            new CodeInstruction(OpCodes.Xor),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Conv_U,
            OpCodes.Ldarg, OpCodes.Conv_U, OpCodes.Add,
            OpCodes.Ldc_I4_1, OpCodes.Conv_U, OpCodes.Xor, OpCodes.Ret);
        Assert.That(output, Has.None.Matches<CodeInstruction>(instruction =>
            instruction.IsStloc() || instruction.IsLdloc()));
    }

    [Test]
    public void CheckedConversions_UnsignedLongToShort_PreserveOverflowOperations()
    {
        List<CodeInstruction> output = Optimize(typeof(short), [typeof(ulong), typeof(bool)], generator =>
        {
            Label signed = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Brfalse, signed),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Conv_Ovf_I2_Un),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(signed),
                new CodeInstruction(OpCodes.Conv_Ovf_I2),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_1, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Conv_Ovf_I2_Un, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Conv_Ovf_I2, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_Ovf_I2_Un));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_Ovf_I2));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ret), Is.EqualTo(2));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Loop_UnsignedDivisionAndRemainder_PreserveUnsignedOperations()
    {
        List<CodeInstruction> output = Optimize(typeof(uint), [typeof(uint), typeof(uint)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(uint));
            Label condition = generator.DefineLabel();
            Label body = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Br, condition),
                new CodeInstruction(OpCodes.Ldloc, value).WithLabels(body),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Div_Un),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Ldloc, value).WithLabels(condition),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Rem_Un),
                new CodeInstruction(OpCodes.Brtrue, body),
                new CodeInstruction(OpCodes.Ldloc, value),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldarg, OpCodes.Rem_Un, OpCodes.Brtrue,
            OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldloc, OpCodes.Ldarg, OpCodes.Div_Un, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Div_Un));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Rem_Un));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_DoubleToIntAndLongConversions_PreserveStackWidths()
    {
        List<CodeInstruction> output = Optimize(typeof(long), [typeof(double), typeof(bool)], generator =>
        {
            Label wide = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Brtrue, wide),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Conv_I4),
                new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(wide),
                new CodeInstruction(OpCodes.Conv_I8),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_1, OpCodes.Brtrue,
            OpCodes.Ldarg, OpCodes.Conv_I4, OpCodes.Conv_I8, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Conv_I8, OpCodes.Ret);
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Conv_I8), Is.EqualTo(2));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Conv_I4), Is.EqualTo(1));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Void_ConstantBranchAndNops_RemoveNoiseAndPreserveBranchStructure()
    {
        List<CodeInstruction> output = Optimize(typeof(void), Type.EmptyTypes, generator =>
        {
            Label exit = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Brtrue, exit),
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldnull).WithLabels(exit),
                new CodeInstruction(OpCodes.Throw),
            ];
        });

        // TODO: SCCP or a dedicated constant-branch fold should remove the untaken edge and expose
        //       its block to dead-code elimination. BranchElimination only handles branches whose
        //       explicit and fallthrough edges already have the same target.
        AssertOpCodes(output, OpCodes.Ldc_I4_0, OpCodes.Brtrue, OpCodes.Ret, OpCodes.Ldnull, OpCodes.Throw);
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_ClassLocalPhi_PreservesReferenceJoinAndFieldRead()
    {
        FieldInfo number = typeof(OptimizerDataObject).GetField(nameof(OptimizerDataObject.Number))!;
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(bool), typeof(OptimizerDataObject), typeof(OptimizerDataObject)],
            generator =>
            {
                LocalBuilder selected = generator.DeclareLocal(typeof(OptimizerDataObject));
                Label alternative = generator.DefineLabel();
                Label join = generator.DefineLabel();
                return
                [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Brfalse, alternative),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Br, join),
                    new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Ldloc, selected).WithLabels(join),
                    new CodeInstruction(OpCodes.Ldfld, number),
                    new CodeInstruction(OpCodes.Ret),
                ];
            });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ldfld, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType),
            Is.All.EqualTo(typeof(OptimizerDataObject)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_StructLocalPhi_PreservesValueJoinAndFieldRead()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(bool), typeof(OptimizerDataStruct), typeof(OptimizerDataStruct)],
            generator =>
            {
                LocalBuilder selected = generator.DeclareLocal(typeof(OptimizerDataStruct));
                Label alternative = generator.DefineLabel();
                Label join = generator.DefineLabel();
                return
                [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Brfalse, alternative),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Br, join),
                    new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Ldloc, selected).WithLabels(join),
                    new CodeInstruction(OpCodes.Ldfld, number),
                    new CodeInstruction(OpCodes.Ret),
                ];
            });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ldfld, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType),
            Is.All.EqualTo(typeof(OptimizerDataStruct)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_BoxedPrimitivePhi_PreservesBothBoxTypes()
    {
        List<CodeInstruction> output = Optimize(typeof(object), [typeof(bool), typeof(int), typeof(long)], generator =>
        {
            LocalBuilder boxed = generator.DeclareLocal(typeof(object));
            Label wide = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brtrue, wide),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Box, typeof(int)),
                new CodeInstruction(OpCodes.Stloc, boxed),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_2).WithLabels(wide),
                new CodeInstruction(OpCodes.Box, typeof(long)),
                new CodeInstruction(OpCodes.Stloc, boxed),
                new CodeInstruction(OpCodes.Ldloc, boxed).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brtrue,
            OpCodes.Ldarg, OpCodes.Box, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Box, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Where(instruction => instruction.opcode == OpCodes.Box)
            .Select(instruction => instruction.operand), Is.EqualTo(new object[] { typeof(int), typeof(long) }));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(object)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_BoxedStructPhi_PreservesUnboxAndFieldRead()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(bool), typeof(OptimizerDataStruct), typeof(OptimizerDataStruct)],
            generator =>
            {
                LocalBuilder boxed = generator.DeclareLocal(typeof(object));
                Label alternative = generator.DefineLabel();
                Label join = generator.DefineLabel();
                return
                [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Brfalse, alternative),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Box, typeof(OptimizerDataStruct)),
                    new CodeInstruction(OpCodes.Stloc, boxed),
                    new CodeInstruction(OpCodes.Br, join),
                    new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                    new CodeInstruction(OpCodes.Box, typeof(OptimizerDataStruct)),
                    new CodeInstruction(OpCodes.Stloc, boxed),
                    new CodeInstruction(OpCodes.Ldloc, boxed).WithLabels(join),
                    new CodeInstruction(OpCodes.Unbox_Any, typeof(OptimizerDataStruct)),
                    new CodeInstruction(OpCodes.Ldfld, number),
                    new CodeInstruction(OpCodes.Ret),
                ];
            });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Box, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Unbox_Any, OpCodes.Ldfld, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Box, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Box), Is.EqualTo(2));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Unbox_Any).operand,
            Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void PrimitiveByRef_ConditionalWriteThenRead_PreservesIndirectAccesses()
    {
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int).MakeByRefType(), typeof(bool)], generator =>
        {
            LocalBuilder original = generator.DeclareLocal(typeof(int));
            Label read = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Stloc, original),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Brfalse, read),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloc, original),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_I4),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(read),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Ldind_I4, OpCodes.Stloc,
            OpCodes.Ldarg_1, OpCodes.Brfalse,
            OpCodes.Ldc_I4_1, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldloc, OpCodes.Add, OpCodes.Stloc,
            OpCodes.Ldarg, OpCodes.Ldloc, OpCodes.Stind_I4,
            OpCodes.Ldarg, OpCodes.Ldind_I4, OpCodes.Ret);
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ldind_I4), Is.EqualTo(2));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Stind_I4), Is.EqualTo(1));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Add));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void StructByRef_FieldWriteThenRead_PreservesManagedPointerOperations()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(OptimizerDataStruct).MakeByRefType(), typeof(int)],
            _ =>
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Stfld, number),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, number),
                new CodeInstruction(OpCodes.Ret),
            ]);

        AssertOpCodes(output, OpCodes.Ldarg, OpCodes.Ldarg, OpCodes.Stfld, OpCodes.Ldarg, OpCodes.Ldfld, OpCodes.Ret);
        Assert.That(output[2].operand, Is.SameAs(number));
        Assert.That(output[4].operand, Is.SameAs(number));
    }

    [Test]
    public void Conditional_StructByRefPhi_PreservesByRefLocalAndFieldRead()
    {
        Type structReference = typeof(OptimizerDataStruct).MakeByRefType();
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(bool), structReference, structReference], generator =>
        {
            LocalBuilder selected = generator.DeclareLocal(structReference);
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Stloc, selected),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                new CodeInstruction(OpCodes.Stloc, selected),
                new CodeInstruction(OpCodes.Ldloc, selected).WithLabels(join),
                new CodeInstruction(OpCodes.Ldfld, number),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ldfld, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(structReference));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void ArrayElementByRef_ConditionalWrite_PreservesElementAddressAndIndirectRead()
    {
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(int[]), typeof(int), typeof(int), typeof(bool)],
            generator =>
            {
                Label read = generator.DefineLabel();
                return
                [
                    new CodeInstruction(OpCodes.Ldarg_3),
                    new CodeInstruction(OpCodes.Brfalse, read),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(OpCodes.Stelem_I4),
                    new CodeInstruction(OpCodes.Ldarg_0).WithLabels(read),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Ldelema, typeof(int)),
                    new CodeInstruction(OpCodes.Ldind_I4),
                    new CodeInstruction(OpCodes.Ret),
                ];
            });

        AssertOpCodes(output,
            OpCodes.Ldarg_3, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Ldarg, OpCodes.Ldarg, OpCodes.Stelem_I4,
            OpCodes.Ldarg, OpCodes.Ldarg, OpCodes.Ldelema, OpCodes.Ldind_I4, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Stelem_I4));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldelema).operand, Is.EqualTo(typeof(int)));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldind_I4));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void ConstrainedStructVirtualCall_PreservesPrefixAndManagedAddress()
    {
        MethodInfo toString = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes)!;
        List<CodeInstruction> output = Optimize(typeof(string), [typeof(OptimizerDataStruct)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(OptimizerDataStruct));
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Ldloca, value),
                new CodeInstruction(OpCodes.Constrained, typeof(OptimizerDataStruct)),
                new CodeInstruction(OpCodes.Callvirt, toString),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloca,
            OpCodes.Constrained, OpCodes.Callvirt, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldloca));
        int constrainedIndex = output.FindIndex(instruction => instruction.opcode == OpCodes.Constrained);
        Assert.That(constrainedIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(output[constrainedIndex].operand, Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output[constrainedIndex + 1].opcode, Is.EqualTo(OpCodes.Callvirt));
        Assert.That(output[constrainedIndex + 1].operand, Is.SameAs(toString));
    }

    [Test]
    public void Conditional_InterfacePhi_PreservesVirtualDispatch()
    {
        MethodInfo read = typeof(IOptimizerDataReader).GetMethod(nameof(IOptimizerDataReader.Read))!;
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(bool), typeof(IOptimizerDataReader), typeof(IOptimizerDataReader)],
            generator =>
            {
                LocalBuilder selected = generator.DeclareLocal(typeof(IOptimizerDataReader));
                Label alternative = generator.DefineLabel();
                Label join = generator.DefineLabel();
                return
                [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Brfalse, alternative),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Br, join),
                    new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Ldloc, selected).WithLabels(join),
                    new CodeInstruction(OpCodes.Callvirt, read),
                    new CodeInstruction(OpCodes.Ret),
                ];
            });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Callvirt, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Callvirt).operand, Is.SameAs(read));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType),
            Is.All.EqualTo(typeof(IOptimizerDataReader)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void TryCatch_Int32Division_PreservesFallbackAndExceptionRegions()
    {
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int), typeof(int)], generator =>
        {
            LocalBuilder result = generator.DeclareLocal(typeof(int));
            Label exit = generator.DefineLabel();
            var tryStart = new CodeInstruction(OpCodes.Ldarg_0);
            tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var catchStart = new CodeInstruction(OpCodes.Pop);
            catchStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(DivideByZeroException)));
            var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
            catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryStart,
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Div),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Leave, exit),
                catchStart,
                new CodeInstruction(OpCodes.Ldc_I4_M1),
                new CodeInstruction(OpCodes.Stloc, result),
                catchLeave,
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(exit),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Div, OpCodes.Stloc, OpCodes.Leave,
            OpCodes.Pop, OpCodes.Ldc_I4_M1, OpCodes.Stloc, OpCodes.Leave,
            OpCodes.Ldloc, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Div));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldc_I4_M1));
        AssertExceptionMarkers(output,
            ExceptionBlockType.BeginExceptionBlock,
            ExceptionBlockType.BeginCatchBlock,
            ExceptionBlockType.EndExceptionBlock);
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void TryFinally_PrimitiveByRefMutation_PreservesBothWritesAndRegions()
    {
        List<CodeInstruction> output = Optimize(typeof(void), [typeof(int).MakeByRefType()], generator =>
        {
            Label exit = generator.DefineLabel();
            var tryStart = new CodeInstruction(OpCodes.Ldarg_0);
            tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var finallyStart = new CodeInstruction(OpCodes.Ldarg_0);
            finallyStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
            var endFinally = new CodeInstruction(OpCodes.Endfinally);
            endFinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryStart,
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_I4),
                new CodeInstruction(OpCodes.Leave, exit),
                finallyStart,
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldind_I4),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stind_I4),
                endFinally,
                new CodeInstruction(OpCodes.Ret).WithLabels(exit),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Dup, OpCodes.Ldind_I4,
            OpCodes.Ldc_I4_1, OpCodes.Add, OpCodes.Stind_I4, OpCodes.Leave,
            OpCodes.Ldarg_0, OpCodes.Dup, OpCodes.Ldind_I4,
            OpCodes.Ldc_I4_2, OpCodes.Add, OpCodes.Stind_I4, OpCodes.Endfinally, OpCodes.Ret);
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Stind_I4), Is.EqualTo(2));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ldind_I4), Is.EqualTo(2));
        AssertExceptionMarkers(output,
            ExceptionBlockType.BeginExceptionBlock,
            ExceptionBlockType.BeginFinallyBlock,
            ExceptionBlockType.EndExceptionBlock);
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void TryCatch_BoxedStructUnbox_PreservesTypeOperationAndFallback()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(object)], generator =>
        {
            LocalBuilder result = generator.DeclareLocal(typeof(int));
            Label exit = generator.DefineLabel();
            var tryStart = new CodeInstruction(OpCodes.Ldarg_0);
            tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var catchStart = new CodeInstruction(OpCodes.Pop);
            catchStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(InvalidCastException)));
            var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
            catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryStart,
                new CodeInstruction(OpCodes.Unbox_Any, typeof(OptimizerDataStruct)),
                new CodeInstruction(OpCodes.Ldfld, number),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Leave, exit),
                catchStart,
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, result),
                catchLeave,
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(exit),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Unbox_Any, OpCodes.Ldfld, OpCodes.Stloc, OpCodes.Leave,
            OpCodes.Pop, OpCodes.Ldc_I4_0, OpCodes.Stloc, OpCodes.Leave,
            OpCodes.Ldloc, OpCodes.Ret);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Unbox_Any).operand,
            Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
        AssertExceptionMarkers(output,
            ExceptionBlockType.BeginExceptionBlock,
            ExceptionBlockType.BeginCatchBlock,
            ExceptionBlockType.EndExceptionBlock);
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void StructInitAndBox_VoidPath_PreservesAddressAndValueOperations()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        MethodInfo keepAlive = typeof(GC).GetMethod(nameof(GC.KeepAlive), [typeof(object)])!;
        List<CodeInstruction> output = Optimize(typeof(void), [typeof(int)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(OptimizerDataStruct));
            return
            [
                new CodeInstruction(OpCodes.Ldloca, value),
                new CodeInstruction(OpCodes.Initobj, typeof(OptimizerDataStruct)),
                new CodeInstruction(OpCodes.Ldloca, value),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Stfld, number),
                new CodeInstruction(OpCodes.Ldloc, value),
                new CodeInstruction(OpCodes.Box, typeof(OptimizerDataStruct)),
                new CodeInstruction(OpCodes.Call, keepAlive),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldloca, OpCodes.Initobj,
            OpCodes.Ldloca, OpCodes.Ldarg, OpCodes.Stfld,
            OpCodes.Ldloc, OpCodes.Box, OpCodes.Call, OpCodes.Ret);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Initobj).operand,
            Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldloca));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Box).operand,
            Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Call).operand, Is.SameAs(keepAlive));
    }

    [Test]
    public void Loop_FloatDecay_PreservesComparisonArithmeticAndBackEdge()
    {
        List<CodeInstruction> output = Optimize(typeof(float), [typeof(float)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(float));
            Label condition = generator.DefineLabel();
            Label body = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Br, condition),
                new CodeInstruction(OpCodes.Ldloc, value).WithLabels(body),
                new CodeInstruction(OpCodes.Ldc_R4, 0.25f),
                new CodeInstruction(OpCodes.Sub),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Ldloc, value).WithLabels(condition),
                new CodeInstruction(OpCodes.Ldc_R4, 0.0f),
                new CodeInstruction(OpCodes.Cgt),
                new CodeInstruction(OpCodes.Brtrue, body),
                new CodeInstruction(OpCodes.Ldloc, value),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldc_R4, OpCodes.Cgt, OpCodes.Brtrue,
            OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldloc, OpCodes.Ldc_R4, OpCodes.Sub, OpCodes.Conv_R4, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Sub));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_R4));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Cgt));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(float)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_BoolLocalPhi_PreservesLogicalOperations()
    {
        List<CodeInstruction> output = Optimize(typeof(bool), [typeof(bool), typeof(bool), typeof(bool)], generator =>
        {
            LocalBuilder result = generator.DeclareLocal(typeof(bool));
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_1).WithLabels(alternative),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Xor),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.And, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Xor, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.And));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Xor));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(bool)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void Conditional_NativeIntPhi_PreservesUnsignedCheckedConversions()
    {
        List<CodeInstruction> output = Optimize(typeof(IntPtr), [typeof(bool), typeof(uint), typeof(ulong)], generator =>
        {
            LocalBuilder result = generator.DeclareLocal(typeof(IntPtr));
            Label wide = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brtrue, wide),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Conv_Ovf_U_Un),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_2).WithLabels(wide),
                new CodeInstruction(OpCodes.Conv_Ovf_U_Un),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(join),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brtrue,
            OpCodes.Ldarg, OpCodes.Conv_Ovf_U_Un, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Conv_Ovf_U_Un, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Conv_Ovf_U_Un), Is.EqualTo(2));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(IntPtr)));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void BoxedPrimitive_TypeTestAndUnbox_PreserveStackJoin()
    {
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(object)], generator =>
        {
            Label matched = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Isinst, typeof(int)),
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Brtrue, matched),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Unbox_Any, typeof(int)).WithLabels(matched),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Isinst, OpCodes.Dup, OpCodes.Brtrue,
            OpCodes.Pop, OpCodes.Ldc_I4_0, OpCodes.Ret,
            OpCodes.Unbox_Any, OpCodes.Ret);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Isinst).operand, Is.EqualTo(typeof(int)));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Dup));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Unbox_Any).operand, Is.EqualTo(typeof(int)));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ret), Is.EqualTo(2));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void ClassCast_NullCheckAndFieldRead_PreserveReferenceOperations()
    {
        FieldInfo number = typeof(OptimizerDataObject).GetField(nameof(OptimizerDataObject.Number))!;
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(object)], generator =>
        {
            LocalBuilder value = generator.DeclareLocal(typeof(OptimizerDataObject));
            Label nullResult = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Castclass, typeof(OptimizerDataObject)),
                new CodeInstruction(OpCodes.Stloc, value),
                new CodeInstruction(OpCodes.Ldloc, value),
                new CodeInstruction(OpCodes.Brfalse, nullResult),
                new CodeInstruction(OpCodes.Ldloc, value),
                new CodeInstruction(OpCodes.Ldfld, number),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(nullResult),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Castclass, OpCodes.Dup, OpCodes.Stloc, OpCodes.Brfalse,
            OpCodes.Ldloc, OpCodes.Ldfld, OpCodes.Ret, OpCodes.Ldc_I4_0, OpCodes.Ret);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Castclass).operand,
            Is.EqualTo(typeof(OptimizerDataObject)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Ret), Is.EqualTo(2));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void StructCopy_LdobjAndStobj_PreserveManagedReferenceTypes()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        Type structReference = typeof(OptimizerDataStruct).MakeByRefType();
        List<CodeInstruction> output = Optimize(typeof(int), [structReference, structReference], _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldobj, typeof(OptimizerDataStruct)),
            new CodeInstruction(OpCodes.Stobj, typeof(OptimizerDataStruct)),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldfld, number),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Ldobj, OpCodes.Stloc,
            OpCodes.Ldarg, OpCodes.Ldloc, OpCodes.Stobj,
            OpCodes.Ldarg, OpCodes.Ldfld, OpCodes.Ret);
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldobj).operand,
            Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Stobj).operand,
            Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Ldfld).operand, Is.SameAs(number));
    }

    [Test]
    public void Conditional_ShortByRefPhi_PreservesSignedIndirectRead()
    {
        Type shortReference = typeof(short).MakeByRefType();
        List<CodeInstruction> output = Optimize(typeof(short), [typeof(bool), shortReference, shortReference], generator =>
        {
            LocalBuilder selected = generator.DeclareLocal(shortReference);
            Label alternative = generator.DefineLabel();
            Label join = generator.DefineLabel();
            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Stloc, selected),
                new CodeInstruction(OpCodes.Br, join),
                new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                new CodeInstruction(OpCodes.Stloc, selected),
                new CodeInstruction(OpCodes.Ldloc, selected).WithLabels(join),
                new CodeInstruction(OpCodes.Ldind_I2),
                new CodeInstruction(OpCodes.Conv_I2),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Ldind_I2, OpCodes.Conv_I2, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldind_I2));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_I2));
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(shortReference));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void TryFinally_DoubleLocal_PreservesArithmeticBoxingAndRegions()
    {
        MethodInfo keepAlive = typeof(GC).GetMethod(nameof(GC.KeepAlive), [typeof(object)])!;
        List<CodeInstruction> output = Optimize(typeof(double), [typeof(double), typeof(bool)], generator =>
        {
            LocalBuilder result = generator.DeclareLocal(typeof(double));
            Label alternative = generator.DefineLabel();
            Label stored = generator.DefineLabel();
            Label exit = generator.DefineLabel();
            var tryStart = new CodeInstruction(OpCodes.Ldarg_1);
            tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var finallyStart = new CodeInstruction(OpCodes.Ldloc, result);
            finallyStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
            var endFinally = new CodeInstruction(OpCodes.Endfinally);
            endFinally.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryStart,
                new CodeInstruction(OpCodes.Brfalse, alternative),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldc_R8, 2.0),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Br, stored),
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(alternative),
                new CodeInstruction(OpCodes.Ldc_R8, 0.5),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, result).WithLabels(stored),
                new CodeInstruction(OpCodes.Leave, exit),
                finallyStart,
                new CodeInstruction(OpCodes.Box, typeof(double)),
                new CodeInstruction(OpCodes.Call, keepAlive),
                endFinally,
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(exit),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldarg_1, OpCodes.Brfalse,
            OpCodes.Ldarg_0, OpCodes.Ldc_R8, OpCodes.Mul, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Stloc, OpCodes.Leave,
            OpCodes.Ldarg_0, OpCodes.Ldc_R8, OpCodes.Add, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Br_S,
            OpCodes.Ldloc, OpCodes.Box, OpCodes.Call, OpCodes.Endfinally,
            OpCodes.Ldloc, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Mul));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Add));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Box).operand, Is.EqualTo(typeof(double)));
        Assert.That(output.Single(instruction => instruction.opcode == OpCodes.Call).operand, Is.SameAs(keepAlive));
        AssertExceptionMarkers(output,
            ExceptionBlockType.BeginExceptionBlock,
            ExceptionBlockType.BeginFinallyBlock,
            ExceptionBlockType.EndExceptionBlock);
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void NewObject_InterfaceDispatch_PreservesConstructorAndVirtualCall()
    {
        ConstructorInfo constructor = typeof(OptimizerDataReader).GetConstructor([typeof(int)])!;
        MethodInfo read = typeof(IOptimizerDataReader).GetMethod(nameof(IOptimizerDataReader.Read))!;
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int)], _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Newobj, constructor),
            new CodeInstruction(OpCodes.Callvirt, read),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output, OpCodes.Ldarg, OpCodes.Newobj, OpCodes.Callvirt, OpCodes.Ret);
        Assert.That(output[1].operand, Is.SameAs(constructor));
        Assert.That(output[2].operand, Is.SameAs(read));
    }

    [Test]
    public void TypeToken_StaticAndVirtualCalls_PreserveRuntimeHandleFlow()
    {
        MethodInfo getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle),
            [typeof(RuntimeTypeHandle)])!;
        MethodInfo getName = typeof(MemberInfo).GetProperty(nameof(MemberInfo.Name))!.GetMethod!;
        List<CodeInstruction> output = Optimize(typeof(string), Type.EmptyTypes, _ =>
        [
            new CodeInstruction(OpCodes.Ldtoken, typeof(OptimizerDataStruct)),
            new CodeInstruction(OpCodes.Call, getTypeFromHandle),
            new CodeInstruction(OpCodes.Callvirt, getName),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output, OpCodes.Ldtoken, OpCodes.Call, OpCodes.Callvirt, OpCodes.Ret);
        Assert.That(output[0].operand, Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output[1].operand, Is.SameAs(getTypeFromHandle));
        Assert.That(output[2].operand, Is.SameAs(getName));
    }

    [Test]
    public void ReadonlyStructArrayElement_PreservesPrefixAddressAndFieldRead()
    {
        FieldInfo number = typeof(OptimizerDataStruct).GetField(nameof(OptimizerDataStruct.Number))!;
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(OptimizerDataStruct[]), typeof(int)], _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Readonly),
            new CodeInstruction(OpCodes.Ldelema, typeof(OptimizerDataStruct)),
            new CodeInstruction(OpCodes.Ldfld, number),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Ldarg, OpCodes.Readonly, OpCodes.Ldelema, OpCodes.Ldfld, OpCodes.Ret);
        int readonlyIndex = output.FindIndex(instruction => instruction.opcode == OpCodes.Readonly);
        Assert.That(readonlyIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(output[readonlyIndex + 1].opcode, Is.EqualTo(OpCodes.Ldelema));
        Assert.That(output[readonlyIndex + 1].operand, Is.EqualTo(typeof(OptimizerDataStruct)));
        Assert.That(output[readonlyIndex + 2].opcode, Is.EqualTo(OpCodes.Ldfld));
        Assert.That(output[readonlyIndex + 2].operand, Is.SameAs(number));
    }

    [Test]
    public void VolatileStaticField_WriteThenRead_PreservesBothPrefixes()
    {
        FieldInfo field = typeof(OptimizerPatches).GetField(nameof(OptimizerPatches.PatchCalls))!;
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int)], _ =>
        [
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Volatile),
            new CodeInstruction(OpCodes.Stsfld, field),
            new CodeInstruction(OpCodes.Volatile),
            new CodeInstruction(OpCodes.Ldsfld, field),
            new CodeInstruction(OpCodes.Ret),
        ]);

        AssertOpCodes(output,
            OpCodes.Ldarg, OpCodes.Volatile, OpCodes.Stsfld, OpCodes.Volatile, OpCodes.Ldsfld, OpCodes.Ret);
        Assert.That(output[2].operand, Is.SameAs(field));
        Assert.That(output[4].operand, Is.SameAs(field));
    }

    [Test]
    public void Conditional_EnumLocalPhi_PreservesUnderlyingConversion()
    {
        List<CodeInstruction> output = Optimize(
            typeof(int),
            [typeof(bool), typeof(DayOfWeek), typeof(DayOfWeek)],
            generator =>
            {
                LocalBuilder selected = generator.DeclareLocal(typeof(DayOfWeek));
                Label alternative = generator.DefineLabel();
                Label join = generator.DefineLabel();
                return
                [
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Brfalse, alternative),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Br, join),
                    new CodeInstruction(OpCodes.Ldarg_2).WithLabels(alternative),
                    new CodeInstruction(OpCodes.Stloc, selected),
                    new CodeInstruction(OpCodes.Ldloc, selected).WithLabels(join),
                    new CodeInstruction(OpCodes.Conv_I4),
                    new CodeInstruction(OpCodes.Ret),
                ];
            });

        AssertOpCodes(output,
            OpCodes.Ldarg_0, OpCodes.Brfalse,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Ldloc, OpCodes.Conv_I4, OpCodes.Ret,
            OpCodes.Ldarg, OpCodes.Stloc, OpCodes.Br_S);
        Assert.That(output.Where(instruction => instruction.IsStloc() || instruction.IsLdloc())
            .Select(instruction => ((LocalBuilder)instruction.operand).LocalType), Is.All.EqualTo(typeof(DayOfWeek)));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Conv_I4));
        AssertAllBranchTargetsAreEmitted(output);
    }

    [Test]
    public void TryCatch_WithInnerLoop_PreservesBackEdgePhiAndHandlerFallback()
    {
        List<CodeInstruction> output = Optimize(typeof(int), [typeof(int), typeof(int)], generator =>
        {
            LocalBuilder index = generator.DeclareLocal(typeof(int));
            LocalBuilder result = generator.DeclareLocal(typeof(int));
            Label condition = generator.DefineLabel();
            Label body = generator.DefineLabel();
            Label exit = generator.DefineLabel();
            var tryStart = new CodeInstruction(OpCodes.Ldc_I4_0);
            tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var catchStart = new CodeInstruction(OpCodes.Pop);
            catchStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArithmeticException)));
            var catchLeave = new CodeInstruction(OpCodes.Leave, exit);
            catchLeave.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            return
            [
                tryStart,
                new CodeInstruction(OpCodes.Stloc, index),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Br, condition),
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(body),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Div),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, result),
                new CodeInstruction(OpCodes.Ldloc, index),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stloc, index),
                new CodeInstruction(OpCodes.Ldloc, index).WithLabels(condition),
                new CodeInstruction(OpCodes.Ldc_I4_3),
                new CodeInstruction(OpCodes.Blt, body),
                new CodeInstruction(OpCodes.Leave, exit),
                catchStart,
                new CodeInstruction(OpCodes.Ldc_I4_M1),
                new CodeInstruction(OpCodes.Stloc, result),
                catchLeave,
                new CodeInstruction(OpCodes.Ldloc, result).WithLabels(exit),
                new CodeInstruction(OpCodes.Ret),
            ];
        });

        AssertOpCodes(output,
            OpCodes.Ldc_I4_0, OpCodes.Stloc, OpCodes.Ldc_I4_0, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldc_I4_3, OpCodes.Blt, OpCodes.Leave,
            OpCodes.Ldloc, OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Div, OpCodes.Add, OpCodes.Stloc,
            OpCodes.Ldloc, OpCodes.Ldc_I4_1, OpCodes.Add, OpCodes.Stloc, OpCodes.Br_S,
            OpCodes.Pop, OpCodes.Ldc_I4_M1, OpCodes.Stloc, OpCodes.Leave,
            OpCodes.Ldloc, OpCodes.Ret);
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Div));
        Assert.That(output.Count(instruction => instruction.opcode == OpCodes.Add), Is.GreaterThanOrEqualTo(2));
        Assert.That(output.Select(instruction => instruction.opcode), Does.Contain(OpCodes.Ldc_I4_M1));
        AssertExceptionMarkers(output,
            ExceptionBlockType.BeginExceptionBlock,
            ExceptionBlockType.BeginCatchBlock,
            ExceptionBlockType.EndExceptionBlock);
        AssertAllBranchTargetsAreEmitted(output);
    }

    private static List<CodeInstruction> Optimize(
        Type returnType,
        Type[] parameterTypes,
        Func<ILGenerator, List<CodeInstruction>> createInstructions)
    {
        var target = new DynamicMethod("OptimizerPipelineTarget", returnType, parameterTypes);
        ILGenerator generator = target.GetILGenerator();
        var optimizer = new Optimizer.Optimizer(target, createInstructions(generator), generator, debug: false);
        return optimizer.Optimize();
    }

    private static void AssertOpCodes(List<CodeInstruction> output, params OpCode[] expected)
    {
        OpCode[] actual = [.. output.Select(instruction => instruction.opcode)];
        Assert.That(actual, Is.EqualTo(expected), string.Join(", ", actual.Select(opcode => opcode.Name)));
    }

    private static void AssertAllBranchTargetsAreEmitted(List<CodeInstruction> output)
    {
        HashSet<Label> emittedLabels = [.. output.SelectMany(instruction => instruction.labels)];
        IEnumerable<Label> targets = output.SelectMany(instruction => instruction.operand switch
        {
            Label label => [label],
            Label[] labels => labels,
            _ => [],
        });
        Assert.That(targets, Is.All.Matches<Label>(emittedLabels.Contains));
    }

    private static void AssertExceptionMarkers(
        List<CodeInstruction> output,
        params ExceptionBlockType[] expected)
    {
        Assert.That(output.SelectMany(instruction => instruction.blocks).Select(block => block.blockType),
            Is.EqualTo(expected));
    }
}
