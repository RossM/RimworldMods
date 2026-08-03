using System.Reflection.Emit;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OptimizerInfrastructureTests
{
    [TestCaseSource(nameof(OperationEffectCases))]
    public void OperationEffectsClassifyRepresentativeCilOperations(
        OpCode opcode,
        int expectedValue,
        bool canDiscardIfUnused)
    {
        var operation = new Optimizer.Op(opcode);
        var expected = (Optimizer.OperationEffects)expectedValue;

        Assert.That(operation.Effects, Is.EqualTo(expected));
        Assert.That(operation.CanDiscardIfUnused, Is.EqualTo(canDiscardIfUnused));
    }

    [Test]
    public void OperationEffectsIncludeBundledVolatilePrefixWithoutCaching()
    {
        var operation = new Optimizer.Op(OpCodes.Ldfld, null, []);
        Optimizer.OperationEffects ordinaryEffects =
            Optimizer.OperationEffects.ReadsMemory | Optimizer.OperationEffects.MayThrow;
        Assert.That(operation.Effects, Is.EqualTo(ordinaryEffects));

        operation = new Optimizer.Op(OpCodes.Ldfld, null, [OpCodes.Volatile]);

        Assert.That(operation.Effects, Is.EqualTo(ordinaryEffects | Optimizer.OperationEffects.Volatile));
        Assert.That(operation.CanDiscardIfUnused, Is.False);
    }

    [Test]
    public void UnknownOperationEffectsAreConservativeAndExplicit()
    {
        var operation = new Optimizer.Op(OpCodes.Prefix1);

        Assert.That(operation.Effects, Is.EqualTo(
            Optimizer.OperationEffects.ReadsMemory |
            Optimizer.OperationEffects.WritesMemory |
            Optimizer.OperationEffects.MayThrow |
            Optimizer.OperationEffects.Unknown));
        Assert.That(operation.CanDiscardIfUnused, Is.False);
    }

    [TestCaseSource(nameof(IndirectAccessCases))]
    public void OpClassifiesIndirectValueAccessOpcodes(OpCode opcode, int expectedValue)
    {
        var operation = new Optimizer.Op(opcode);
        Optimizer.Op.IndirectAccessKind? expected = expectedValue < 0
            ? null
            : (Optimizer.Op.IndirectAccessKind)expectedValue;

        Assert.That(operation.GetIndirectAccessKind(), Is.EqualTo(expected));
    }

    [Test]
    public void ConstantValuePreservesCilKind()
    {
        Optimizer.ConstantValue int32 = Optimizer.ConstantValue.FromInt32(1);
        Optimizer.ConstantValue int64 = Optimizer.ConstantValue.FromInt64(1);
        Optimizer.ConstantValue nativeInt = Optimizer.ConstantValue.FromNativeInt(new IntPtr(1));

        Assert.Multiple(() =>
        {
            Assert.That(int32.Kind, Is.EqualTo(Optimizer.ConstantValueKind.Int32));
            Assert.That(int32.GetInt32(), Is.EqualTo(1));
            Assert.That(int64.Kind, Is.EqualTo(Optimizer.ConstantValueKind.Int64));
            Assert.That(int64.GetInt64(), Is.EqualTo(1));
            Assert.That(nativeInt.Kind, Is.EqualTo(Optimizer.ConstantValueKind.NativeInt));
            Assert.That(nativeInt.GetNativeInt(), Is.EqualTo(new IntPtr(1)));
            Assert.That(int32, Is.Not.EqualTo(int64));
            Assert.That(int64, Is.Not.EqualTo(nativeInt));
        });
    }

    [Test]
    public void ConstantValuePreservesFloatingPointBits()
    {
        Optimizer.ConstantValue positiveZero = Optimizer.ConstantValue.FromFloat32(+0.0f);
        Optimizer.ConstantValue negativeZero = Optimizer.ConstantValue.FromFloat32(-0.0f);
        Optimizer.ConstantValue firstNan = Optimizer.ConstantValue.FromFloat32(FloatFromBits(0x7FC00001));
        Optimizer.ConstantValue sameNan = Optimizer.ConstantValue.FromFloat32(FloatFromBits(0x7FC00001));
        Optimizer.ConstantValue secondNan = Optimizer.ConstantValue.FromFloat32(FloatFromBits(0x7FC00002));

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
        var firstLocal = new Optimizer.Variable { id = 1, kind = Optimizer.VariableKind.Local, index = 0 };
        var sameIndexDifferentLocal = new Optimizer.Variable
            { id = 2, kind = Optimizer.VariableKind.Local, index = 0 };
        var temporary = new Optimizer.Variable { id = 3, kind = Optimizer.VariableKind.Temporary };

        Optimizer.ConstantValue reference = Optimizer.ConstantValue.ReferenceTo(firstLocal);

        Assert.Multiple(() =>
        {
            Assert.That(reference.Kind, Is.EqualTo(Optimizer.ConstantValueKind.ManagedReference));
            Assert.That(reference.GetReferencedVariable(), Is.SameAs(firstLocal));
            Assert.That(reference, Is.EqualTo(Optimizer.ConstantValue.ReferenceTo(firstLocal)));
            Assert.That(reference, Is.Not.EqualTo(Optimizer.ConstantValue.ReferenceTo(sameIndexDifferentLocal)));
            Assert.That(() => Optimizer.ConstantValue.ReferenceTo(temporary), Throws.ArgumentException);
        });
    }

    [Test]
    public void ValueLatticeHasUnreachedBottomAndVaryingTop()
    {
        Optimizer.ValueLatticeElement constant = Optimizer.ValueLatticeElement.ForConstant(
            Optimizer.ConstantValue.FromInt32(7));

        Assert.Multiple(() =>
        {
            Assert.That(default(Optimizer.ValueLatticeElement), Is.EqualTo(Optimizer.ValueLatticeElement.Unreached));
            Assert.That(Optimizer.ValueLatticeElement.Unreached.Join(constant), Is.EqualTo(constant));
            Assert.That(constant.Join(Optimizer.ValueLatticeElement.Unreached), Is.EqualTo(constant));
            Assert.That(Optimizer.ValueLatticeElement.Varying.Join(constant),
                Is.EqualTo(Optimizer.ValueLatticeElement.Varying));
            Assert.That(constant.Join(Optimizer.ValueLatticeElement.Varying),
                Is.EqualTo(Optimizer.ValueLatticeElement.Varying));
        });
    }

    [Test]
    public void ValueLatticeJoinsEqualConstantsButWidensDifferentConstants()
    {
        Optimizer.ValueLatticeElement firstSeven = Optimizer.ValueLatticeElement.ForConstant(
            Optimizer.ConstantValue.FromInt32(7));
        Optimizer.ValueLatticeElement secondSeven = Optimizer.ValueLatticeElement.ForConstant(
            Optimizer.ConstantValue.FromInt32(7));
        Optimizer.ValueLatticeElement nine = Optimizer.ValueLatticeElement.ForConstant(
            Optimizer.ConstantValue.FromInt32(9));

        Assert.That(firstSeven.Join(secondSeven), Is.EqualTo(firstSeven));
        Assert.That(firstSeven.Join(nine), Is.EqualTo(Optimizer.ValueLatticeElement.Varying));
    }

    [Test]
    public void ValueLatticeJoinObeysLatticeLaws()
    {
        Optimizer.ValueLatticeElement[] values =
        [
            Optimizer.ValueLatticeElement.Unreached,
            Optimizer.ValueLatticeElement.ForConstant(Optimizer.ConstantValue.Null),
            Optimizer.ValueLatticeElement.ForConstant(Optimizer.ConstantValue.FromInt32(1)),
            Optimizer.ValueLatticeElement.ForConstant(Optimizer.ConstantValue.FromInt32(2)),
            Optimizer.ValueLatticeElement.Varying,
        ];

        foreach (Optimizer.ValueLatticeElement first in values)
        {
            Assert.That(first.Join(first), Is.EqualTo(first), $"join is not idempotent for {first}");
            foreach (Optimizer.ValueLatticeElement second in values)
            {
                Assert.That(first.Join(second), Is.EqualTo(second.Join(first)),
                    $"join is not commutative for {first} and {second}");
                foreach (Optimizer.ValueLatticeElement third in values)
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
        Type nullType = typeof(Optimizer.NullType);

        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.CombineTypes(nullType, referenceType), Is.EqualTo(referenceType));
            Assert.That(Optimizer.CombineTypes(referenceType, nullType), Is.EqualTo(referenceType));
        });
    }

    [Test]
    public void TypeLatticeCoalescesTwoNullValuesAsNull()
    {
        Assert.That(Optimizer.CombineTypes(typeof(Optimizer.NullType), typeof(Optimizer.NullType)),
            Is.EqualTo(typeof(Optimizer.NullType)));
    }

    [Test]
    public void TypeLatticeCoalescesNullWithItsGlobalBounds()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.CombineTypes(typeof(Optimizer.NullType), typeof(Optimizer.UnknownType)),
                Is.EqualTo(typeof(Optimizer.NullType)));
            Assert.That(Optimizer.CombineTypes(typeof(Optimizer.UnknownType), typeof(Optimizer.NullType)),
                Is.EqualTo(typeof(Optimizer.NullType)));
            Assert.That(Optimizer.CombineTypes(typeof(Optimizer.NullType), typeof(Optimizer.AnyType)),
                Is.EqualTo(typeof(Optimizer.AnyType)));
            Assert.That(Optimizer.CombineTypes(typeof(Optimizer.AnyType), typeof(Optimizer.NullType)),
                Is.EqualTo(typeof(Optimizer.AnyType)));
        });
    }

    [TestCaseSource(nameof(CliNonReferenceTypes))]
    public void TypeLatticeRejectsNullCoalescedWithNonReferenceType(Type nonReferenceType)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Optimizer.CombineTypes(typeof(Optimizer.NullType), nonReferenceType),
                Is.EqualTo(typeof(void)));
            Assert.That(Optimizer.CombineTypes(nonReferenceType, typeof(Optimizer.NullType)),
                Is.EqualTo(typeof(void)));
        });
    }

    private static IEnumerable<TestCaseData> OperationEffectCases()
    {
        yield return EffectCase(OpCodes.Ldc_I4_1, Optimizer.OperationEffects.None, true);
        yield return EffectCase(OpCodes.Add, Optimizer.OperationEffects.None, true);
        yield return EffectCase(OpCodes.Add_Ovf, Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ckfinite, Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ldloc, Optimizer.OperationEffects.ReadsStorage, true);
        yield return EffectCase(OpCodes.Ldloca, Optimizer.OperationEffects.TakesStorageAddress, true);
        yield return EffectCase(OpCodes.Stloc, Optimizer.OperationEffects.WritesStorage, false);
        yield return EffectCase(OpCodes.Ldfld,
            Optimizer.OperationEffects.ReadsMemory | Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Stfld,
            Optimizer.OperationEffects.WritesMemory | Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ldflda,
            Optimizer.OperationEffects.TakesMemoryAddress | Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Ldsfld,
            Optimizer.OperationEffects.ReadsMemory |
            Optimizer.OperationEffects.WritesMemory |
            Optimizer.OperationEffects.Calls |
            Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Cpobj,
            Optimizer.OperationEffects.ReadsMemory |
            Optimizer.OperationEffects.WritesMemory |
            Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Newarr,
            Optimizer.OperationEffects.Allocates | Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Call,
            Optimizer.OperationEffects.Calls |
            Optimizer.OperationEffects.ReadsMemory |
            Optimizer.OperationEffects.WritesMemory |
            Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Newobj,
            Optimizer.OperationEffects.Calls |
            Optimizer.OperationEffects.ReadsMemory |
            Optimizer.OperationEffects.WritesMemory |
            Optimizer.OperationEffects.Allocates |
            Optimizer.OperationEffects.MayThrow, false);
        yield return EffectCase(OpCodes.Br, Optimizer.OperationEffects.ControlFlow, false);
        yield return EffectCase(OpCodes.Break, Optimizer.OperationEffects.Observable, false);
        yield return EffectCase(OpCodes.Throw,
            Optimizer.OperationEffects.ControlFlow | Optimizer.OperationEffects.MayThrow, false);
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
            yield return new TestCaseData(opcode, (int)Optimizer.Op.IndirectAccessKind.Load)
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
            yield return new TestCaseData(opcode, (int)Optimizer.Op.IndirectAccessKind.Store)
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
        yield return TypeCase(typeof(Optimizer.UnknownType).MakeByRefType(), "ManagedPointerToUnknownType");
        yield return TypeCase(typeof(Optimizer.AnyType).MakeByRefType(), "ManagedPointerToAnyType");
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
        Optimizer.OperationEffects effects,
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
