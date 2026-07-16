using NUnit.Framework;

namespace Disharmony.Tests;

public static class PatchMethods
{
    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.PrefixTarget1))]
    public static void PrefixPatch1() {}

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.PrefixTarget2))]
    public static bool PrefixPatch2()
    {
        return true;
    }

    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.PrefixTarget3))]
    public static bool PrefixPatch3()
    {
        return false;
    }
}

public static class PatchTargets
{
    public static void PrefixTarget1() {}

    public static int PrefixTarget2()
    {
        return 1;
    }

    public static int PrefixTarget3()
    {
        Assert.Fail();
        return 1;
    }
}

[TestFixture]
public sealed class DisharmonyTests
{
    [Test]
    public void PrefixTest1()
    {
        Autopatcher.Patch(typeof(PatchMethods).GetMethod(nameof(PatchMethods.PrefixPatch1)));
        PatchTargets.PrefixTarget1();
    }

    [Test]
    public void PrefixTest2()
    {
        Autopatcher.Patch(typeof(PatchMethods).GetMethod(nameof(PatchMethods.PrefixPatch2)));
        int result = PatchTargets.PrefixTarget2();
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void PrefixTest3()
    {
        Autopatcher.Patch(typeof(PatchMethods).GetMethod(nameof(PatchMethods.PrefixPatch3)));
        int result = PatchTargets.PrefixTarget3();
        Assert.That(result, Is.EqualTo(0));
    }
}
