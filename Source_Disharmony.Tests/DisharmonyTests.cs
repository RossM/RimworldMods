using NUnit.Framework;

namespace Disharmony.Tests;

public static class PatchMethods
{
    [Prefix]
    [Target(typeof(PatchTargets), nameof(PatchTargets.PrefixTarget1))]
    public static void PrefixPatch1() {}
}

public static class PatchTargets
{
    public static void PrefixTarget1() {}
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
}
