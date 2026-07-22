namespace Disharmony.Tests;

public abstract class PatchTestBase
{
    [SetUp]
    public void UnpatchBeforeTest() =>
        Autopatcher.UnpatchAll(typeof(PatchTestBase).Assembly);

    protected static void ApplyPatch(Type patchMethodsType, string patchMethodName) =>
        Autopatcher.Patch(patchMethodsType.GetMethod(patchMethodName));
}
