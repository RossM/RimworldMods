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
