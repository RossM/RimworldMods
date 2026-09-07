namespace Disharmony.Tests.Unit.ParameterBinding;

internal interface IMethodLookupTarget
{
    int Convert(int value);
}

internal class MethodLookupBase
{
    public int Inherited(int value) => value;
}

internal class MethodLookupTarget : MethodLookupBase, IMethodLookupTarget
{
    public class Nested : MethodLookupBase
    {
        public static int StaticMethod(int value) => value + 2;

        public class DeepNested
        {
            public static int StaticMethod(int value) => value + 3;
        }
    }

    public void Target() { }
    public static void StaticTarget() { }
    public int Convert(int value) => value + 1;
    public string Convert(string value) => value;
    public int Convert() => 42;
    public int ByRef(ref int value) => ++value;
    public int ByIn(in int value) => value;
    public int ByOut(out int value) => value = 7;
}

internal class MethodLookupDerived : MethodLookupTarget
{
    public int DerivedOnly(int value) => value;
}

internal static class MethodLookupPatches
{
    public delegate int RefMethod(ref int value);
    public delegate int InMethod(in int value);
    public delegate int OutMethod(out int value);

    public static void NestedPatch(
        [Method("MethodLookupTarget.Nested.StaticMethod")] Func<int, int> shortType,
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Nested.StaticMethod")] Func<int, int> fullType,
        [Method("MethodLookupTarget:Nested.StaticMethod")] Func<int, int> colon,
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget:Nested.DeepNested.StaticMethod")] Func<int, int> deepColon,
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Nested.DeepNested.StaticMethod")] Func<int, int> deepDot,
        [Method("MethodLookupTarget.Nested.Inherited")] Func<int, int> inherited) { }

    public static void Patch(
        [Method("Convert")] Func<int, int> unqualified,
        [Method("MethodLookupTarget.Convert")] Func<int, int> shortType,
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Convert")] Func<int, int> fullType,
        [Method("Convert")] Func<string, string> stringOverload,
        [Method("Convert")] Func<int> noArguments,
        [Method("Inherited")] Func<int, int> inherited,
        [Method("MethodLookupBase.Inherited")] Func<int, int> baseType,
        [Method("IMethodLookupTarget.Convert")] Func<int, int> interfaceType,
        [Method("ByRef")] RefMethod byRef,
        [Method("ByIn")] InMethod byIn,
        [Method("ByOut")] OutMethod byOut,
        [Method("System.Math.Abs")] Func<int, int> unrelatedStatic,
        [Method("MethodLookupDerived.DerivedOnly")] Func<int, int> derived,
        [Method("System.String.CompareTo")] Func<string, int> unrelatedInstance,
        [Method("ByRef")] OutMethod wrongModifier) { }
}

[TestFixture]
public sealed class MethodLookupBindingTests
{
    [TestCase(0, 7)]
    [TestCase(1, 7)]
    [TestCase(2, 7)]
    [TestCase(3, 8)]
    [TestCase(4, 8)]
    [TestCase(5, 5)]
    public void BindMethod_ResolvesNestedTypesThroughSharedLookup(int parameterIndex, int expected)
    {
        ParameterInfo parameter = typeof(MethodLookupPatches).GetMethod("NestedPatch")!.GetParameters()[parameterIndex];
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var binding = binder.Bind(parameter);
        MethodInfo method = (MethodInfo)binding.methodInfo!;
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            method.IsStatic ? null : new MethodLookupTarget.Nested(), method);

        Assert.That(callable(5), Is.EqualTo(expected));
    }

    [TestCase(0, 6)]
    [TestCase(1, 6)]
    [TestCase(2, 6)]
    [TestCase(3, "hello")]
    [TestCase(4, 42)]
    [TestCase(5, 5)]
    [TestCase(6, 5)]
    [TestCase(7, 6)]
    [TestCase(8, 6)]
    [TestCase(9, 5)]
    [TestCase(10, 7)]
    [TestCase(11, 5)]
    public void BindMethod_SelectsCallableMethod(int parameterIndex, object expected)
    {
        ParameterInfo parameter = typeof(MethodLookupPatches).GetMethod("Patch")!.GetParameters()[parameterIndex];
        var invocation = new MethodInvocation(typeof(MethodLookupTarget).GetMethod("Target")!);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var binding = binder.Bind(parameter);
        MethodInfo method = (MethodInfo)binding.methodInfo!;
        Delegate callable = Delegate.CreateDelegate(parameter.ParameterType,
            method.IsStatic ? null : new MethodLookupTarget(), method);
        object[] arguments = parameterIndex == 4 ? [] : parameterIndex == 3 ? ["hello"] : [5];

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(callable.DynamicInvoke(arguments), Is.EqualTo(expected));
    }

    [TestCase(12, "Instance type mismatch")]
    [TestCase(13, "Instance type mismatch")]
    [TestCase(14, "Method not found")]
    public void BindMethod_RejectsIncompatibleMethod(int parameterIndex, string message)
    {
        ParameterInfo parameter = typeof(MethodLookupPatches).GetMethod("Patch")!.GetParameters()[parameterIndex];
        var invocation = new MethodInvocation(typeof(MethodLookupTarget).GetMethod("Target")!);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter));

        Assert.That(exception!.Message, Is.EqualTo($"{parameter.Name}: {message}"));
    }

    [Test]
    public void BindMethod_QualifiedStaticMethodDoesNotRequireInstance()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches).GetMethod("Patch")!.GetParameters()[11];
        var invocation = new MethodInvocation(typeof(MethodLookupTarget).GetMethod("StaticTarget")!);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var binding = binder.Bind(parameter);

        Assert.That(binding.methodInfo, Is.EqualTo(typeof(Math).GetMethod("Abs", [typeof(int)])));
    }
}
