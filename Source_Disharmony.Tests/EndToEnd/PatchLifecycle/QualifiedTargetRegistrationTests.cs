namespace Disharmony.Tests.EndToEnd.PatchLifecycle;

[Patch]
[Category("qualified-target-colon")]
public static class QualifiedColonTargetPatches
{
    [Postfix]
    [Target("Disharmony.Tests.StaticMethodTargets:RegistrationResultA")]
    public static void Postfix(ref int __result) => __result = 42;
}

[Patch]
[Category("qualified-target-dot")]
public static class QualifiedDotTargetPatches
{
    [Postfix]
    [Target("Disharmony.Tests.StaticMethodTargets.RegistrationResultA")]
    public static void Postfix(ref int __result) => __result = 42;
}

[Patch]
[Category("qualified-targets-colon")]
public static class QualifiedColonTargetsPatches
{
    [Postfix]
    [Targets("Disharmony.Tests.StaticMethodTargets:RegistrationResultA")]
    public static void Postfix(ref int __result) => __result = 42;
}

[Patch]
[Category("qualified-targets-dot")]
public static class QualifiedDotTargetsPatches
{
    [Postfix]
    [Targets("Disharmony.Tests.StaticMethodTargets.RegistrationResultA")]
    public static void Postfix(ref int __result) => __result = 42;
}

[TestFixture]
public sealed class QualifiedTargetRegistrationTests : PatchTestBase
{
    [TestCase("qualified-target-colon")]
    [TestCase("qualified-target-dot")]
    [TestCase("qualified-targets-colon")]
    [TestCase("qualified-targets-dot")]
    public void PatchCategoryResolvesQualifiedNameWithoutDefaultType(string category)
    {
        Patcher.PatchCategory(typeof(QualifiedTargetRegistrationTests).Assembly, category);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
    }

    [Test]
    public void PatchAllResolvesQualifiedNamesWithoutDefaultTypes()
    {
        Patcher.PatchAll(typeof(QualifiedTargetRegistrationTests).Assembly);

        Assert.That(StaticMethodTargets.RegistrationResultA(), Is.EqualTo(42));
    }
}
