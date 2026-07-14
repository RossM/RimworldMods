namespace Disharmony;

public static class ReflectionExtensions
{
    extension(MemberInfo member)
    {
        public string FullName => $"{member.DeclaringType?.FullName}::{member.Name}";
    }
}
