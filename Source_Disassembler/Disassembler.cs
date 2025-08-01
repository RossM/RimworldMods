using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace XylDisassembler;

public class Instruction
{
    public int ByteIndex;
    public int Length;
    public OpCode OpCode;
    public object Value;
}

public class Method
{
    public List<Instruction> Instructions = [];
}

public class Disassembler
{
    private static readonly Dictionary<int, OpCode> opCodeByValue = new();
    private static readonly HashSet<int> twoBytePrefixes = [];

    static Disassembler()
    {
        var type = typeof(OpCodes);
        foreach (var field in type.GetFields())
        {
            var opCode = (OpCode)(field.GetValue(null) ?? throw new InvalidOperationException());
            opCodeByValue.Add((UInt16)opCode.Value, opCode);
            if (opCode.Size == 2)
                twoBytePrefixes.Add((UInt16)opCode.Value >> 8);
        }
    }

    public Method Decode(MethodInfo methodInfo)
    {
        MethodBody methodBody = methodInfo.GetMethodBody();
        if (methodBody == null)
        {
            return default;
        }

        var il = methodBody.GetILAsByteArray();
        var module = methodInfo.Module;

        List<Instruction> instructions = [];

        for (int curByte = 0; curByte < il.Length;)
        {
            int startByte = curByte;

            int value = il[curByte++];
            if (twoBytePrefixes.Contains(value))
                value = value << 8 | il[curByte++];

            var opcode = opCodeByValue[value];

            object operandValue = opcode.OperandType switch
            {
                // 0 bytes
                OperandType.InlineNone => 0,

                // 1 byte
                OperandType.ShortInlineBrTarget => curByte + 1 + il[curByte],
                OperandType.ShortInlineI => (int)il[curByte],
                OperandType.ShortInlineVar => (int)il[curByte],

                // 2 bytes
                OperandType.InlineVar => (int)BitConverter.ToInt16(il, curByte),

                // 4 bytes
                OperandType.InlineType => module.ResolveType(BitConverter.ToInt32(il, curByte)),
                OperandType.InlineField => module.ResolveField(BitConverter.ToInt32(il, curByte)),
                OperandType.InlineMethod => module.ResolveMethod(BitConverter.ToInt32(il, curByte)),
                OperandType.InlineString => module.ResolveString(BitConverter.ToInt32(il, curByte)),
                OperandType.InlineTok => module.ResolveType(BitConverter.ToInt32(il, curByte)),
                OperandType.InlineBrTarget => curByte + 4 + BitConverter.ToInt32(il, curByte),
                OperandType.ShortInlineR => BitConverter.ToSingle(il, curByte),

                // 8 bytes
                OperandType.InlineR => BitConverter.ToDouble(il, curByte),
                OperandType.InlineI8 => BitConverter.ToInt64(il, curByte),

                _ => BitConverter.ToInt32(il, curByte)
            };

            curByte += opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget => 1,
                OperandType.ShortInlineI => 1,
                OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 => 8,
                OperandType.InlineR => 8,
                _ => 4,
            };

            instructions.Add(new()
            {
                OpCode = opcode,
                Value = operandValue,
                ByteIndex = startByte,
                Length = curByte - startByte,
            });
        }

        return new Method() { Instructions = instructions };
    }
}