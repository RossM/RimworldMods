namespace Xylib;

public static class ReflectionHelpers
{
    // This is necessary because MethodInfo.GetBaseDefinition is broken for the version of Mono used in Unity
    public static bool HasOverridingMethod(Type childType, Type baseType, string methodName)
    {
        for (Type type = childType; type != null && type != baseType; type = type.BaseType)
        {
            if (type.GetMethod(methodName) != null)
                return true;
        }

        return false;
    }
}
