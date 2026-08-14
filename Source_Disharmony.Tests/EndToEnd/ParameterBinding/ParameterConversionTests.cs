namespace Disharmony.Tests.EndToEnd.ParameterBinding;

public static class ParameterConversionPatches
{
    public static object? ObjectObserved;
    public static int? NullableObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Prefix_ValueTypeParameter_Object(object value) => ObjectObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Prefix_ValueTypeParameter_Nullable(int? value) => NullableObserved = value;
}

[TestFixture]
public sealed class ParameterConversionTests : PatchTestBase
{
    [Test]
    public void Prefix_ValueTypeParameter_Object()
    {
        ParameterConversionPatches.ObjectObserved = null;
        ApplyPatch(
            typeof(ParameterConversionPatches),
            nameof(ParameterConversionPatches.Prefix_ValueTypeParameter_Object));

        StaticMethodTargets.IntArgument(42);

        Assert.That(ParameterConversionPatches.ObjectObserved, Is.TypeOf<int>());
        Assert.That(ParameterConversionPatches.ObjectObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ValueTypeParameter_Nullable()
    {
        ParameterConversionPatches.NullableObserved = null;
        ApplyPatch(
            typeof(ParameterConversionPatches),
            nameof(ParameterConversionPatches.Prefix_ValueTypeParameter_Nullable));

        StaticMethodTargets.IntArgument(42);

        Assert.That(ParameterConversionPatches.NullableObserved, Is.EqualTo((int?)42));
    }
}
