namespace Disharmony.Tests.EndToEnd.ParameterBinding;

public static class MemberInfoBindingPatches
{
    [Prefix]
    [Target(typeof(MemberInfoGenericTargets<int>), nameof(MemberInfoGenericTargets<int>.StaticIdentity))]
    public static void Prefix_Method_ClosedGenericType_Static([MemberInfo] MethodInfo member) => Observed = member;

    [Postfix]
    [Target(typeof(MemberInfoGenericTargets<string>), nameof(MemberInfoGenericTargets<string>.Identity))]
    public static void Postfix_Method_ClosedGenericType_Instance([MemberInfo] MethodInfo member) => Observed = member;

    [Prefix]
    [Inner(typeof(MemberInfoGenericTargets<int>), nameof(MemberInfoGenericTargets<int>.Identity))]
    [Target(typeof(MemberInfoGenericTargets<int>), nameof(MemberInfoGenericTargets<int>.CallIdentity))]
    public static void InnerPrefix_Method_ClosedGenericType_InnerScope([MemberInfo(Scope.Inner)] MethodInfo member) => Observed = member;

    [Postfix]
    [Inner(typeof(MemberInfoGenericTargets<string>), nameof(MemberInfoGenericTargets<string>.Identity))]
    [Target(typeof(MemberInfoGenericTargets<string>), nameof(MemberInfoGenericTargets<string>.CallIdentity))]
    public static void InnerPostfix_Method_ClosedGenericType_OuterScope([MemberInfo(Scope.Outer)] MethodInfo member) => Observed = member;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_Method_TypedMethodInfo([MemberInfo] MethodInfo member) => Observed = member;

    [Prefix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [typeof(int)])]
    public static void Prefix_Constructor_TypedConstructorInfo([MemberInfo] ConstructorInfo member) => Observed = member;

    [Prefix]
    [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), memberType: MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.FieldResult))]
    public static void InnerPrefix_Field_TypedFieldInfo([MemberInfo(Scope.Inner)] FieldInfo member) => Observed = member;

    public static MemberInfo? Observed;
    public static MemberInfo? InnerObserved;
    public static MemberInfo? OuterObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_Method_Static(
        [MemberInfo] MemberInfo member)
    {
        Observed = member;
    }

    [Postfix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static void Postfix_Method_Instance(
        [MemberInfo] MemberInfo member)
    {
        Observed = member;
    }

    [Prefix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [typeof(int)])]
    public static void Prefix_Constructor(
        [MemberInfo] MemberInfo member)
    {
        Observed = member;
    }

    [Postfix]
    [Target(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [typeof(int)])]
    public static void Postfix_Constructor(
        [MemberInfo] MemberInfo member)
    {
        Observed = member;
    }

    [Prefix]
    [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void InnerPrefix_Method_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Postfix]
    [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void InnerPostfix_Method_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Prefix]
    [Inner(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [typeof(int)])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [typeof(int)])]
    public static void InnerPrefix_Constructor_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Postfix]
    [Inner(typeof(ConstructorTargets), memberType: MemberType.Constructor, parameterTypes: [typeof(int)])]
    [Target(typeof(ConstructorTargets), nameof(ConstructorTargets.Create), parameterTypes: [typeof(int)])]
    public static void InnerPostfix_Constructor_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Prefix]
    [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), memberType: MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.FieldResult))]
    public static void InnerPrefix_FieldGetter_Static_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Postfix]
    [Inner(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.foo), memberType: MemberType.Getter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.ReadInstanceField))]
    public static void InnerPostfix_FieldGetter_Instance_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Prefix]
    [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Field), memberType: MemberType.Setter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SetStaticField))]
    public static void InnerPrefix_FieldSetter_Static_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Postfix]
    [Inner(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.foo), memberType: MemberType.Setter)]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SetInstanceField))]
    public static void InnerPostfix_FieldSetter_Instance_Scopes(
        [MemberInfo] MemberInfo member,
        [MemberInfo(Scope.Inner)] MemberInfo inner,
        [MemberInfo(Scope.Outer)] MemberInfo outer)
    {
        Observed = member;
        InnerObserved = inner;
        OuterObserved = outer;
    }

    [Prefix]
    [Targets(typeof(StaticMethodTargets), nameof(StaticMethodTargets.OverloadedVoid))]
    public static void Prefix_MultipleTargets_IdentifiesEachOverload([MemberInfo] MemberInfo member) =>
        Observed = member;
}

[TestFixture]
public sealed class MemberInfoBindingTests : PatchTestBase
{
    [Test]
    public void Prefix_Method_ClosedGenericType_Static()
    {
        MemberInfoBindingPatches.Observed = null;
        MethodInfo expected = typeof(MemberInfoGenericTargets<int>).GetMethod(nameof(MemberInfoGenericTargets<int>.StaticIdentity))!;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Prefix_Method_ClosedGenericType_Static));

        Assert.That(MemberInfoGenericTargets<int>.StaticIdentity(42), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
        var observed = (MethodInfo)MemberInfoBindingPatches.Observed!;
        Assert.Multiple(() =>
        {
            Assert.That(observed.DeclaringType, Is.EqualTo(typeof(MemberInfoGenericTargets<int>)));
            Assert.That(observed.ContainsGenericParameters, Is.False);
            Assert.That(observed.ReturnType, Is.EqualTo(typeof(int)));
            Assert.That(observed.GetParameters().Single().ParameterType, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void Postfix_Method_ClosedGenericType_Instance()
    {
        MemberInfoBindingPatches.Observed = null;
        MethodInfo expected = typeof(MemberInfoGenericTargets<string>).GetMethod(nameof(MemberInfoGenericTargets<string>.Identity))!;
        var target = new MemberInfoGenericTargets<string>();
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Postfix_Method_ClosedGenericType_Instance));

        Assert.That(target.Identity("argument"), Is.EqualTo("argument"));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
        var observed = (MethodInfo)MemberInfoBindingPatches.Observed!;
        Assert.Multiple(() =>
        {
            Assert.That(observed.DeclaringType, Is.EqualTo(typeof(MemberInfoGenericTargets<string>)));
            Assert.That(observed.ContainsGenericParameters, Is.False);
            Assert.That(observed.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(observed.GetParameters().Single().ParameterType, Is.EqualTo(typeof(string)));
        });
    }

    [Test]
    public void InnerPrefix_Method_ClosedGenericType_InnerScope()
    {
        MemberInfoBindingPatches.Observed = null;
        MethodInfo expected = typeof(MemberInfoGenericTargets<int>).GetMethod(nameof(MemberInfoGenericTargets<int>.Identity))!;
        var target = new MemberInfoGenericTargets<int>();
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPrefix_Method_ClosedGenericType_InnerScope));

        Assert.That(target.CallIdentity(42), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
        var observed = (MethodInfo)MemberInfoBindingPatches.Observed!;
        Assert.Multiple(() =>
        {
            Assert.That(observed.DeclaringType, Is.EqualTo(typeof(MemberInfoGenericTargets<int>)));
            Assert.That(observed.ContainsGenericParameters, Is.False);
            Assert.That(observed.ReturnType, Is.EqualTo(typeof(int)));
            Assert.That(observed.GetParameters().Single().ParameterType, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void InnerPostfix_Method_ClosedGenericType_OuterScope()
    {
        MemberInfoBindingPatches.Observed = null;
        MethodInfo expected = typeof(MemberInfoGenericTargets<string>).GetMethod(nameof(MemberInfoGenericTargets<string>.CallIdentity))!;
        var target = new MemberInfoGenericTargets<string>();
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPostfix_Method_ClosedGenericType_OuterScope));

        Assert.That(target.CallIdentity("argument"), Is.EqualTo("argument"));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
        var observed = (MethodInfo)MemberInfoBindingPatches.Observed!;
        Assert.Multiple(() =>
        {
            Assert.That(observed.DeclaringType, Is.EqualTo(typeof(MemberInfoGenericTargets<string>)));
            Assert.That(observed.ContainsGenericParameters, Is.False);
            Assert.That(observed.ReturnType, Is.EqualTo(typeof(string)));
            Assert.That(observed.GetParameters().Single().ParameterType, Is.EqualTo(typeof(string)));
        });
    }

    [Test]
    public void Prefix_Method_TypedMethodInfo()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Prefix_Method_TypedMethodInfo));

        Assert.That(StaticMethodTargets.IntIdentity(42), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntIdentity))!));
    }

    [Test]
    public void Prefix_Constructor_TypedConstructorInfo()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Prefix_Constructor_TypedConstructorInfo));

        var instance = new ConstructorTargets(42);
        Assert.That(instance.Value, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(typeof(ConstructorTargets).GetConstructor([typeof(int)])!));
    }

    [Test]
    public void InnerPrefix_Field_TypedFieldInfo()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPrefix_Field_TypedFieldInfo));

        InnerStaticMethodTargets.Field = 42;
        Assert.That(OuterStaticMethodTargets.FieldResult(), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(typeof(InnerStaticMethodTargets).GetField(nameof(InnerStaticMethodTargets.Field))!));
    }

    [Test]
    public void Prefix_Method_Static()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Prefix_Method_Static));
        MemberInfo expected = typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.IntIdentity))!;

        Assert.That(StaticMethodTargets.IntIdentity(42), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
    }

    [Test]
    public void Postfix_Method_Instance()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Postfix_Method_Instance));
        MemberInfo expected = typeof(ClassMethodTargets).GetMethod(nameof(ClassMethodTargets.IntIdentity))!;

        var target = new ClassMethodTargets();
        target.IntIdentity(42);

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
    }

    [Test]
    public void Prefix_Constructor()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Prefix_Constructor));
        MemberInfo expected = typeof(ConstructorTargets).GetConstructor([typeof(int)])!;

        var target = new ConstructorTargets(42);
        Assert.That(target.ConstructorExecuted, Is.True);
        Assert.That(target.Value, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
    }

    [Test]
    public void Postfix_Constructor()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.Postfix_Constructor));
        MemberInfo expected = typeof(ConstructorTargets).GetConstructor([typeof(int)])!;

        var target = new ConstructorTargets(42);
        Assert.That(target.ConstructorExecuted, Is.True);
        Assert.That(target.Value, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expected));
    }

    [Test]
    public void InnerPrefix_Method_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPrefix_Method_Scopes));
        MemberInfo expected = typeof(InnerStaticMethodTargets).GetMethod(nameof(InnerStaticMethodTargets.IntIdentity))!;
        MemberInfo expectedOuter = typeof(OuterStaticMethodTargets).GetMethod(nameof(OuterStaticMethodTargets.IntIdentity))!;

        Assert.That(OuterStaticMethodTargets.IntIdentity(42), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPostfix_Method_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPostfix_Method_Scopes));
        MemberInfo expected = typeof(InnerStaticMethodTargets).GetMethod(nameof(InnerStaticMethodTargets.IntIdentity))!;
        MemberInfo expectedOuter = typeof(OuterStaticMethodTargets).GetMethod(nameof(OuterStaticMethodTargets.IntIdentity))!;

        Assert.That(OuterStaticMethodTargets.IntIdentity(42), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPrefix_Constructor_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPrefix_Constructor_Scopes));
        MemberInfo expected = typeof(ConstructorTargets).GetConstructor([typeof(int)])!;
        MemberInfo expectedOuter = typeof(ConstructorTargets).GetMethod(nameof(ConstructorTargets.Create), [typeof(int)])!;

        var target = ConstructorTargets.Create(42);
        Assert.That(target.ConstructorExecuted, Is.True);
        Assert.That(target.Value, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPostfix_Constructor_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPostfix_Constructor_Scopes));
        MemberInfo expected = typeof(ConstructorTargets).GetConstructor([typeof(int)])!;
        MemberInfo expectedOuter = typeof(ConstructorTargets).GetMethod(nameof(ConstructorTargets.Create), [typeof(int)])!;

        var target = ConstructorTargets.Create(42);
        Assert.That(target.ConstructorExecuted, Is.True);
        Assert.That(target.Value, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPrefix_FieldGetter_Static_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPrefix_FieldGetter_Static_Scopes));
        MemberInfo expected = typeof(InnerStaticMethodTargets).GetField(nameof(InnerStaticMethodTargets.Field))!;
        MemberInfo expectedOuter = typeof(OuterStaticMethodTargets).GetMethod(nameof(OuterStaticMethodTargets.FieldResult))!;

        InnerStaticMethodTargets.Field = 42;
        Assert.That(OuterStaticMethodTargets.FieldResult(), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPostfix_FieldGetter_Instance_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPostfix_FieldGetter_Instance_Scopes));
        MemberInfo expected = typeof(InnerInstanceMethodTargets).GetField(nameof(InnerInstanceMethodTargets.foo))!;
        MemberInfo expectedOuter = typeof(OuterStaticMethodTargets).GetMethod(nameof(OuterStaticMethodTargets.ReadInstanceField))!;

        var inner = new InnerInstanceMethodTargets { foo = 42 };
        Assert.That(OuterStaticMethodTargets.ReadInstanceField(inner), Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPrefix_FieldSetter_Static_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPrefix_FieldSetter_Static_Scopes));
        MemberInfo expected = typeof(InnerStaticMethodTargets).GetField(nameof(InnerStaticMethodTargets.Field))!;
        MemberInfo expectedOuter = typeof(OuterStaticMethodTargets).GetMethod(nameof(OuterStaticMethodTargets.SetStaticField))!;

        InnerStaticMethodTargets.Field = 0;
        OuterStaticMethodTargets.SetStaticField(42);
        Assert.That(InnerStaticMethodTargets.Field, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void InnerPostfix_FieldSetter_Instance_Scopes()
    {
        MemberInfoBindingPatches.Observed = null;
        MemberInfoBindingPatches.InnerObserved = null;
        MemberInfoBindingPatches.OuterObserved = null;
        ApplyPatch(typeof(MemberInfoBindingPatches), nameof(MemberInfoBindingPatches.InnerPostfix_FieldSetter_Instance_Scopes));
        MemberInfo expected = typeof(InnerInstanceMethodTargets).GetField(nameof(InnerInstanceMethodTargets.foo))!;
        MemberInfo expectedOuter = typeof(OuterStaticMethodTargets).GetMethod(nameof(OuterStaticMethodTargets.SetInstanceField))!;

        var inner = new InnerInstanceMethodTargets();
        OuterStaticMethodTargets.SetInstanceField(inner, 42);
        Assert.That(inner.foo, Is.EqualTo(42));

        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(expectedOuter));
        Assert.That(MemberInfoBindingPatches.InnerObserved, Is.EqualTo(expected));
        Assert.That(MemberInfoBindingPatches.OuterObserved, Is.EqualTo(expectedOuter));
    }

    [Test]
    public void Prefix_MultipleTargets_IdentifiesEachOverload()
    {
        MemberInfoBindingPatches.Observed = null;
        ApplyPatch(typeof(MemberInfoBindingPatches),
            nameof(MemberInfoBindingPatches.Prefix_MultipleTargets_IdentifiesEachOverload));

        StaticMethodTargets.OverloadedVoid(42);
        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(
            typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.OverloadedVoid), [typeof(int)])));

        StaticMethodTargets.OverloadedVoid("argument");
        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(
            typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.OverloadedVoid), [typeof(string)])));

        StaticMethodTargets.OverloadedVoid(7);
        Assert.That(MemberInfoBindingPatches.Observed, Is.EqualTo(
            typeof(StaticMethodTargets).GetMethod(nameof(StaticMethodTargets.OverloadedVoid), [typeof(int)])));
    }
}
