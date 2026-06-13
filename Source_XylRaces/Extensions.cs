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

    extension<T>(T obj)
    {
        public T MemberwiseClone()
        {
            if (memberwiseCloneFn == null)
            {
                var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
                memberwiseCloneFn = method.CreateDelegate<Func<object, object>>();
            }

            return (T)memberwiseCloneFn(obj);
        }
    }

    private static Func<object, object> memberwiseCloneFn;
}
