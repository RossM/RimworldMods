using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Disharmony.Optimizer;
using HarmonyLib;
// ReSharper disable StringLiteralTypo

namespace Disharmony.Tests;

[TestFixture]
internal sealed class GetOutputTypeTests
{
    internal sealed record OutputTypeCase(
        string Name,
        OpCode Opcode,
        Type[] InputTypes,
        Type Expected,
        object? Operand = null,
        Prefix[]? Prefixes = null,
        string? IgnoreReason = null);

    private static readonly Type ClassType = typeof(OpcodeUtilitiesClass);
    private static readonly Type StructType = typeof(OpcodeUtilitiesStruct);

    private static readonly MethodInfo ReturnVoid = ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnVoid))!;
    private static readonly MethodInfo ReturnInt = ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnInt))!;
    private static readonly MethodInfo ReturnInstanceVoid =
        ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnInstanceVoid))!;
    private static readonly MethodInfo ReturnInstanceInt =
        ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnInstanceInt))!;
    private static readonly ConstructorInfo ClassConstructor = ClassType.GetConstructor(Type.EmptyTypes)!;
    private static readonly ConstructorInfo StructConstructor = StructType.GetConstructor([typeof(int)])!;

    private static readonly FieldInfo IntField = ClassType.GetField(nameof(OpcodeUtilitiesClass.IntField))!;
    private static readonly FieldInfo StructField = ClassType.GetField(nameof(OpcodeUtilitiesClass.StructField))!;
    private static readonly FieldInfo ClassField = ClassType.GetField(nameof(OpcodeUtilitiesClass.ClassField))!;
    private static readonly FieldInfo StaticIntField =
        ClassType.GetField(nameof(OpcodeUtilitiesClass.StaticIntField))!;
    private static readonly FieldInfo StaticStructField =
        ClassType.GetField(nameof(OpcodeUtilitiesClass.StaticStructField))!;
    private static readonly FieldInfo StaticClassField =
        ClassType.GetField(nameof(OpcodeUtilitiesClass.StaticClassField))!;

    // Local and argument opcodes receive the declared local or argument type as InputTypes[0]. Any values actually
    // popped by an opcode follow it. This applies equally to macro forms such as ldarg.0 and ldloc.2.
    // Numeric expectations use the CLI stack types int, long, native int (IntPtr), and float (represented by double).
    private static readonly OutputTypeCase[] Cases =
    [
        // Arithmetic and comparison opcodes. The first case for each opcode provides explicit opcode coverage;
        // later cases exercise the ECMA-335 numeric and managed-pointer result table without a full cross product.
        new("Add_Int_Int", OpCodes.Add, [typeof(int), typeof(int)], typeof(int)),
        new("AddOvf_Int_Int", OpCodes.Add_Ovf, [typeof(int), typeof(int)], typeof(int)),
        new("AddOvfUn_Int_Int", OpCodes.Add_Ovf_Un, [typeof(int), typeof(int)], typeof(int)),
        new("And_Int_Int", OpCodes.And, [typeof(int), typeof(int)], typeof(int)),
        new("Div_Int_Int", OpCodes.Div, [typeof(int), typeof(int)], typeof(int)),
        new("DivUn_Int_Int", OpCodes.Div_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Mul_Int_Int", OpCodes.Mul, [typeof(int), typeof(int)], typeof(int)),
        new("MulOvf_Int_Int", OpCodes.Mul_Ovf, [typeof(int), typeof(int)], typeof(int)),
        new("MulOvfUn_Int_Int", OpCodes.Mul_Ovf_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Neg_Int", OpCodes.Neg, [typeof(int)], typeof(int)),
        new("Not_Int", OpCodes.Not, [typeof(int)], typeof(int)),
        new("Or_Int_Int", OpCodes.Or, [typeof(int), typeof(int)], typeof(int)),
        new("Rem_Int_Int", OpCodes.Rem, [typeof(int), typeof(int)], typeof(int)),
        new("RemUn_Int_Int", OpCodes.Rem_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Sub_Int_Int", OpCodes.Sub, [typeof(int), typeof(int)], typeof(int)),
        new("SubOvf_Int_Int", OpCodes.Sub_Ovf, [typeof(int), typeof(int)], typeof(int)),
        new("SubOvfUn_Int_Int", OpCodes.Sub_Ovf_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Xor_Int_Int", OpCodes.Xor, [typeof(int), typeof(int)], typeof(int)),
        new("Ceq_Int_Int", OpCodes.Ceq, [typeof(int), typeof(int)], typeof(int)),
        new("Cgt_Int_Int", OpCodes.Cgt, [typeof(int), typeof(int)], typeof(int)),
        new("CgtUn_Int_Int", OpCodes.Cgt_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Clt_Int_Int", OpCodes.Clt, [typeof(int), typeof(int)], typeof(int)),
        new("CltUn_Int_Int", OpCodes.Clt_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Add_Long_Long", OpCodes.Add, [typeof(long), typeof(long)], typeof(long)),
        new("Add_IntPtr_IntPtr", OpCodes.Add, [typeof(IntPtr), typeof(IntPtr)], typeof(IntPtr)),
        new("Add_Double_Double", OpCodes.Add, [typeof(double), typeof(double)], typeof(double)),
        new("Add_Int_IntPtr", OpCodes.Add, [typeof(int), typeof(IntPtr)], typeof(IntPtr)),
        new("Add_IntPtr_Int", OpCodes.Add, [typeof(IntPtr), typeof(int)], typeof(IntPtr)),
        new("Add_Class_Int", OpCodes.Add, [ClassType, typeof(int)], ClassType),
        new("Add_Int_Class", OpCodes.Add, [typeof(int), ClassType], ClassType),
        new("Add_StructRef_IntPtr", OpCodes.Add,
            [StructType.MakeByRefType(), typeof(IntPtr)], StructType.MakeByRefType()),
        new("Add_IntPtr_ClassRef", OpCodes.Add,
            [typeof(IntPtr), ClassType.MakeByRefType()], ClassType.MakeByRefType()),
        new("Sub_Class_Class", OpCodes.Sub, [ClassType, ClassType], typeof(IntPtr)),
        new("Sub_IntRef_StructRef", OpCodes.Sub,
            [typeof(int).MakeByRefType(), StructType.MakeByRefType()], typeof(IntPtr)),

        // Shift opcodes return the first operand's type.
        new("Shl_Int_Int", OpCodes.Shl, [typeof(int), typeof(int)], typeof(int)),
        new("Shr_Int_Int", OpCodes.Shr, [typeof(int), typeof(int)], typeof(int)),
        new("ShrUn_Int_Int", OpCodes.Shr_Un, [typeof(int), typeof(int)], typeof(int)),
        new("Shl_Long_Int", OpCodes.Shl, [typeof(long), typeof(int)], typeof(long)),
        new("Shl_IntPtr_Int", OpCodes.Shl, [typeof(IntPtr), typeof(int)], typeof(IntPtr)),
        new("Shl_Long_IntPtr", OpCodes.Shl, [typeof(long), typeof(IntPtr)], typeof(long)),

        // PushesInput opcodes preserve their input type.
        new("Dup_Int", OpCodes.Dup, [typeof(int)], typeof(int)),
        new("Dup_Long", OpCodes.Dup, [typeof(long)], typeof(long)),
        new("Dup_IntPtr", OpCodes.Dup, [typeof(IntPtr)], typeof(IntPtr)),
        new("Dup_Double", OpCodes.Dup, [typeof(double)], typeof(double)),
        new("Dup_Struct", OpCodes.Dup, [StructType], StructType),
        new("Dup_Class", OpCodes.Dup, [ClassType], ClassType),
        new("Dup_IntRef", OpCodes.Dup, [typeof(int).MakeByRefType()], typeof(int).MakeByRefType()),
        new("Dup_StructRef", OpCodes.Dup, [StructType.MakeByRefType()], StructType.MakeByRefType()),
        new("Dup_ClassRef", OpCodes.Dup, [ClassType.MakeByRefType()], ClassType.MakeByRefType()),
        new("Dup_Null", OpCodes.Dup, [TypeLattice.Null], TypeLattice.Null),
        new("Dup_Unknown", OpCodes.Dup, [TypeLattice.Unknown], TypeLattice.Unknown),
        new("Dup_Any", OpCodes.Dup, [TypeLattice.Any], TypeLattice.Any),
        new("Ckfinite_Double", OpCodes.Ckfinite, [typeof(double)], typeof(double)),

        // Conversion opcodes.
        new("ConvI_Int", OpCodes.Conv_I, [typeof(int)], typeof(IntPtr)),
        new("ConvI1_Int", OpCodes.Conv_I1, [typeof(int)], typeof(int)),
        new("ConvI2_Int", OpCodes.Conv_I2, [typeof(int)], typeof(int)),
        new("ConvI4_Int", OpCodes.Conv_I4, [typeof(int)], typeof(int)),
        new("ConvI8_Int", OpCodes.Conv_I8, [typeof(int)], typeof(long)),
        new("ConvU_Int", OpCodes.Conv_U, [typeof(int)], typeof(IntPtr)),
        new("ConvU1_Int", OpCodes.Conv_U1, [typeof(int)], typeof(int)),
        new("ConvU2_Int", OpCodes.Conv_U2, [typeof(int)], typeof(int)),
        new("ConvU4_Int", OpCodes.Conv_U4, [typeof(int)], typeof(int)),
        new("ConvU8_Int", OpCodes.Conv_U8, [typeof(int)], typeof(long)),
        new("ConvRUn_Int", OpCodes.Conv_R_Un, [typeof(int)], typeof(double)),
        new("ConvR4_Int", OpCodes.Conv_R4, [typeof(int)], typeof(double)),
        new("ConvR8_Int", OpCodes.Conv_R8, [typeof(int)], typeof(double)),
        new("ConvOvfI_Int", OpCodes.Conv_Ovf_I, [typeof(int)], typeof(IntPtr)),
        new("ConvOvfIUn_Int", OpCodes.Conv_Ovf_I_Un, [typeof(int)], typeof(IntPtr)),
        new("ConvOvfI1_Int", OpCodes.Conv_Ovf_I1, [typeof(int)], typeof(int)),
        new("ConvOvfI1Un_Int", OpCodes.Conv_Ovf_I1_Un, [typeof(int)], typeof(int)),
        new("ConvOvfI2_Int", OpCodes.Conv_Ovf_I2, [typeof(int)], typeof(int)),
        new("ConvOvfI2Un_Int", OpCodes.Conv_Ovf_I2_Un, [typeof(int)], typeof(int)),
        new("ConvOvfI4_Int", OpCodes.Conv_Ovf_I4, [typeof(int)], typeof(int)),
        new("ConvOvfI4Un_Int", OpCodes.Conv_Ovf_I4_Un, [typeof(int)], typeof(int)),
        new("ConvOvfI8_Int", OpCodes.Conv_Ovf_I8, [typeof(int)], typeof(long)),
        new("ConvOvfI8Un_Int", OpCodes.Conv_Ovf_I8_Un, [typeof(int)], typeof(long)),
        new("ConvOvfU_Int", OpCodes.Conv_Ovf_U, [typeof(int)], typeof(IntPtr)),
        new("ConvOvfUUn_Int", OpCodes.Conv_Ovf_U_Un, [typeof(int)], typeof(IntPtr)),
        new("ConvOvfU1_Int", OpCodes.Conv_Ovf_U1, [typeof(int)], typeof(int)),
        new("ConvOvfU1Un_Int", OpCodes.Conv_Ovf_U1_Un, [typeof(int)], typeof(int)),
        new("ConvOvfU2_Int", OpCodes.Conv_Ovf_U2, [typeof(int)], typeof(int)),
        new("ConvOvfU2Un_Int", OpCodes.Conv_Ovf_U2_Un, [typeof(int)], typeof(int)),
        new("ConvOvfU4_Int", OpCodes.Conv_Ovf_U4, [typeof(int)], typeof(int)),
        new("ConvOvfU4Un_Int", OpCodes.Conv_Ovf_U4_Un, [typeof(int)], typeof(int)),
        new("ConvOvfU8_Int", OpCodes.Conv_Ovf_U8, [typeof(int)], typeof(long)),
        new("ConvOvfU8Un_Int", OpCodes.Conv_Ovf_U8_Un, [typeof(int)], typeof(long)),

        // Constants and fixed-result metadata-independent opcodes.
        new("Arglist", OpCodes.Arglist, [], typeof(RuntimeArgumentHandle)),
        new("LdcI4_Value1", OpCodes.Ldc_I4, [], typeof(int), Operand: 1),
        new("LdcI4S_Value1", OpCodes.Ldc_I4_S, [], typeof(int), Operand: (sbyte)1),
        new("LdcI4M1", OpCodes.Ldc_I4_M1, [], typeof(int)),
        new("LdcI4_0", OpCodes.Ldc_I4_0, [], typeof(int)),
        new("LdcI4_1", OpCodes.Ldc_I4_1, [], typeof(int)),
        new("LdcI4_2", OpCodes.Ldc_I4_2, [], typeof(int)),
        new("LdcI4_3", OpCodes.Ldc_I4_3, [], typeof(int)),
        new("LdcI4_4", OpCodes.Ldc_I4_4, [], typeof(int)),
        new("LdcI4_5", OpCodes.Ldc_I4_5, [], typeof(int)),
        new("LdcI4_6", OpCodes.Ldc_I4_6, [], typeof(int)),
        new("LdcI4_7", OpCodes.Ldc_I4_7, [], typeof(int)),
        new("LdcI4_8", OpCodes.Ldc_I4_8, [], typeof(int)),
        new("LdcI8_Value1", OpCodes.Ldc_I8, [], typeof(long), Operand: 1L),
        new("LdcR4_Value1", OpCodes.Ldc_R4, [], typeof(double), Operand: 1f),
        new("LdcR8_Value1", OpCodes.Ldc_R8, [], typeof(double), Operand: 1d),
        new("Ldnull", OpCodes.Ldnull, [], TypeLattice.Null),
        new("Ldstr_Text", OpCodes.Ldstr, [], typeof(string), Operand: "text"),
        new("Ldftn_ReturnInt", OpCodes.Ldftn, [], typeof(IntPtr), Operand: ReturnInt),
        new("Ldvirtftn_ReturnInstanceInt_Class", OpCodes.Ldvirtftn, [ClassType], typeof(IntPtr), Operand: ReturnInstanceInt),
        new("Ldlen", OpCodes.Ldlen, [typeof(int[])], typeof(IntPtr)),
        new("Localloc", OpCodes.Localloc, [typeof(IntPtr)], typeof(IntPtr)),
        new("Mkrefany_Int_IntRef", OpCodes.Mkrefany, [typeof(int).MakeByRefType()], typeof(TypedReference),
            Operand: typeof(int)),
        new("Refanytype", OpCodes.Refanytype, [typeof(TypedReference)], typeof(TypeToken)),
        new("Sizeof_Struct", OpCodes.Sizeof, [], typeof(IntPtr), Operand: StructType),

        // Variable loads. The declared variable type is the synthetic first input described above.
        new("Ldarg_0_Int", OpCodes.Ldarg, [typeof(int)], typeof(int), Operand: 0),
        new("LdargS_1_Long", OpCodes.Ldarg_S, [typeof(long)], typeof(long), Operand: 1),
        new("Ldarg0_Struct", OpCodes.Ldarg_0, [StructType], StructType),
        new("Ldarg1_Class", OpCodes.Ldarg_1, [ClassType], ClassType),
        new("Ldarg2_IntRef", OpCodes.Ldarg_2,
            [typeof(int).MakeByRefType()], typeof(int).MakeByRefType()),
        new("Ldarg3_ClassRef", OpCodes.Ldarg_3, [ClassType.MakeByRefType()],
            ClassType.MakeByRefType()),
        new("Ldarga_0_Int", OpCodes.Ldarga, [typeof(int)], typeof(int).MakeByRefType(), Operand: 0),
        new("LdargaS_1_Struct", OpCodes.Ldarga_S, [StructType], StructType.MakeByRefType(), Operand: 1),
        new("Ldloc_0_Int", OpCodes.Ldloc, [typeof(int)], typeof(int), Operand: 0),
        new("LdlocS_1_Long", OpCodes.Ldloc_S, [typeof(long)], typeof(long), Operand: 1),
        new("Ldloc0_IntPtr", OpCodes.Ldloc_0, [typeof(IntPtr)], typeof(IntPtr)),
        new("Ldloc1_Double", OpCodes.Ldloc_1, [typeof(double)], typeof(double)),
        new("Ldloc2_Struct", OpCodes.Ldloc_2, [StructType], StructType),
        new("Ldloc3_Class", OpCodes.Ldloc_3, [ClassType], ClassType),
        new("Ldloca_0_Class", OpCodes.Ldloca, [ClassType], ClassType.MakeByRefType(), Operand: 0),
        new("LdlocaS_1_Struct", OpCodes.Ldloca_S, [StructType], StructType.MakeByRefType(), Operand: 1),
        new("Ldarg_0_Unknown", OpCodes.Ldarg, [TypeLattice.Unknown], TypeLattice.Unknown, Operand: 0),
        new("Ldloc_0_Any", OpCodes.Ldloc, [TypeLattice.Any], TypeLattice.Any, Operand: 0),
        new("Ldarga_0_Unknown", OpCodes.Ldarga,
            [TypeLattice.Unknown], TypeLattice.Unknown.MakeByRefType(), Operand: 0),
        new("Ldarga_0_Any", OpCodes.Ldarga,
            [TypeLattice.Any], TypeLattice.Any.MakeByRefType(), Operand: 0),
        new("Ldloca_0_Unknown", OpCodes.Ldloca,
            [TypeLattice.Unknown], TypeLattice.Unknown.MakeByRefType(), Operand: 0),
        new("Ldloca_0_Any", OpCodes.Ldloca,
            [TypeLattice.Any], TypeLattice.Any.MakeByRefType(), Operand: 0),

        // Typed indirect and array loads.
        new("LdindI_IntRef", OpCodes.Ldind_I,
            [typeof(IntPtr).MakeByRefType()], typeof(IntPtr)),
        new("LdindI1_IntRef", OpCodes.Ldind_I1, [typeof(int).MakeByRefType()], typeof(int)),
        new("LdindI2_IntRef", OpCodes.Ldind_I2, [typeof(int).MakeByRefType()], typeof(int)),
        new("LdindI4_IntRef", OpCodes.Ldind_I4, [typeof(int).MakeByRefType()], typeof(int)),
        new("LdindI8_LongRef", OpCodes.Ldind_I8, [typeof(long).MakeByRefType()], typeof(long)),
        new("LdindU1_IntRef", OpCodes.Ldind_U1, [typeof(int).MakeByRefType()], typeof(int)),
        new("LdindU2_IntRef", OpCodes.Ldind_U2, [typeof(int).MakeByRefType()], typeof(int)),
        new("LdindU4_IntRef", OpCodes.Ldind_U4, [typeof(int).MakeByRefType()], typeof(int)),
        new("LdindR4_DoubleRef", OpCodes.Ldind_R4, [typeof(double).MakeByRefType()], typeof(double)),
        new("LdindR8_DoubleRef", OpCodes.Ldind_R8, [typeof(double).MakeByRefType()], typeof(double)),
        new("LdindRef_ClassRef", OpCodes.Ldind_Ref, [ClassType.MakeByRefType()], ClassType),
        new("LdindRef_UnknownRef", OpCodes.Ldind_Ref,
            [TypeLattice.Unknown.MakeByRefType()], TypeLattice.Unknown),
        new("LdindRef_AnyRef", OpCodes.Ldind_Ref,
            [TypeLattice.Any.MakeByRefType()], TypeLattice.Any),
        new("LdelemI_IntPtrArray_Int", OpCodes.Ldelem_I,
            [typeof(IntPtr[]), typeof(int)], typeof(IntPtr)),
        new("LdelemI1_IntArray_Int", OpCodes.Ldelem_I1, [typeof(int[]), typeof(int)], typeof(int)),
        new("LdelemI2_IntArray_Int", OpCodes.Ldelem_I2, [typeof(int[]), typeof(int)], typeof(int)),
        new("LdelemI4_IntArray_Int", OpCodes.Ldelem_I4, [typeof(int[]), typeof(int)], typeof(int)),
        new("LdelemI8_LongArray_Int", OpCodes.Ldelem_I8, [typeof(long[]), typeof(int)], typeof(long)),
        new("LdelemU1_IntArray_Int", OpCodes.Ldelem_U1, [typeof(int[]), typeof(int)], typeof(int)),
        new("LdelemU2_IntArray_Int", OpCodes.Ldelem_U2, [typeof(int[]), typeof(int)], typeof(int)),
        new("LdelemU4_IntArray_Int", OpCodes.Ldelem_U4, [typeof(int[]), typeof(int)], typeof(int)),
        new("LdelemR4_DoubleArray_Int", OpCodes.Ldelem_R4,
            [typeof(double[]), typeof(int)], typeof(double)),
        new("LdelemR8_DoubleArray_Int", OpCodes.Ldelem_R8,
            [typeof(double[]), typeof(int)], typeof(double)),
        new("LdelemRef_ClassArray_Int", OpCodes.Ldelem_Ref,
            [ClassType.MakeArrayType(), typeof(int)], ClassType),
        new("LdelemRef_NullArray_Int", OpCodes.Ldelem_Ref,
            [TypeLattice.Null, typeof(int)], TypeLattice.Unknown),

        // Type-operand instructions with representative numeric, struct, class, and managed-reference cases.
        new("Ldobj_Int_IntRef", OpCodes.Ldobj, [typeof(int).MakeByRefType()], typeof(int), Operand: typeof(int)),
        new("Ldobj_Long_LongRef", OpCodes.Ldobj, [typeof(long).MakeByRefType()], typeof(long), Operand: typeof(long)),
        new("Ldobj_IntPtr_IntPtrRef", OpCodes.Ldobj,
            [typeof(IntPtr).MakeByRefType()], typeof(IntPtr), Operand: typeof(IntPtr)),
        new("Ldobj_Double_DoubleRef", OpCodes.Ldobj,
            [typeof(double).MakeByRefType()], typeof(double), Operand: typeof(double)),
        new("Ldobj_Struct_StructRef", OpCodes.Ldobj, [StructType.MakeByRefType()], StructType, Operand: StructType),
        new("Ldobj_Class_ClassRef", OpCodes.Ldobj, [ClassType.MakeByRefType()], ClassType, Operand: ClassType),
        new("Ldobj_Unknown_UnknownRef", OpCodes.Ldobj,
            [TypeLattice.Unknown.MakeByRefType()], TypeLattice.Unknown, Operand: TypeLattice.Unknown),
        new("Ldobj_Any_AnyRef", OpCodes.Ldobj,
            [TypeLattice.Any.MakeByRefType()], TypeLattice.Any, Operand: TypeLattice.Any),
        new("Ldelem_Int_IntArray_Int", OpCodes.Ldelem, [typeof(int[]), typeof(int)], typeof(int), Operand: typeof(int)),
        new("Ldelem_Struct_StructArray_Int", OpCodes.Ldelem,
            [StructType.MakeArrayType(), typeof(int)], StructType, Operand: StructType),
        new("Ldelem_Class_ClassArray_Int", OpCodes.Ldelem,
            [ClassType.MakeArrayType(), typeof(int)], ClassType, Operand: ClassType),
        new("Ldelema_Struct_StructArray_Int", OpCodes.Ldelema,
            [StructType.MakeArrayType(), typeof(int)], StructType.MakeByRefType(), Operand: StructType),
        new("Ldelema_Unknown_Unknown_Int", OpCodes.Ldelema,
            [TypeLattice.Unknown, typeof(int)], TypeLattice.Unknown.MakeByRefType(), Operand: TypeLattice.Unknown),
        new("Ldelema_Any_Any_Int", OpCodes.Ldelema,
            [TypeLattice.Any, typeof(int)], TypeLattice.Any.MakeByRefType(), Operand: TypeLattice.Any),
        new("Newarr_Int_Int", OpCodes.Newarr, [typeof(int)], typeof(int[]), Operand: typeof(int)),
        new("Newarr_Struct_Int", OpCodes.Newarr,
            [typeof(int)], StructType.MakeArrayType(), Operand: StructType),
        new("Newarr_Class_Int", OpCodes.Newarr,
            [typeof(int)], ClassType.MakeArrayType(), Operand: ClassType),
        new("Castclass_Class_Object", OpCodes.Castclass, [typeof(object)], ClassType, Operand: ClassType),
        new("Castclass_Class_Null", OpCodes.Castclass, [TypeLattice.Null], ClassType, Operand: ClassType),
        new("Isinst_Class_Object", OpCodes.Isinst, [typeof(object)], ClassType, Operand: ClassType),
        new("Isinst_Class_Null", OpCodes.Isinst, [TypeLattice.Null], ClassType, Operand: ClassType),
        new("Box_Int_Int", OpCodes.Box, [typeof(int)], typeof(object), Operand: typeof(int)),
        new("Box_Struct_Struct", OpCodes.Box, [StructType], typeof(object), Operand: StructType),
        new("Unbox_Int_Object", OpCodes.Unbox,
            [typeof(object)], typeof(int).MakeByRefType(), Operand: typeof(int)),
        new("Unbox_Struct_Object", OpCodes.Unbox,
            [typeof(object)], StructType.MakeByRefType(), Operand: StructType),
        new("UnboxAny_Int_Object", OpCodes.Unbox_Any, [typeof(object)], typeof(int), Operand: typeof(int)),
        new("UnboxAny_Struct_Object", OpCodes.Unbox_Any, [typeof(object)], StructType, Operand: StructType),
        new("UnboxAny_Class_Object", OpCodes.Unbox_Any, [typeof(object)], ClassType, Operand: ClassType),
        new("Refanyval_Struct_TypedReference", OpCodes.Refanyval,
            [typeof(TypedReference)], StructType.MakeByRefType(), Operand: StructType),

        // Field operands.
        new("Ldfld_IntField_Class", OpCodes.Ldfld, [ClassType], typeof(int), Operand: IntField),
        new("Ldfld_StructField_Class", OpCodes.Ldfld, [ClassType], StructType, Operand: StructField),
        new("Ldfld_ClassField_Class", OpCodes.Ldfld, [ClassType], ClassType, Operand: ClassField),
        new("Ldflda_IntField_Class", OpCodes.Ldflda,
            [ClassType], typeof(int).MakeByRefType(), Operand: IntField),
        new("Ldflda_StructField_Class", OpCodes.Ldflda,
            [ClassType], StructType.MakeByRefType(), Operand: StructField),
        new("Ldflda_ClassField_Class", OpCodes.Ldflda,
            [ClassType], ClassType.MakeByRefType(), Operand: ClassField),
        new("Ldsfld_StaticIntField", OpCodes.Ldsfld, [], typeof(int), Operand: StaticIntField),
        new("Ldsfld_StaticStructField", OpCodes.Ldsfld, [], StructType, Operand: StaticStructField),
        new("Ldsfld_StaticClassField", OpCodes.Ldsfld, [], ClassType, Operand: StaticClassField),
        new("Ldsflda_StaticIntField", OpCodes.Ldsflda,
            [], typeof(int).MakeByRefType(), Operand: StaticIntField),
        new("Ldsflda_StaticStructField", OpCodes.Ldsflda,
            [], StructType.MakeByRefType(), Operand: StaticStructField),
        new("Ldsflda_StaticClassField", OpCodes.Ldsflda,
            [], ClassType.MakeByRefType(), Operand: StaticClassField),

        // Varpush opcodes use method/signature metadata. Both void and value cases are explicit because Push0 handling is
        // already tested separately in OpCodeValuesTests.
        new("Call_ReturnVoid", OpCodes.Call, [], typeof(void), Operand: ReturnVoid),
        new("Call_ReturnInt", OpCodes.Call, [], typeof(int), Operand: ReturnInt),
        new("Call_ReturnLong", OpCodes.Call, [], typeof(long),
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnLong))!),
        new("Call_ReturnIntPtr", OpCodes.Call, [], typeof(IntPtr),
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnIntPtr))!),
        new("Call_ReturnDouble", OpCodes.Call, [], typeof(double),
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnDouble))!),
        new("Call_ReturnStruct", OpCodes.Call, [], StructType,
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnStruct))!),
        new("Call_ReturnClass", OpCodes.Call, [], ClassType,
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnClass))!),
        new("Call_ReturnIntByReference", OpCodes.Call, [], typeof(int).MakeByRefType(),
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnIntByReference))!),
        new("Call_ReturnStructByReference", OpCodes.Call, [], StructType.MakeByRefType(),
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnStructByReference))!),
        new("Call_ReturnClassByReference", OpCodes.Call, [], ClassType.MakeByRefType(),
            Operand: ClassType.GetMethod(nameof(OpcodeUtilitiesClass.ReturnClassByReference))!),
        new("Callvirt_ReturnInstanceVoid_Class", OpCodes.Callvirt, [ClassType], typeof(void), Operand: ReturnInstanceVoid),
        new("Callvirt_ReturnInstanceInt_Class", OpCodes.Callvirt, [ClassType], typeof(int), Operand: ReturnInstanceInt),
        new("Calli_VoidSignature", OpCodes.Calli, [], typeof(void), Operand: CreateInlineSignature(typeof(void)),
            IgnoreReason: "The optimizer does not currently support calli instructions"),
        new("Calli_IntSignature", OpCodes.Calli, [], typeof(int), Operand: CreateInlineSignature(typeof(int)),
            IgnoreReason: "The optimizer does not currently support calli instructions"),
        new("Newobj_ClassConstructor", OpCodes.Newobj, [], ClassType, Operand: ClassConstructor),
        new("Newobj_StructConstructor_Int", OpCodes.Newobj, [typeof(int)], StructType, Operand: StructConstructor),

        // ldtoken depends on the metadata operand kind.
        new("Ldtoken_StructType", OpCodes.Ldtoken, [], typeof(RuntimeTypeHandle), Operand: StructType),
        new("Ldtoken_ReturnInt", OpCodes.Ldtoken, [], typeof(RuntimeMethodHandle), Operand: ReturnInt),
        new("Ldtoken_IntField", OpCodes.Ldtoken, [], typeof(RuntimeFieldHandle), Operand: IntField),

        // Arithmetic lattice propagation uses the broad compatibility rules documented on OpCodeData.Arithmetic, not
        // the more restrictive valid-input table for any one ECMA opcode. A concrete double uniquely constrains the
        // other operand to double. Integer, native-integer, and reference operands do not uniquely constrain it because
        // the other operand might select a different valid result rule.
        new("Add_Double_Any", OpCodes.Add,
            [typeof(double), TypeLattice.Any], typeof(double)),
        new("Add_Any_Double", OpCodes.Add,
            [TypeLattice.Any, typeof(double)], typeof(double)),
        new("Add_Double_Unknown", OpCodes.Add,
            [typeof(double), TypeLattice.Unknown], typeof(double)),
        new("Add_Unknown_Double", OpCodes.Add,
            [TypeLattice.Unknown, typeof(double)], typeof(double)),
        new("Add_Int_Any", OpCodes.Add,
            [typeof(int), TypeLattice.Any], TypeLattice.Any),
        new("Add_Any_Int", OpCodes.Add,
            [TypeLattice.Any, typeof(int)], TypeLattice.Any),
        new("Add_IntPtr_Any", OpCodes.Add,
            [typeof(IntPtr), TypeLattice.Any], TypeLattice.Any),
        new("Add_Long_Any", OpCodes.Add,
            [typeof(long), TypeLattice.Any], TypeLattice.Any),
        new("Add_Class_Any", OpCodes.Add,
            [ClassType, TypeLattice.Any], TypeLattice.Any),
        new("Add_StructRef_Any", OpCodes.Add,
            [StructType.MakeByRefType(), TypeLattice.Any], TypeLattice.Any),
        new("Add_Int_Unknown", OpCodes.Add,
            [typeof(int), TypeLattice.Unknown], TypeLattice.Unknown),
        new("Add_Unknown_Int", OpCodes.Add,
            [TypeLattice.Unknown, typeof(int)], TypeLattice.Unknown),
        new("Add_IntPtr_Unknown", OpCodes.Add,
            [typeof(IntPtr), TypeLattice.Unknown], TypeLattice.Unknown),
        new("Add_Long_Unknown", OpCodes.Add,
            [typeof(long), TypeLattice.Unknown], TypeLattice.Unknown),
        new("Add_Class_Unknown", OpCodes.Add,
            [ClassType, TypeLattice.Unknown], TypeLattice.Unknown),
        new("Add_StructRef_Unknown", OpCodes.Add,
            [StructType.MakeByRefType(), TypeLattice.Unknown], TypeLattice.Unknown),
        new("Add_Any_Unknown", OpCodes.Add,
            [TypeLattice.Any, TypeLattice.Unknown], TypeLattice.Any),
        new("Add_Unknown_Any", OpCodes.Add,
            [TypeLattice.Unknown, TypeLattice.Any], TypeLattice.Any),
        new("Add_Unknown_Unknown", OpCodes.Add,
            [TypeLattice.Unknown, TypeLattice.Unknown], TypeLattice.Unknown),
        new("Neg_Any", OpCodes.Neg, [TypeLattice.Any], TypeLattice.Any),
        new("Neg_Unknown", OpCodes.Neg,
            [TypeLattice.Unknown], TypeLattice.Unknown),

        // Shift results are always the type of the first operand. A concrete first operand therefore wins before the
        // Any/Unknown fallback rules are considered; a lattice-valued first operand is not uniquely determined.
        new("Shl_Int_Any", OpCodes.Shl,
            [typeof(int), TypeLattice.Any], typeof(int)),
        new("Shl_Long_Unknown", OpCodes.Shl,
            [typeof(long), TypeLattice.Unknown], typeof(long)),
        new("Shl_IntPtr_Any", OpCodes.Shl,
            [typeof(IntPtr), TypeLattice.Any], typeof(IntPtr)),
        new("Shl_Any_Int", OpCodes.Shl,
            [TypeLattice.Any, typeof(int)], TypeLattice.Any),
        new("Shl_Any_IntPtr", OpCodes.Shl,
            [TypeLattice.Any, typeof(IntPtr)], TypeLattice.Any),
        new("Shl_Unknown_Int", OpCodes.Shl,
            [TypeLattice.Unknown, typeof(int)], TypeLattice.Unknown),
        new("Shl_Unknown_IntPtr", OpCodes.Shl,
            [TypeLattice.Unknown, typeof(IntPtr)], TypeLattice.Unknown),
        new("Shl_Any_Unknown", OpCodes.Shl,
            [TypeLattice.Any, TypeLattice.Unknown], TypeLattice.Any),
        new("Shl_Unknown_Any", OpCodes.Shl,
            [TypeLattice.Unknown, TypeLattice.Any], TypeLattice.Any),
        new("Shl_Unknown_Unknown", OpCodes.Shl,
            [TypeLattice.Unknown, TypeLattice.Unknown], TypeLattice.Unknown),

        // Prefixes affect execution semantics but not the type pushed by these instructions.
        new("LdindI4_Volatile_Unaligned_IntRef", OpCodes.Ldind_I4,
            [typeof(int).MakeByRefType()], typeof(int),
            Prefixes: [new Prefix(OpCodes.Unaligned, (byte)1), new Prefix(OpCodes.Volatile, null)]),
        new("Ldelema_Struct_Readonly_StructArray_Int", OpCodes.Ldelema,
            [StructType.MakeArrayType(), typeof(int)], StructType.MakeByRefType(), Operand: StructType,
            Prefixes: [new Prefix(OpCodes.Readonly, null)]),
        new("Callvirt_ReturnInstanceInt_Constrained_Class", OpCodes.Callvirt,
            [ClassType], typeof(int), Operand: ReturnInstanceInt, Prefixes: [new Prefix(OpCodes.Constrained, ClassType)]),
        new("Call_ReturnInt_Tail", OpCodes.Call,
            [], typeof(int), Operand: ReturnInt, Prefixes: [new Prefix(OpCodes.Tailcall, null)]),
    ];

    private static IEnumerable<TestCaseData> OutputTypeCases()
    {
        foreach (OutputTypeCase testCase in Cases)
        {
            TestCaseData data = new TestCaseData(testCase).SetName(testCase.Name);
            if (testCase.IgnoreReason != null)
                data.Ignore(testCase.IgnoreReason);
            yield return data;
        }
    }

    [TestCaseSource(nameof(OutputTypeCases))]
    public void ReturnsTheCilStackType(OutputTypeCase testCase)
    {
        ILInstruction instruction = new(testCase.Opcode, testCase.Operand!, testCase.Prefixes ?? []);

        Assert.That(OpcodeUtilities.GetOutputType(instruction, testCase.InputTypes), Is.EqualTo(testCase.Expected));
    }

    private static object CreateInlineSignature(Type returnType)
    {
        Type signatureType = typeof(CodeInstruction).Assembly.GetType("HarmonyLib.InlineSignature")!;
        object signature = Activator.CreateInstance(signatureType)!;
        signatureType.GetProperty("CallingConvention")!.SetValue(signature, CallingConvention.Cdecl);
        signatureType.GetProperty("Parameters")!.SetValue(signature, new List<object>());
        signatureType.GetProperty("ReturnType")!.SetValue(signature, returnType);
        return signature;
    }
}
