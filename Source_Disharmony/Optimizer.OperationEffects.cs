namespace Disharmony;

internal partial class Optimizer
{
    /// <summary>
    ///     Conservative facts about executing an operation. A set flag means that the effect may
    ///     occur; an absent flag means that the classifier knows it cannot occur.
    /// </summary>
    [Flags]
    internal enum OperationEffects
    {
        None = 0,

        /// <summary>Reads an argument or local slot represented explicitly in Variables form.</summary>
        ReadsStorage = 1 << 0,

        /// <summary>Writes an argument or local slot represented explicitly in Variables form.</summary>
        WritesStorage = 1 << 1,

        /// <summary>Takes the address of an argument or local slot.</summary>
        TakesStorageAddress = 1 << 2,

        /// <summary>Reads storage reached through a reference, pointer, field, or array.</summary>
        ReadsMemory = 1 << 3,

        /// <summary>Writes storage reached through a reference, pointer, field, or array.</summary>
        WritesMemory = 1 << 4,

        /// <summary>Computes an address within an object, static field, or array.</summary>
        TakesMemoryAddress = 1 << 5,

        /// <summary>Invokes user or runtime code whose effects are not otherwise modeled.</summary>
        Calls = 1 << 6,

        /// <summary>Allocates managed or stack storage.</summary>
        Allocates = 1 << 7,

        /// <summary>Can terminate normal execution by throwing or faulting.</summary>
        MayThrow = 1 << 8,

        /// <summary>Can choose or terminate the normal control-flow continuation.</summary>
        ControlFlow = 1 << 9,

        /// <summary>Has volatile memory semantics due to an attached CIL prefix.</summary>
        Volatile = 1 << 10,

        /// <summary>Has an observable effect not described by the other categories.</summary>
        Observable = 1 << 11,

        /// <summary>The opcode has no precise model, so conservative effects were substituted.</summary>
        Unknown = 1 << 12,
    }

    /// <summary>Central classification of CIL operation effects used by analysis passes.</summary>
    private static class OperationEffectClassifier
    {
        private const OperationEffects UnknownEffects =
            OperationEffects.ReadsMemory |
            OperationEffects.WritesMemory |
            OperationEffects.MayThrow |
            OperationEffects.Unknown;

        public const OperationEffects PreventsDiscard =
            OperationEffects.WritesStorage |
            OperationEffects.WritesMemory |
            OperationEffects.Calls |
            OperationEffects.MayThrow |
            OperationEffects.ControlFlow |
            OperationEffects.Volatile |
            OperationEffects.Observable |
            OperationEffects.Unknown;

        public static OperationEffects Classify(Op op)
        {
            OperationEffects prefixEffects = op.Prefixes.Any(prefix => prefix.Opcode == OpCodes.Volatile)
                ? OperationEffects.Volatile
                : OperationEffects.None;

            // FlowControl is the runtime's canonical broad classification and keeps every branch
            // spelling out of the opcode switch. jmp is handled as a call as well as a transfer.
            if (op.Opcode == OpCodes.Jmp)
            {
                return prefixEffects |
                    OperationEffects.Calls |
                    OperationEffects.ReadsMemory |
                    OperationEffects.WritesMemory |
                    OperationEffects.MayThrow |
                    OperationEffects.ControlFlow;
            }

            if (op.Opcode.FlowControl == FlowControl.Call)
            {
                OperationEffects effects =
                    OperationEffects.Calls |
                    OperationEffects.ReadsMemory |
                    OperationEffects.WritesMemory |
                    OperationEffects.MayThrow;
                if (op.Opcode == OpCodes.Newobj)
                    effects |= OperationEffects.Allocates;
                return prefixEffects | effects;
            }

            if (op.Opcode.FlowControl == FlowControl.Break)
                return prefixEffects | OperationEffects.Observable;

            if (op.Opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return)
            {
                return prefixEffects | OperationEffects.ControlFlow;
            }

            if (op.Opcode.FlowControl == FlowControl.Throw)
                return prefixEffects | OperationEffects.ControlFlow | OperationEffects.MayThrow;

            OperationEffects opcodeEffects = unchecked((ushort)op.Opcode.Value) switch
            {
                OpCodeValues.Ldarg_0 or OpCodeValues.Ldarg_1 or OpCodeValues.Ldarg_2 or OpCodeValues.Ldarg_3 or
                    OpCodeValues.Ldarg or OpCodeValues.Ldarg_S or
                    OpCodeValues.Ldloc_0 or OpCodeValues.Ldloc_1 or OpCodeValues.Ldloc_2 or OpCodeValues.Ldloc_3 or
                    OpCodeValues.Ldloc or OpCodeValues.Ldloc_S => OperationEffects.ReadsStorage,

                OpCodeValues.Starg or OpCodeValues.Starg_S or
                    OpCodeValues.Stloc_0 or OpCodeValues.Stloc_1 or OpCodeValues.Stloc_2 or OpCodeValues.Stloc_3 or
                    OpCodeValues.Stloc or OpCodeValues.Stloc_S => OperationEffects.WritesStorage,

                OpCodeValues.Ldarga or OpCodeValues.Ldarga_S or
                    OpCodeValues.Ldloca or OpCodeValues.Ldloca_S => OperationEffects.TakesStorageAddress,

                OpCodeValues.Nop or OpCodeValues.Ldnull or
                    OpCodeValues.Ldc_I4_M1 or OpCodeValues.Ldc_I4_0 or OpCodeValues.Ldc_I4_1 or
                    OpCodeValues.Ldc_I4_2 or OpCodeValues.Ldc_I4_3 or OpCodeValues.Ldc_I4_4 or
                    OpCodeValues.Ldc_I4_5 or OpCodeValues.Ldc_I4_6 or OpCodeValues.Ldc_I4_7 or
                    OpCodeValues.Ldc_I4_8 or OpCodeValues.Ldc_I4_S or OpCodeValues.Ldc_I4 or
                    OpCodeValues.Ldc_I8 or OpCodeValues.Ldc_R4 or OpCodeValues.Ldc_R8 or
                    OpCodeValues.Dup or OpCodeValues.Pop or OpCodeValues.Ldstr or OpCodeValues.Ldtoken or
                    OpCodeValues.Ldftn or OpCodeValues.Sizeof or OpCodeValues.Isinst or
                    OpCodeValues.Arglist or OpCodeValues.Mkrefany or OpCodeValues.Refanytype or
                    OpCodeValues.Ceq or OpCodeValues.Cgt or OpCodeValues.Cgt_Un or
                    OpCodeValues.Clt or OpCodeValues.Clt_Un or
                    OpCodeValues.Add or OpCodeValues.Sub or OpCodeValues.Mul or
                    OpCodeValues.And or OpCodeValues.Or or OpCodeValues.Xor or
                    OpCodeValues.Shl or OpCodeValues.Shr or OpCodeValues.Shr_Un or
                    OpCodeValues.Neg or OpCodeValues.Not or
                    OpCodeValues.Conv_I1 or OpCodeValues.Conv_I2 or OpCodeValues.Conv_I4 or
                    OpCodeValues.Conv_I8 or OpCodeValues.Conv_U1 or OpCodeValues.Conv_U2 or
                    OpCodeValues.Conv_U4 or OpCodeValues.Conv_U8 or OpCodeValues.Conv_I or
                    OpCodeValues.Conv_U or OpCodeValues.Conv_R4 or OpCodeValues.Conv_R8 or
                    OpCodeValues.Conv_R_Un => OperationEffects.None,

                OpCodeValues.Div or OpCodeValues.Div_Un or OpCodeValues.Rem or OpCodeValues.Rem_Un or
                    OpCodeValues.Ckfinite or
                    OpCodeValues.Add_Ovf or OpCodeValues.Add_Ovf_Un or
                    OpCodeValues.Sub_Ovf or OpCodeValues.Sub_Ovf_Un or
                    OpCodeValues.Mul_Ovf or OpCodeValues.Mul_Ovf_Un or
                    OpCodeValues.Conv_Ovf_I1 or OpCodeValues.Conv_Ovf_I1_Un or
                    OpCodeValues.Conv_Ovf_I2 or OpCodeValues.Conv_Ovf_I2_Un or
                    OpCodeValues.Conv_Ovf_I4 or OpCodeValues.Conv_Ovf_I4_Un or
                    OpCodeValues.Conv_Ovf_I8 or OpCodeValues.Conv_Ovf_I8_Un or
                    OpCodeValues.Conv_Ovf_U1 or OpCodeValues.Conv_Ovf_U1_Un or
                    OpCodeValues.Conv_Ovf_U2 or OpCodeValues.Conv_Ovf_U2_Un or
                    OpCodeValues.Conv_Ovf_U4 or OpCodeValues.Conv_Ovf_U4_Un or
                    OpCodeValues.Conv_Ovf_U8 or OpCodeValues.Conv_Ovf_U8_Un or
                    OpCodeValues.Conv_Ovf_I or OpCodeValues.Conv_Ovf_I_Un or
                    OpCodeValues.Conv_Ovf_U or OpCodeValues.Conv_Ovf_U_Un or
                OpCodeValues.Castclass or OpCodeValues.Unbox or OpCodeValues.Unbox_Any or
                    OpCodeValues.Refanyval => OperationEffects.MayThrow,

                // A static field access may run its declaring type's initializer. Model that as an
                // unknown call rather than teaching each consumer this CIL-specific exception.
                OpCodeValues.Ldsfld => OperationEffects.ReadsMemory | OperationEffects.WritesMemory |
                    OperationEffects.Calls | OperationEffects.MayThrow,
                OpCodeValues.Ldsflda => OperationEffects.ReadsMemory | OperationEffects.WritesMemory |
                    OperationEffects.TakesMemoryAddress | OperationEffects.Calls | OperationEffects.MayThrow,
                OpCodeValues.Stsfld => OperationEffects.ReadsMemory | OperationEffects.WritesMemory |
                    OperationEffects.Calls | OperationEffects.MayThrow,

                OpCodeValues.Ldflda or OpCodeValues.Ldelema =>
                    OperationEffects.TakesMemoryAddress | OperationEffects.MayThrow,

                OpCodeValues.Ldind_I1 or OpCodeValues.Ldind_U1 or OpCodeValues.Ldind_I2 or
                    OpCodeValues.Ldind_U2 or OpCodeValues.Ldind_I4 or OpCodeValues.Ldind_U4 or
                    OpCodeValues.Ldind_I8 or OpCodeValues.Ldind_I or OpCodeValues.Ldind_R4 or
                    OpCodeValues.Ldind_R8 or OpCodeValues.Ldind_Ref or OpCodeValues.Ldobj or
                    OpCodeValues.Ldfld or OpCodeValues.Ldlen or
                    OpCodeValues.Ldelem_I1 or OpCodeValues.Ldelem_U1 or OpCodeValues.Ldelem_I2 or
                    OpCodeValues.Ldelem_U2 or OpCodeValues.Ldelem_I4 or OpCodeValues.Ldelem_U4 or
                    OpCodeValues.Ldelem_I8 or OpCodeValues.Ldelem_I or OpCodeValues.Ldelem_R4 or
                    OpCodeValues.Ldelem_R8 or OpCodeValues.Ldelem_Ref or OpCodeValues.Ldelem or
                    OpCodeValues.Ldvirtftn => OperationEffects.ReadsMemory | OperationEffects.MayThrow,

                OpCodeValues.Stind_Ref or OpCodeValues.Stind_I1 or OpCodeValues.Stind_I2 or
                    OpCodeValues.Stind_I4 or OpCodeValues.Stind_I8 or OpCodeValues.Stind_I or OpCodeValues.Stind_R4 or
                    OpCodeValues.Stind_R8 or OpCodeValues.Stobj or OpCodeValues.Stfld or
                    OpCodeValues.Initobj or OpCodeValues.Initblk or
                    OpCodeValues.Stelem_I or OpCodeValues.Stelem_I1 or OpCodeValues.Stelem_I2 or
                    OpCodeValues.Stelem_I4 or OpCodeValues.Stelem_I8 or OpCodeValues.Stelem_R4 or
                    OpCodeValues.Stelem_R8 or OpCodeValues.Stelem_Ref or OpCodeValues.Stelem =>
                    OperationEffects.WritesMemory | OperationEffects.MayThrow,

                OpCodeValues.Cpobj or OpCodeValues.Cpblk =>
                    OperationEffects.ReadsMemory | OperationEffects.WritesMemory | OperationEffects.MayThrow,

                OpCodeValues.Box or OpCodeValues.Newarr or OpCodeValues.Localloc =>
                    OperationEffects.Allocates | OperationEffects.MayThrow,

                _ => UnknownEffects,
            };

            return prefixEffects | opcodeEffects;
        }
    }
}
