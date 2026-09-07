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

    public static void UnqualifiedIntOverload_BindsConvertInt_AndReturns6(
        [Method("Convert")] Func<int, int> method) { }

    public static void ShortTypeName_BindsConvertInt_AndReturns6(
        [Method("MethodLookupTarget.Convert")] Func<int, int> method) { }

    public static void FullyQualifiedTypeName_BindsConvertInt_AndReturns6(
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Convert")] Func<int, int> method) { }

    public static void StringDelegate_BindsConvertString_AndReturnsHello(
        [Method("Convert")] Func<string, string> method) { }

    public static void ParameterlessDelegate_BindsConvertWithoutArguments_AndReturns42(
        [Method("Convert")] Func<int> method) { }

    public static void UnqualifiedInheritedMethod_BindsBaseDeclaration_AndReturns5(
        [Method("Inherited")] Func<int, int> method) { }

    public static void ExplicitBaseType_BindsBaseDeclaration_AndReturns5(
        [Method("MethodLookupBase.Inherited")] Func<int, int> method) { }

    public static void ExplicitInterface_BindsInterfaceDeclaration_AndReturns6(
        [Method("IMethodLookupTarget.Convert")] Func<int, int> method) { }

    public static void RefDelegate_BindsByRef_AndUpdatesArgumentTo6(
        [Method("ByRef")] MethodLookupPatches.RefMethod method) { }

    public static void InDelegate_BindsByIn_AndReturns5(
        [Method("ByIn")] MethodLookupPatches.InMethod method) { }

    public static void OutDelegate_BindsByOut_AndSetsArgumentTo7(
        [Method("ByOut")] MethodLookupPatches.OutMethod method) { }

    public static void UnrelatedStaticMethod_BindsMathAbs_AndReturns5(
        [Method("System.Math.Abs")] Func<int, int> method) { }

    public static void NestedShortTypeName_BindsNestedStaticMethod_AndReturns7(
        [Method("MethodLookupTarget.Nested.StaticMethod")] Func<int, int> method) { }

    public static void NestedFullyQualifiedName_BindsNestedStaticMethod_AndReturns7(
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Nested.StaticMethod")] Func<int, int> method) { }

    public static void NestedColonSyntax_BindsNestedStaticMethod_AndReturns7(
        [Method("MethodLookupTarget:Nested.StaticMethod")] Func<int, int> method) { }

    public static void DeepNestedColonSyntax_BindsDeepNestedStaticMethod_AndReturns8(
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget:Nested.DeepNested.StaticMethod")] Func<int, int> method) { }

    public static void DeepNestedDottedName_BindsDeepNestedStaticMethod_AndReturns8(
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Nested.DeepNested.StaticMethod")] Func<int, int> method) { }

    public static void UnqualifiedInheritedMethodOnNestedInstance_BindsBaseDeclaration_AndReturns5(
        [Method("Inherited")] Func<int, int> method) { }

    public static void DerivedOnlyMethodOnBaseInstance_ThrowsInstanceTypeMismatch(
        [Method("MethodLookupDerived.DerivedOnly")] Func<int, int> method) { }

    public static void UnrelatedInstanceMethod_ThrowsInstanceTypeMismatch(
        [Method("System.String.CompareTo")] Func<string, int> method) { }

    public static void OutDelegateForRefMethod_ThrowsMethodNotFound(
        [Method("ByRef")] MethodLookupPatches.OutMethod method) { }

    public static void ExplicitNestedTypeWithInheritedMethod_ThrowsMethodNotFound(
        [Method("MethodLookupTarget.Nested.Inherited")] Func<int, int> method) { }

    public static void ExplicitShortTypeWithInheritedMethod_ThrowsMethodNotFound(
        [Method("MethodLookupTarget.Inherited")] Func<int, int> method) { }

    public static void ExplicitFullyQualifiedTypeWithInheritedMethod_ThrowsMethodNotFound(
        [Method("Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Inherited")] Func<int, int> method) { }

    public static void ExplicitColonTypeWithInheritedMethod_ThrowsMethodNotFound(
        [Method("MethodLookupTarget:Inherited")] Func<int, int> method) { }

    public static void QualifiedStaticMethodOnStaticTarget_BindsMathAbsWithoutInstance(
        [Method("System.Math.Abs")] Func<int, int> method) { }
}

[TestFixture]
public sealed class MethodLookupBindingTests
{
    [Test]
    public void UnqualifiedIntOverload_BindsConvertInt_AndReturns6()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.UnqualifiedIntOverload_BindsConvertInt_AndReturns6))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("Convert", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Convert' must select MethodLookupTarget.Convert with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(6));
    }

    [Test]
    public void ShortTypeName_BindsConvertInt_AndReturns6()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ShortTypeName_BindsConvertInt_AndReturns6))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("Convert", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'MethodLookupTarget.Convert' must select MethodLookupTarget.Convert with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(6));
    }

    [Test]
    public void FullyQualifiedTypeName_BindsConvertInt_AndReturns6()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.FullyQualifiedTypeName_BindsConvertInt_AndReturns6))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("Convert", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Convert' must select MethodLookupTarget.Convert with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(6));
    }

    [Test]
    public void StringDelegate_BindsConvertString_AndReturnsHello()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.StringDelegate_BindsConvertString_AndReturnsHello))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("Convert", [typeof(string)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Convert' must select MethodLookupTarget.Convert with the delegate's signature.");
        var callable = (Func<string, string>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable("hello"), Is.EqualTo("hello"));
    }

    [Test]
    public void ParameterlessDelegate_BindsConvertWithoutArguments_AndReturns42()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ParameterlessDelegate_BindsConvertWithoutArguments_AndReturns42))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("Convert", [])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Convert' must select MethodLookupTarget.Convert with the delegate's signature.");
        var callable = (Func<int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(), Is.EqualTo(42));
    }

    [Test]
    public void UnqualifiedInheritedMethod_BindsBaseDeclaration_AndReturns5()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.UnqualifiedInheritedMethod_BindsBaseDeclaration_AndReturns5))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupBase).GetMethod("Inherited", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Inherited' must select MethodLookupBase.Inherited with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(5));
    }

    [Test]
    public void ExplicitBaseType_BindsBaseDeclaration_AndReturns5()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ExplicitBaseType_BindsBaseDeclaration_AndReturns5))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupBase).GetMethod("Inherited", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'MethodLookupBase.Inherited' must select MethodLookupBase.Inherited with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(5));
    }

    [Test]
    public void ExplicitInterface_BindsInterfaceDeclaration_AndReturns6()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ExplicitInterface_BindsInterfaceDeclaration_AndReturns6))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(IMethodLookupTarget).GetMethod("Convert", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'IMethodLookupTarget.Convert' must select IMethodLookupTarget.Convert with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(6));
    }

    [Test]
    public void RefDelegate_BindsByRef_AndUpdatesArgumentTo6()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.RefDelegate_BindsByRef_AndUpdatesArgumentTo6))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("ByRef", [typeof(int).MakeByRefType()])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'ByRef' must select MethodLookupTarget.ByRef with the delegate's signature.");
        var callable = (MethodLookupPatches.RefMethod)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        int value = 5;
        Assert.That(callable(ref value), Is.EqualTo(6));
        Assert.That(value, Is.EqualTo(6));
    }

    [Test]
    public void InDelegate_BindsByIn_AndReturns5()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.InDelegate_BindsByIn_AndReturns5))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("ByIn", [typeof(int).MakeByRefType()])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'ByIn' must select MethodLookupTarget.ByIn with the delegate's signature.");
        var callable = (MethodLookupPatches.InMethod)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        int value = 5;
        Assert.That(callable(in value), Is.EqualTo(5));
        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public void OutDelegate_BindsByOut_AndSetsArgumentTo7()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.OutDelegate_BindsByOut_AndSetsArgumentTo7))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget).GetMethod("ByOut", [typeof(int).MakeByRefType()])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'ByOut' must select MethodLookupTarget.ByOut with the delegate's signature.");
        var callable = (MethodLookupPatches.OutMethod)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        int value = 5;
        Assert.That(callable(out value), Is.EqualTo(7));
        Assert.That(value, Is.EqualTo(7));
    }

    [Test]
    public void UnrelatedStaticMethod_BindsMathAbs_AndReturns5()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.UnrelatedStaticMethod_BindsMathAbs_AndReturns5))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(Math).GetMethod("Abs", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'System.Math.Abs' must select Math.Abs with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(-5), Is.EqualTo(5));
    }

    [Test]
    public void NestedShortTypeName_BindsNestedStaticMethod_AndReturns7()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.NestedShortTypeName_BindsNestedStaticMethod_AndReturns7))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget.Nested).GetMethod("StaticMethod", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'MethodLookupTarget.Nested.StaticMethod' must select MethodLookupTarget.Nested.StaticMethod with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget.Nested(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(7));
    }

    [Test]
    public void NestedFullyQualifiedName_BindsNestedStaticMethod_AndReturns7()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.NestedFullyQualifiedName_BindsNestedStaticMethod_AndReturns7))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget.Nested).GetMethod("StaticMethod", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Nested.StaticMethod' must select MethodLookupTarget.Nested.StaticMethod with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget.Nested(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(7));
    }

    [Test]
    public void NestedColonSyntax_BindsNestedStaticMethod_AndReturns7()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.NestedColonSyntax_BindsNestedStaticMethod_AndReturns7))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget.Nested).GetMethod("StaticMethod", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'MethodLookupTarget:Nested.StaticMethod' must select MethodLookupTarget.Nested.StaticMethod with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget.Nested(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(7));
    }

    [Test]
    public void DeepNestedColonSyntax_BindsDeepNestedStaticMethod_AndReturns8()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.DeepNestedColonSyntax_BindsDeepNestedStaticMethod_AndReturns8))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget.Nested.DeepNested).GetMethod("StaticMethod", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget:Nested.DeepNested.StaticMethod' must select MethodLookupTarget.Nested.DeepNested.StaticMethod with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget.Nested(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(8));
    }

    [Test]
    public void DeepNestedDottedName_BindsDeepNestedStaticMethod_AndReturns8()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.DeepNestedDottedName_BindsDeepNestedStaticMethod_AndReturns8))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupTarget.Nested.DeepNested).GetMethod("StaticMethod", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Nested.DeepNested.StaticMethod' must select MethodLookupTarget.Nested.DeepNested.StaticMethod with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget.Nested(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(8));
    }

    [Test]
    public void UnqualifiedInheritedMethodOnNestedInstance_BindsBaseDeclaration_AndReturns5()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.UnqualifiedInheritedMethodOnNestedInstance_BindsBaseDeclaration_AndReturns5))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");
        MethodInfo expectedMethod = typeof(MethodLookupBase).GetMethod("Inherited", [typeof(int)])!;

        var binding = binder.Bind(parameter);

        Assert.That(binding.bindingType, Is.EqualTo(BindingType.Delegate));
        Assert.That(binding.methodInfo, Is.EqualTo(expectedMethod),
            "Lookup 'Inherited' must select MethodLookupBase.Inherited with the delegate's signature.");
        var callable = (Func<int, int>)Delegate.CreateDelegate(parameter.ParameterType,
            expectedMethod.IsStatic ? null : new MethodLookupTarget.Nested(), (MethodInfo)binding.methodInfo!);
        Assert.That(callable(5), Is.EqualTo(5));
    }

    [Test]
    public void DerivedOnlyMethodOnBaseInstance_ThrowsInstanceTypeMismatch()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.DerivedOnlyMethodOnBaseInstance_ThrowsInstanceTypeMismatch))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'MethodLookupDerived.DerivedOnly' with Func<int, int> on MethodLookupTarget must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Instance type mismatch"));
    }

    [Test]
    public void UnrelatedInstanceMethod_ThrowsInstanceTypeMismatch()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.UnrelatedInstanceMethod_ThrowsInstanceTypeMismatch))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'System.String.CompareTo' with Func<string, int> on MethodLookupTarget must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Instance type mismatch"));
    }

    [Test]
    public void OutDelegateForRefMethod_ThrowsMethodNotFound()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.OutDelegateForRefMethod_ThrowsMethodNotFound))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'ByRef' with MethodLookupPatches.OutMethod on MethodLookupTarget must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Method not found"));
    }

    [Test]
    public void ExplicitNestedTypeWithInheritedMethod_ThrowsMethodNotFound()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ExplicitNestedTypeWithInheritedMethod_ThrowsMethodNotFound))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget.Nested), typeof(void),
            [typeof(MethodLookupTarget.Nested)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'MethodLookupTarget.Nested.Inherited' with Func<int, int> on MethodLookupTarget.Nested must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Method not found"));
    }

    [Test]
    public void ExplicitShortTypeWithInheritedMethod_ThrowsMethodNotFound()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ExplicitShortTypeWithInheritedMethod_ThrowsMethodNotFound))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'MethodLookupTarget.Inherited' with Func<int, int> on MethodLookupTarget must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Method not found"));
    }

    [Test]
    public void ExplicitFullyQualifiedTypeWithInheritedMethod_ThrowsMethodNotFound()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ExplicitFullyQualifiedTypeWithInheritedMethod_ThrowsMethodNotFound))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'Disharmony.Tests.Unit.ParameterBinding.MethodLookupTarget.Inherited' with Func<int, int> on MethodLookupTarget must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Method not found"));
    }

    [Test]
    public void ExplicitColonTypeWithInheritedMethod_ThrowsMethodNotFound()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.ExplicitColonTypeWithInheritedMethod_ThrowsMethodNotFound))!.GetParameters().Single();
        var invocation = new MockInvocation(typeof(MethodLookupTarget), typeof(void),
            [typeof(MethodLookupTarget)], ["<instance>"], false);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var exception = Assert.Throws<ParameterBindingException>(() => binder.Bind(parameter),
            "Lookup 'MethodLookupTarget:Inherited' with Func<int, int> on MethodLookupTarget must be rejected.");

        Assert.That(exception!.Message, Is.EqualTo("method: Method not found"));
    }

    [Test]
    public void QualifiedStaticMethodOnStaticTarget_BindsMathAbsWithoutInstance()
    {
        ParameterInfo parameter = typeof(MethodLookupPatches)
            .GetMethod(nameof(MethodLookupPatches.QualifiedStaticMethodOnStaticTarget_BindsMathAbsWithoutInstance))!.GetParameters().Single();
        var invocation = new MethodInvocation(typeof(MethodLookupTarget).GetMethod("StaticTarget")!);
        var binder = new ParameterBinder(invocation, invocation, EmptyInvocation.Instance,
            PatchType.Prefix, PatchOptions.Default, "test");

        var binding = binder.Bind(parameter);

        Assert.That(binding.methodInfo, Is.EqualTo(typeof(Math).GetMethod("Abs", [typeof(int)])));
    }
}
