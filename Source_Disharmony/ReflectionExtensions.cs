namespace Disharmony;

public static class ReflectionExtensions
{
    extension(MemberInfo member)
    {
        public string FullName => $"{member.DeclaringType?.FullName}::{member.Name}";
    }

    extension(MethodInfo method)
    {
        public MethodInfo? GetIteratorImplementation()
        {
            // Check if the method is an iterator state machine wrapper. If so, look at the iterator's MoveNext method.
            Type? stateMachineType = method.GetCustomAttribute<IteratorStateMachineAttribute>()?.StateMachineType;
            return stateMachineType?.GetMethod("MoveNext", AccessTools.all);
        }
    }
}
