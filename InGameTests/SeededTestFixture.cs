using System.Reflection;
using DevTools.Testing;
using Verse;

namespace InGameTests;

public abstract class SeededTestFixture
{
    private static readonly PropertyInfo randomState = typeof(Rand)
        .GetProperty("StateCompressed", BindingFlags.Static | BindingFlags.NonPublic)!;

    [SetUp]
    [ExecutionPriority(int.MaxValue)]
    public void ResetRandomSeed()
    {
        // Reset before other setup methods, including pawn generation. Copy the whole state so
        // the draw counter resets too, without keeping a RandBlock open across coroutine frames.
        object state;
        using (new RandBlock(1234567))
            state = randomState.GetValue(null)!;
        randomState.SetValue(null, state);
    }
}
