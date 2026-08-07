namespace Disharmony.Tests;

public abstract class PatchTestBase
{
    [SetUp]
    public void UnpatchBeforeTest()
    {
        HarmonyInterface.Instance.optimizerEnabled = false;
        Patcher.UnpatchAll(typeof(PatchTestBase).Assembly);
        Patcher.UnpatchAll(typeof(StaticMethodTargets).Assembly);
    }

    protected static void ApplyPatch(Type patchMethodsType, string patchMethodName) =>
        Patcher.Patch(patchMethodsType.GetMethod(patchMethodName));
}
