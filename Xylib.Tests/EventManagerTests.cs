namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class EventManagerTests : SeededTestFixture, IEventListener
{
    private EventManager manager = null!;
    private readonly EventDef signal = new() { defName = "TestSignal" };
    private readonly EventDef typed = new() { defName = "TestTyped", dataType = typeof(string) };
    private readonly EventDef duringScribe = new() { defName = "TestDuringScribe", allowDuringScribe = true };
    private readonly List<string> calls = [];
    private Thing? receivedTarget;
    private string? receivedData;
    private int unregistered;

    [SetUp]
    public void SetUp()
    {
        // Use distinct event indices so Reset's built-in static listeners cannot
        // receive the synthetic events in this isolated manager.
        DefDatabase<EventDef>.Add(signal);
        DefDatabase<EventDef>.Add(typed);
        DefDatabase<EventDef>.Add(duringScribe);
        manager = new EventManager();
        calls.Clear();
        receivedTarget = null;
        receivedData = null;
        unregistered = 0;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (EventDef def in new[] { duringScribe, typed, signal })
            typeof(DefDatabase<EventDef>).GetMethod("Remove", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.Invoke(null, [def]);
    }

    [Test]
    public void TargetedAndGlobalCallbacksReceiveOnlyMatchingTargets()
    {
        var first = new Thing();
        var second = new Thing();
        manager.Register(signal, null, Global);
        manager.Register(signal, first, Local);
        manager.Notify(signal, second);
        Expect.AreEqual("global", string.Join(",", calls));
        calls.Clear();
        manager.Notify(signal, first);
        Expect.AreEqual("global,local", string.Join(",", calls));
    }

    [Test]
    public void HigherPriorityRunsBeforeGlobalAndLocalLowerPriorities()
    {
        var target = new Thing();
        manager.Register(signal, null, Global, -5);
        manager.Register(signal, target, Local, 5);
        manager.Notify(signal, target);
        Expect.AreEqual("local,global", string.Join(",", calls));
    }

    [Test]
    [WarningsAllowed(@"^\[EventManager\] Adding a duplicate callback: eventDef=TestSignal global listener=Xylib.Tests.EventManagerTests name=Xylib.Tests.EventManagerTests.Global$")]
    public void RegisteringSameCallbackTwiceDeliversOnce()
    {
        manager.Register(signal, null, Global);
        manager.Register(signal, null, Global);
        manager.Notify(signal, new Thing());
        Expect.AreEqual(1, calls.Count);
    }

    [Test]
    public void TypedCallbacksReceiveTargetAndData()
    {
        var target = new Thing();
        manager.Register<string>(typed, target, TypedWithTarget);
        manager.Register<string>(typed, target, TypedData);
        manager.Notify(typed, target, "payload");
        Expect.ReferencesAreEqual(target, receivedTarget);
        Expect.AreEqual("payload", receivedData);
        Expect.AreEqual("typed,targetless", string.Join(",", calls));
    }

    [Test]
    public void TargetOnlyCallbackReceivesEventTarget()
    {
        var target = new Thing();
        manager.Register(signal, null, TargetOnly);
        manager.Notify(signal, target);
        Expect.ReferencesAreEqual(target, receivedTarget);
    }

    [Test]
    public void ExplicitListenerOwnsLambdaRegistration()
    {
        manager.Register<string>(this, typed, null, (target, data) => receivedData = data);
        manager.Notify(typed, new Thing(), "first");
        Expect.AreEqual("first", receivedData);
        manager.RemoveListener(this);
        manager.Notify(typed, new Thing(), "second");
        Expect.AreEqual("first", receivedData);
        Expect.AreEqual(1, unregistered);
    }

    [Test]
    public void RemovingListenerRemovesAllItsGlobalAndTargetedCallbacks()
    {
        var target = new Thing();
        manager.AddListener(this);
        manager.Register(signal, target, Local);
        manager.RemoveListener(this);
        manager.Notify(signal, target);
        Expect.IsEmpty(calls);
        Expect.AreEqual(1, unregistered);
    }

    [Test]
    public void ResetRemovesOrdinaryRegistrations()
    {
        manager.AddListener(this);
        manager.Reset();
        manager.Notify(signal, new Thing());
        Expect.IsEmpty(calls);
        manager.AddListener(this);
        manager.Notify(signal, new Thing());
        Expect.AreEqual("global", string.Join(",", calls));
    }

    [Test]
    public void NotificationsDuringScribeRequireExplicitOptIn()
    {
        manager.Register(signal, null, Global);
        manager.Register(duringScribe, null, Local);
        LoadSaveMode original = Scribe.mode;
        try
        {
            Scribe.mode = LoadSaveMode.Saving;
            manager.Notify(signal, new Thing());
            manager.Notify(duringScribe, new Thing());
        }
        finally { Scribe.mode = original; }
        Expect.AreEqual("local", string.Join(",", calls));
    }

    public void RegisterWith(EventManager events) => events.Register(signal, null, Global);
    public void PreUnregister(EventManager events) => unregistered++;
    private void Global() => calls.Add("global");
    private void Local() => calls.Add("local");
    private void TargetOnly(Thing? target) => receivedTarget = target;
    private void TypedWithTarget(Thing? target, string data)
    {
        receivedTarget = target;
        receivedData = data;
        calls.Add("typed");
    }
    private void TypedData(string data)
    {
        receivedData = data;
        calls.Add("targetless");
    }
}
