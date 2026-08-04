using System.Reflection.Emit;
using Disharmony.Optimizer;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OptimizerInfrastructureTests
{
    private enum ByteEnum : byte { Zero }

    [TestCaseSource(nameof(OperationEffectCases))]
    public void OperationEffectsClassifyRepresentativeCilOperations(
        OpCode opcode,
        int expectedValue,
        bool canDiscardIfUnused)
    {
        var operation = new Op(opcode);
        var expected = (OperationEffects)expectedValue;

        Assert.That(operation.Effects, Is.EqualTo(expected));
        Assert.That(operation.CanDiscardIfUnused, Is.EqualTo(canDiscardIfUnused));
    }

    [Test]
    public void OperationEffectsIncludeBundledVolatilePrefixWithoutCaching()
    {
        var operation = new Op(OpCodes.Ldfld, null, []);
        OperationEffects ordinaryEffects =
            OperationEffects.ReadsMemory | OperationEffects.MayThrow;
        Assert.That(operation.Effects, Is.EqualTo(ordinaryEffects));

        operation = new Op(OpCodes.Ldfld, null, [OpCodes.Volatile]);

        Assert.That(operation.Effects, Is.EqualTo(ordinaryEffects | OperationEffects.Volatile));
        Assert.That(operation.CanDiscardIfUnused, Is.False);
    }

    [Test]
    public void UnknownOperationEffectsAreConservativeAndExplicit()
    {
        var operation = new Op(OpCodes.Prefix1);

        Assert.That(operation.Effects, Is.EqualTo(
            OperationEffects.ReadsMemory |
            OperationEffects.WritesMemory |
            OperationEffects.MayThrow |
            OperationEffects.Unknown));
        Assert.That(operation.CanDiscardIfUnused, Is.False);
    }

    [TestCaseSource(nameof(IndirectAccessCases))]
    public void OpClassifiesIndirectValueAccessOpcodes(OpCode opcode, int expectedValue)
    {
        var operation = new Op(opcode);
        Op.VariableAccessKind? expected = expectedValue < 0
            ? null
            : (Op.VariableAccessKind)expectedValue;

        Assert.That(operation.GetIndirectAccessKind(), Is.EqualTo(expected));
    }

    [Test]
    public void ConstantValuePreservesCilKind()
    {
        ConstantValue int32 = ConstantValue.FromInt32(1);
        ConstantValue int64 = ConstantValue.FromInt64(1);
        ConstantValue nativeInt = ConstantValue.FromNativeInt(new IntPtr(1));

        Assert.Multiple(() =>
        {
            Assert.That(int32.Kind, Is.EqualTo(ConstantValueKind.Int32));
            Assert.That(int32.GetInt32(), Is.EqualTo(1));
            Assert.That(int64.Kind, Is.EqualTo(ConstantValueKind.Int64));
            Assert.That(int64.GetInt64(), Is.EqualTo(1));
            Assert.That(nativeInt.Kind, Is.EqualTo(ConstantValueKind.NativeInt));
            Assert.That(nativeInt.GetNativeInt(), Is.EqualTo(new IntPtr(1)));
            Assert.That(int32, Is.Not.EqualTo(int64));
            Assert.That(int64, Is.Not.EqualTo(nativeInt));
        });
    }

    [Test]
    public void ConstantValuePreservesFloatingPointBits()
    {
        ConstantValue positiveZero = ConstantValue.FromFloat32(+0.0f);
        ConstantValue negativeZero = ConstantValue.FromFloat32(-0.0f);
        ConstantValue firstNan = ConstantValue.FromFloat32(FloatFromBits(0x7FC00001));
        ConstantValue sameNan = ConstantValue.FromFloat32(FloatFromBits(0x7FC00001));
        ConstantValue secondNan = ConstantValue.FromFloat32(FloatFromBits(0x7FC00002));

        Assert.Multiple(() =>
        {
            Assert.That(positiveZero, Is.Not.EqualTo(negativeZero));
            Assert.That(firstNan, Is.EqualTo(sameNan));
            Assert.That(firstNan, Is.Not.EqualTo(secondNan));
        });
    }

    [Test]
    public void ConstantValueCanIdentifyArgumentOrLocalStorageByIdentity()
    {
        var firstLocal = new Variable { id = 1, kind = VariableKind.Local, index = 0 };
        var sameIndexDifferentLocal = new Variable
            { id = 2, kind = VariableKind.Local, index = 0 };
        var temporary = new Variable { id = 3, kind = VariableKind.Temporary };

        ConstantValue reference = ConstantValue.ReferenceTo(firstLocal);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(ConstantValueKind.ManagedReference));
            Assert.That(reference.GetReferencedVariable(), Is.SameAs(firstLocal));
            Assert.That(reference, Is.EqualTo(ConstantValue.ReferenceTo(firstLocal)));
            Assert.That(reference, Is.Not.EqualTo(ConstantValue.ReferenceTo(sameIndexDifferentLocal)));
            Assert.That(() => ConstantValue.ReferenceTo(temporary), Throws.ArgumentException);
        });
    }

    [Test]
    public void ValueLatticeHasUnreachedBottomAndVaryingTop()
    {
        ValueLatticeElement constant = ValueLatticeElement.ForConstant(
            ConstantValue.FromInt32(7));

        Assert.Multiple(() =>
        {
            Assert.That(default(ValueLatticeElement), Is.EqualTo(ValueLatticeElement.Unreached));
            Assert.That(ValueLatticeElement.Unreached.Join(constant), Is.EqualTo(constant));
            Assert.That(constant.Join(ValueLatticeElement.Unreached), Is.EqualTo(constant));
            Assert.That(ValueLatticeElement.Varying.Join(constant),
                Is.EqualTo(ValueLatticeElement.Varying));
            Assert.That(constant.Join(ValueLatticeElement.Varying),
                Is.EqualTo(ValueLatticeElement.Varying));
        });
    }

    [Test]
    public void ValueLatticeJoinsEqualConstantsButWidensDifferentConstants()
    {
        ValueLatticeElement firstSeven = ValueLatticeElement.ForConstant(
            ConstantValue.FromInt32(7));
        ValueLatticeElement secondSeven = ValueLatticeElement.ForConstant(
            ConstantValue.FromInt32(7));
        ValueLatticeElement nine = ValueLatticeElement.ForConstant(
            ConstantValue.FromInt32(9));

        Assert.That(firstSeven.Join(secondSeven), Is.EqualTo(firstSeven));
        Assert.That(firstSeven.Join(nine), Is.EqualTo(ValueLatticeElement.Varying));
    }

    [Test]
    public void ValueLatticeJoinObeysLatticeLaws()
    {
        ValueLatticeElement[] values =
        [
            ValueLatticeElement.Unreached,
            ValueLatticeElement.ForConstant(ConstantValue.Null),
            ValueLatticeElement.ForConstant(ConstantValue.FromInt32(1)),
            ValueLatticeElement.ForConstant(ConstantValue.FromInt32(2)),
            ValueLatticeElement.Varying,
        ];

        foreach (ValueLatticeElement first in values)
        {
            Assert.That(first.Join(first), Is.EqualTo(first), $"join is not idempotent for {first}");
            foreach (ValueLatticeElement second in values)
            {
                Assert.That(first.Join(second), Is.EqualTo(second.Join(first)),
                    $"join is not commutative for {first} and {second}");
                foreach (ValueLatticeElement third in values)
                {
                    Assert.That(first.Join(second).Join(third), Is.EqualTo(first.Join(second.Join(third))),
                        $"join is not associative for {first}, {second}, and {third}");
                }
            }
        }
    }

    [TestCaseSource(nameof(CliReferenceTypes))]
    public void TypeLatticeCoalescesNullWithEveryCliReferenceType(Type referenceType)
    {
        Type nullType = typeof(Optimizer.TypeLattice.NullType);

        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.TypeLattice.CombineTypes(nullType, referenceType), Is.EqualTo(referenceType));
            Assert.That(Optimizer.TypeLattice.CombineTypes(referenceType, nullType), Is.EqualTo(referenceType));
        });
    }

    [Test]
    public void TypeLatticeCoalescesTwoNullValuesAsNull()
    {
        Assert.That(Optimizer.TypeLattice.CombineTypes(typeof(Optimizer.TypeLattice.NullType), typeof(Optimizer.TypeLattice.NullType)),
            Is.EqualTo(typeof(Optimizer.TypeLattice.NullType)));
    }

    [Test]
    public void TypeLatticeRecognizesNarrowEnumStorage()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.TypeLattice.ToStackType(typeof(ByteEnum)), Is.EqualTo(typeof(int)));
            Assert.That(Optimizer.TypeLattice.StorageNarrowsStackValue(typeof(ByteEnum)), Is.True);
        });
    }

    [Test]
    public void TypeLatticeCoalescesNullWithItsGlobalBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.TypeLattice.CombineTypes(typeof(Optimizer.TypeLattice.NullType), typeof(Optimizer.TypeLattice.UnknownType)),
                Is.EqualTo(typeof(Optimizer.TypeLattice.NullType)));
            Assert.That(Optimizer.TypeLattice.CombineTypes(typeof(Optimizer.TypeLattice.UnknownType), typeof(Optimizer.TypeLattice.NullType)),
                Is.EqualTo(typeof(Optimizer.TypeLattice.NullType)));
            Assert.That(Optimizer.TypeLattice.CombineTypes(typeof(Optimizer.TypeLattice.NullType), typeof(Optimizer.TypeLattice.AnyType)),
                Is.EqualTo(typeof(Optimizer.TypeLattice.AnyType)));
            Assert.That(Optimizer.TypeLattice.CombineTypes(typeof(Optimizer.TypeLattice.AnyType), typeof(Optimizer.TypeLattice.NullType)),
                Is.EqualTo(typeof(Optimizer.TypeLattice.AnyType)));
        });
    }

    [TestCaseSource(nameof(CliNonReferenceTypes))]
    public void TypeLatticeRejectsNullCoalescedWithNonReferenceType(Type nonReferenceType)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.TypeLattice.CombineTypes(typeof(Optimizer.TypeLattice.NullType), nonReferenceType),
                Is.EqualTo(typeof(void)));
            Assert.That(Optimizer.TypeLattice.CombineTypes(nonReferenceType, typeof(Optimizer.TypeLattice.NullType)),
                Is.EqualTo(typeof(void)));
        });
    }

    private static IEnumerable<TestCaseData> OperationEffectCases()
    {
        yield return EffectCase(OpCodes.Ldc_I4_1, OperationEffects.None, true);
        yield return EffectCase(OpCodes.Add, OperationEffects.None, true);
        yield return EffectCase(OpCodes.Add_Ovf, OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ckfinite, OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ldloc, OperationEffects.ReadsStorage, true);
        yield return EffectCase(OpCodes.Ldloca, OperationEffects.TakesStorageAddress, true);
        yield return EffectCase(OpCodes.Stloc, OperationEffects.WritesStorage, false);
        yield return EffectCase(OpCodes.Ldfld,
            OperationEffects.ReadsMemory | OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Stfld,
            OperationEffects.WritesMemory | OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ldflda,
            OperationEffects.TakesMemoryAddress | OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ldsfld,
            OperationEffects.ReadsMemory |
            OperationEffects.WritesMemory |
            OperationEffects.Calls |
            OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Cpobj,
            OperationEffects.ReadsMemory |
            OperationEffects.WritesMemory |
            OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Newarr,
            OperationEffects.Allocates | OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Call,
            OperationEffects.Calls |
            OperationEffects.ReadsMemory |
            OperationEffects.WritesMemory |
            OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Newobj,
            OperationEffects.Calls |
            OperationEffects.ReadsMemory |
            OperationEffects.WritesMemory |
            OperationEffects.Allocates |
            OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Br, OperationEffects.ControlFlow, false);
        yield return EffectCase(OpCodes.Break, OperationEffects.Observable, false);
        yield return EffectCase(OpCodes.Throw,
            OperationEffects.ControlFlow | OperationEffects.MayThrow, false);
    }

    private static IEnumerable<TestCaseData> IndirectAccessCases()
    {
        OpCode[] loads =
        [
            OpCodes.Ldobj,
            OpCodes.Ldind_I1, OpCodes.Ldind_U1,
            OpCodes.Ldind_I2, OpCodes.Ldind_U2,
            OpCodes.Ldind_I4, OpCodes.Ldind_U4,
            OpCodes.Ldind_I8, OpCodes.Ldind_I,
            OpCodes.Ldind_R4, OpCodes.Ldind_R8, OpCodes.Ldind_Ref,
        ];
        foreach (OpCode opcode in loads)
        {
            yield return new TestCaseData(opcode, (int)Op.VariableAccessKind.Read)
                .SetName($"IndirectAccess_{opcode}_Load");
        }

        OpCode[] stores =
        [
            OpCodes.Stobj,
            OpCodes.Stind_I1, OpCodes.Stind_I2, OpCodes.Stind_I4,
            OpCodes.Stind_I8, OpCodes.Stind_I,
            OpCodes.Stind_R4, OpCodes.Stind_R8, OpCodes.Stind_Ref,
        ];
        foreach (OpCode opcode in stores)
        {
            yield return new TestCaseData(opcode, (int)Op.VariableAccessKind.Write)
                .SetName($"IndirectAccess_{opcode}_Store");
        }

        yield return new TestCaseData(OpCodes.Ldfld, -1).SetName("IndirectAccess_ldfld_None");
    }

    private static IEnumerable<TestCaseData> CliReferenceTypes()
    {
        yield return TypeCase(typeof(object), "Object");
        yield return TypeCase(typeof(string), "BuiltInString");
        yield return TypeCase(typeof(IDisposable), "Interface");
        yield return TypeCase(typeof(int[]), "Array");
        yield return TypeCase(typeof(Action), "Delegate");
        yield return TypeCase(typeof(int).MakeByRefType(), "ManagedPointerToValue");
        yield return TypeCase(typeof(string).MakeByRefType(), "ManagedPointerToObjectReference");
        yield return TypeCase(typeof(Optimizer.TypeLattice.UnknownType).MakeByRefType(), "ManagedPointerToUnknownType");
        yield return TypeCase(typeof(Optimizer.TypeLattice.AnyType).MakeByRefType(), "ManagedPointerToAnyType");
        yield return TypeCase(typeof(int).MakePointerType(), "UnmanagedPointer");
    }

    private static IEnumerable<TestCaseData> CliNonReferenceTypes()
    {
        yield return TypeCase(typeof(int), "ValueType");
        yield return TypeCase(typeof(int?), "NullableValueType");
        yield return TypeCase(typeof(IntPtr), "NativeInt");
        yield return TypeCase(typeof(TypedReference), "TypedReference");
        yield return TypeCase(typeof(RuntimeArgumentHandle), "RuntimeArgumentHandle");
        Type[] genericParameters = typeof(GenericParameters<,,,>).GetGenericArguments();
        yield return TypeCase(genericParameters[0], "UnconstrainedGenericParameter");
        yield return TypeCase(genericParameters[1], "ReferenceConstrainedGenericParameter");
        yield return TypeCase(genericParameters[2], "ValueConstrainedGenericParameter");
        yield return TypeCase(genericParameters[3], "BaseClassConstrainedGenericParameter");
    }

    private static TestCaseData TypeCase(Type type, string name) =>
        new TestCaseData(type).SetName($"TypeLattice_Null_{name}");

    private static TestCaseData EffectCase(
        OpCode opcode,
        OperationEffects effects,
        bool canDiscardIfUnused) =>
        new TestCaseData(opcode, (int)effects, canDiscardIfUnused).SetName($"OperationEffects_{opcode}");

    private static float FloatFromBits(int bits) => BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

    private sealed class GenericParameters<T, TReference, TValue, TBase>
        where TReference : class
        where TValue : struct
        where TBase : Exception
    {
    }
}
