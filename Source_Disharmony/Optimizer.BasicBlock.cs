namespace Disharmony;

internal partial class Optimizer
{
    internal class BasicBlock : Block
    {
        public readonly List<Op> ops = [];

        public void SymbolicExecute(List<Type> parameterTypes)
        {
            List<Type> locals = [.. entryLocals];
            List<Type> stack = [.. entryStack];

            foreach (var op in ops)
            {
                switch (unchecked((ushort)op.Opcode.Value))
                {
                    case OpCodeValues.Ldloc_0:
                    case OpCodeValues.Ldloc_1:
                    case OpCodeValues.Ldloc_2:
                    case OpCodeValues.Ldloc_3:
                    case OpCodeValues.Ldloc:
                    case OpCodeValues.Ldloc_S:
                    {
                        int index = op.Index;
                        ExpandLocals(index);
                        stack.Add(locals[index]);
                        break;
                    }
                    case OpCodeValues.Stloc_0:
                    case OpCodeValues.Stloc_1:
                    case OpCodeValues.Stloc_2:
                    case OpCodeValues.Stloc_3:
                    case OpCodeValues.Stloc:
                    case OpCodeValues.Stloc_S:
                    {
                        int index = op.Index;
                        ExpandLocals(index);
                        locals[index] = stack[^1];
                        stack.RemoveAt(stack.Count - 1);
                        break;
                    }
                    case OpCodeValues.Ldloca:
                    case OpCodeValues.Ldloca_S:
                    {
                        int index = op.Index;
                        ExpandLocals(index);
                        stack.Add(ToRef(locals[index]));
                        // Can't be bothered to do fancy analysis here
                        if (!locals[index].IsValueType)
                            locals[index] = typeof(object);
                        break;
                    }
                    case OpCodeValues.Ldarg_0:
                    case OpCodeValues.Ldarg_1:
                    case OpCodeValues.Ldarg_2:
                    case OpCodeValues.Ldarg_3:
                    case OpCodeValues.Ldarg:
                    case OpCodeValues.Ldarg_S:
                    {
                        int index = op.Index;
                        stack.Add(parameterTypes[index]);
                        break;
                    }
                    case OpCodeValues.Ldarga:
                    case OpCodeValues.Ldarga_S:
                    {
                        int index = op.Index;
                        stack.Add(ToRef(parameterTypes[index]));
                        break;
                    }
                    case OpCodeValues.Dup:
                    {
                        stack.Add(stack[^1]);
                        break;
                    }
                    case OpCodeValues.Ldobj:
                    {
                        stack[^1] = FromRef(stack[^1]);
                        break;
                    }
                    case OpCodeValues.Ldstr:
                    {
                        stack.Add(typeof(string));
                        break;
                    }
                    case OpCodeValues.Ldfld when op.Operand is FieldInfo field:
                    {
                        stack[^1] = field.FieldType;
                        break;
                    }
                    case OpCodeValues.Ldflda when op.Operand is FieldInfo field:
                    {
                        stack[^1] = ToRef(field.FieldType);
                        break;
                    }
                    case OpCodeValues.Ldsfld when op.Operand is FieldInfo field:
                    {
                        stack.Add(field.FieldType);
                        break;
                    }
                    case OpCodeValues.Ldsflda when op.Operand is FieldInfo field:
                    {
                        stack.Add(ToRef(field.FieldType));
                        break;
                    }
                    case OpCodeValues.NewObj when op.Operand is ConstructorInfo constructor:
                    {
                        var count = constructor.GetParameters().Length;
                        for (int i = 0; i < count; i++)
                            stack.RemoveAt(stack.Count - 1);
                        stack.Add(constructor.DeclaringType);
                        break;
                    }
                    default:
                    {
                        int popCount = op.StackPops;
                        for (int i = 0; i < popCount; i++)
                            stack.RemoveAt(stack.Count - 1);

                        switch (op.Opcode.StackBehaviourPush)
                        {
                            case StackBehaviour.Push0: break;
                            case StackBehaviour.Push1: stack.Add(typeof(AnyType)); break;
                            case StackBehaviour.Push1_push1:
                                stack.Add(typeof(AnyType));
                                stack.Add(typeof(AnyType));
                                break;
                            case StackBehaviour.Pushi: stack.Add(typeof(AnyType)); break;
                            case StackBehaviour.Pushi8: stack.Add(typeof(long)); break;
                            case StackBehaviour.Pushr4: stack.Add(typeof(float)); break;
                            case StackBehaviour.Pushr8: stack.Add(typeof(double)); break;
                            case StackBehaviour.Pushref: stack.Add(typeof(object)); break;
                            case StackBehaviour.Varpush when op.Operand is MethodInfo methodInfo:
                            {
                                if (methodInfo.ReturnType != typeof(void))
                                    stack.Add(methodInfo.ReturnType);
                                break;
                            }
                            default: throw new ArgumentException();
                        }

                        break;
                    }
                }
            }

            if (ops is [.., { ClearsStack: true }])
                stack = [];

            exitLocals = locals;
            exitStack = stack;
            return;

            void ExpandLocals(int i)
            {
                while (locals.Count < i + 1)
                    locals.Add(typeof(UnknownType));
            }
        }

        private static bool IsSpecialType(Type type) => type == typeof(AnyType) || type == typeof(UnknownType);

        private static Type ToRef(Type type) => IsSpecialType(type) ? type : type.MakeByRefType();

        private static Type FromRef(Type type) => IsSpecialType(type) ? type : type.GetElementType() ?? throw new InvalidOperationException();
    }
}
