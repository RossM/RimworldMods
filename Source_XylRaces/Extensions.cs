using System.Reflection;

namespace XylXenos;

public static class Extensions
{
    extension(MethodInfo method)
    {
        public T CreateDelegate<T>() where T : Delegate
        {
            return (T)method.CreateDelegate(typeof(T));
        }
    }
}
