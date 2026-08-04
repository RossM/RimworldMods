namespace Disharmony;

public static class ReflectionExtensions
{
    extension(MemberInfo member)
    {
        public string FullName => $"{member.DeclaringType?.FullName}::{member.Name}";
    }

    extension(MethodBase method)
    {
        public MethodInfo? GetIteratorImplementation()
        {
            // Check if the method is an iterator state machine wrapper. If so, look at the iterator's MoveNext method.
            Type? stateMachineType = method.GetCustomAttribute<IteratorStateMachineAttribute>()?.StateMachineType;
            return stateMachineType?.GetMethod("MoveNext", AccessTools.all);
        }

        public bool HasThis => (method.CallingConvention & CallingConventions.HasThis) != 0;
    }

    extension(Type type)
    {
        public bool IsClosureType
        {
            get
            {
                type = type.NoRefType;
                return type.Name.StartsWith("<>c") && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute));
            }
        }

        public bool IsPointerLike => type.IsByRef || type.IsPointer;
        public bool IsPointerCompatibleNumeric => type == typeof(int) || type == typeof(uint) || type == typeof(IntPtr) || type == typeof(UIntPtr);


        public Type NoRefType => type.IsByRef ? type.GetElementType() : type;

        public Type CallableType => type.IsValueType ? type.MakeByRefType() : type;
    }

    extension(ILGenerator generator)
    {
        internal void Emit(OpCode opCode, MethodBaseInvocation invocation)
        {
            switch (invocation)
            {
                case MethodInvocation method: generator.Emit(opCode, method.MethodInfo); break;
                case ConstructorInvocation constructor: generator.Emit(opCode, constructor.ConstructorInfo); break;
                default: throw new ArgumentOutOfRangeException(nameof(invocation));
            }
        }
    }
}
