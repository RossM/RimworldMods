using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace Source_ExposableChecker
{
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
                Log.Message($"{methodInfo.DeclaringType?.Namespace ?? "<Unknown>"}.{methodInfo.DeclaringType?.Name ?? "<Unknown>"}.{methodInfo.Name}: No method body");
                return new Method() { Instructions = [] };
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

                var operandLength = opcode.OperandType switch
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

                var operandBytes = il.Skip(curByte).Take(operandLength).ToArray();
                curByte += operandLength;

                var operandInt64 = operandLength switch
                {
                    0 => 0,
                    1 => operandBytes[0],
                    2 => BitConverter.ToInt16(operandBytes, 0),
                    4 => BitConverter.ToInt32(operandBytes, 0),
                    8 => BitConverter.ToInt64(operandBytes, 0),
                    _ => throw new InvalidOperationException(),
                };

                object operandValue = opcode.OperandType switch
                {
                    OperandType.InlineType => module.ResolveType((int)operandInt64),
                    OperandType.InlineField => module.ResolveField((int)operandInt64),
                    OperandType.InlineMethod => module.ResolveMethod((int)operandInt64),
                    OperandType.InlineString => module.ResolveString((int)operandInt64),
                    OperandType.InlineR => BitConverter.ToDouble(operandBytes, 0),
                    OperandType.ShortInlineR => BitConverter.ToSingle(operandBytes, 0),
                    OperandType.InlineTok => module.ResolveType((int)operandInt64),
                    OperandType.InlineBrTarget => curByte + (int)operandInt64,
                    OperandType.ShortInlineBrTarget => curByte + (int)operandInt64,
                    _ => (int)operandInt64
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

    [UsedImplicitly]
    [StaticConstructorOnStartup]
    public class Main(ModContentPack content) : Mod(content)
    {
        private static readonly Disassembler Disassembler = new();

        public static void Check(Type type)
        {
            var fields = type.GetFields().Where(
                field => field.GetCustomAttribute<UnsavedAttribute>() == null &&
                         !field.Attributes.HasFlag(FieldAttributes.InitOnly) &&
                         !field.Attributes.HasFlag(FieldAttributes.Literal) &&
                         !field.Attributes.HasFlag(FieldAttributes.Static) &&
                         field.DeclaringType == type).ToList();

            if (fields.Count == 0)
                return;

            HashSet<FieldInfo> usedFields = [];

            MethodInfo curMethod = type.GetMethod("ExposeData");
            if (curMethod != null)
            {
                var method = Disassembler.Decode(curMethod);
                usedFields.AddRange(method.Instructions.Select(i => i.Value).OfType<FieldInfo>());
            }

            foreach (var field in fields.Except(usedFields))
            {
                Log.Warning($"Possibly unsaved field ({type.Assembly.GetName().Name}) {type.Namespace ?? "<Unknown>"}.{type.Name}.{field.Name}. Either save this field in ExposeData, mark it [Unsaved], or make it const or readonly or static.");
            }
        }

        [UsedImplicitly]
        static Main()
        {
            HashSet<Assembly> checkedAssemblies = [];

            foreach (Type type in GenTypes.AllTypes.Where(IsIExposable))
            {
                string assemblyName = type.Assembly.GetName().Name;
                bool skipAssembly = assemblyName == "Assembly-CSharp";
                if (!checkedAssemblies.Contains(type.Assembly))
                {
                    Log.Message(skipAssembly
                        ? $"IExposable checker: skipping {assemblyName}"
                        : $"IExposable checker: checking {assemblyName}");
                    checkedAssemblies.Add(type.Assembly);
                }

                if (!skipAssembly) 
                    Check(type);
            }
        }

        private static bool IsIExposable(Type t)
        {
            return t.FindInterfaces((type, criteria) => type == (Type)criteria, typeof(IExposable)).Any();
        }
    }
}
