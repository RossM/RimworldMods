using static Xylib.Tests.TestDefOf;

namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class EventManagerTests : SeededTestFixture, IEventListener
{
    private EventManager manager = null!;
    private readonly List<string> calls = [];
    private Thing? receivedTarget;
    private string? receivedData;
    private int unregistered;

    [SetUp]
    public void SetUp()
    {
        manager = new EventManager();
        calls.Clear();
        receivedTarget = null;
        receivedData = null;
        unregistered = 0;
    }

    [Test]
    public void TargetedAndGlobalCallbacksReceiveOnlyMatchingTargets()
    {
        var first = new Thing();
        var second = new Thing();
        manager.Register(XylibTestSignal, null, Global);
        manager.Register(XylibTestSignal, first, Local);
        manager.Notify(XylibTestSignal, second);
        Expect.AreEqual("global", string.Join(",", calls));
        calls.Clear();
        manager.Notify(XylibTestSignal, first);
        Expect.AreEqual("global,local", string.Join(",", calls));
    }

    [Test]
    public void HigherPriorityRunsBeforeGlobalAndLocalLowerPriorities()
    {
        var target = new Thing();
        manager.Register(XylibTestSignal, null, Global, -5);
        manager.Register(XylibTestSignal, target, Local, 5);
        manager.Notify(XylibTestSignal, target);
        Expect.AreEqual("local,global", string.Join(",", calls));
    }

    [Test]
    [WarningsAllowed(@"^\[EventManager\] Adding a duplicate callback: eventDef=XylibTestSignal global listener=Xylib.Tests.EventManagerTests name=Xylib.Tests.EventManagerTests.Global$")]
    public void RegisteringSameCallbackTwiceDeliversOnce()
    {
        manager.Register(XylibTestSignal, null, Global);
        manager.Register(XylibTestSignal, null, Global);
        manager.Notify(XylibTestSignal, new Thing());
        Expect.AreEqual(1, calls.Count);
    }

    [Test]
    public void TypedCallbacksReceiveTargetAndData()
    {
        var target = new Thing();
        manager.Register<string>(XylibTestTyped, target, TypedWithTarget);
        manager.Register<string>(XylibTestTyped, target, TypedData);
        manager.Notify(XylibTestTyped, target, "payload");
        Expect.ReferencesAreEqual(target, receivedTarget);
        Expect.AreEqual("payload", receivedData);
        Expect.AreEqual("typed,targetless", string.Join(",", calls));
    }

    [Test]
    public void TargetOnlyCallbackReceivesEventTarget()
    {
        var target = new Thing();
        manager.Register(XylibTestSignal, null, TargetOnly);
        manager.Notify(XylibTestSignal, target);
        Expect.ReferencesAreEqual(target, receivedTarget);
    }

    [Test]
    public void ExplicitListenerOwnsLambdaRegistration()
    {
        manager.Register<string>(this, XylibTestTyped, null, (target, data) => receivedData = data);
        manager.Notify(XylibTestTyped, new Thing(), "first");
        Expect.AreEqual("first", receivedData);
        manager.RemoveListener(this);
        manager.Notify(XylibTestTyped, new Thing(), "second");
        Expect.AreEqual("first", receivedData);
        Expect.AreEqual(1, unregistered);
    }

    [Test]
    public void RemovingListenerRemovesAllItsGlobalAndTargetedCallbacks()
    {
        var target = new Thing();
        manager.AddListener(this);
        manager.Register(XylibTestSignal, target, Local);
        manager.RemoveListener(this);
        manager.Notify(XylibTestSignal, target);
        Expect.IsEmpty(calls);
        Expect.AreEqual(1, unregistered);
    }

    [Test]
    public void ResetRemovesOrdinaryRegistrations()
    {
        manager.AddListener(this);
        manager.Reset();
        manager.Notify(XylibTestSignal, new Thing());
        Expect.IsEmpty(calls);
        manager.AddListener(this);
        manager.Notify(XylibTestSignal, new Thing());
        Expect.AreEqual("global", string.Join(",", calls));
    }

    [Test]
    public void NotificationsDuringScribeRequireExplicitOptIn()
    {
        manager.Register(XylibTestSignal, null, Global);
        manager.Register(XylibTestDuringScribe, null, Local);
        LoadSaveMode original = Scribe.mode;
        try
        {
            Scribe.mode = LoadSaveMode.Saving;
            manager.Notify(XylibTestSignal, new Thing());
            manager.Notify(XylibTestDuringScribe, new Thing());
        }
        finally { Scribe.mode = original; }
        Expect.AreEqual("local", string.Join(",", calls));
    }

    public void RegisterWith(EventManager events) => events.Register(XylibTestSignal, null, Global);
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
